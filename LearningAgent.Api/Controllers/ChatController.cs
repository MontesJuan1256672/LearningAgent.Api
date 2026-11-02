using LearningAgent.Api.Dtos;
using LearningAgent.Api.Services.Agent;
using LearningAgent.Api.Services.Embedding;
using LearningAgent.Api.Services.Qdrant;
using LearningAgent.Api.Services.Retrieval;
using LearningAgent.Api.Services.Tools;
using Microsoft.AspNetCore.Mvc;
using Qdrant.Client.Grpc;
using UglyToad.PdfPig;

namespace LearningAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly IRetrievalService _retrievalService;

    public ChatController(
        IAgentService agentService, 
        IToolRegistry toolRegistry, 
        IEmbeddingService embeddingService, 
        IQdrantService qdrantService,
        IRetrievalService retrievalService)
    {
        _agentService = agentService;
        _toolRegistry = toolRegistry;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _retrievalService = retrievalService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
    {
        //string response = await _chatService.GetResponseAsync(request.Message);
        string response = await _agentService.ProcessAsync(request.ConversationId, request.Message);

        return Ok(new ChatResponse
        {
            Response = response
        });
    }

    [HttpGet("test-tool/{name}")]
    public async Task<IActionResult> TestTool(string name)
    {
        var tool = _toolRegistry.GetTool(name);

        if (tool is null)
            return NotFound($"No se encontró la herramienta '{name}'.");

        var result = await tool.ExecuteAsync("""{"expression":"10 + 20"}""");

        return Ok(new
        {
            tool.Definition.Name,
            tool.Definition.Description,
            tool.Definition.Parameters,
            result
        });
    }

    [HttpPost("test-embedding")]
    public async Task<IActionResult> GenerateEmbedding([FromBody] string text)
    {
        var embedding = await _embeddingService.GenerateAsync(text);

        return Ok(new
        {
            Dimensions = embedding.Length,
            Embedding = embedding
        });
    }

    [HttpPost("test-similarity")]
    public async Task<IActionResult> TestSimilarity([FromBody] SimilarityRequest request)
    {      
        var embeddingA = await _embeddingService.GenerateAsync(request.TextA);
        var embeddingB = await _embeddingService.GenerateAsync(request.TextB);

        var similarity = new EmbeddingSimilarity();

        double result = similarity.CosineSimilarity(
            embeddingA,
            embeddingB);

        return Ok(new
        {
            TextA = request.TextA,
            TextB = request.TextB,
            DimensionsA = embeddingA.Length,
            DimensionsB = embeddingB.Length,
            Similarity = result
        });
    }

    [HttpPost("test-chunking")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> TestChunking(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debes seleccionar un archivo PDF.");

        if (!Path.GetExtension(file.FileName)
            .Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("El archivo debe ser un PDF.");
        }

        await using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);

        var paginas = document.GetPages()
            .Select(pagina => pagina.Text)
            .ToList();

        var cleaner = new TextCleaner();
        var texto = cleaner.CleanPages(paginas);

        var chunker = new TextChunker();
        var chunks = chunker.SplitText(texto);

        return Ok(new
        {
            TotalPaginas = document.NumberOfPages,
            TotalCaracteres = texto.Length,
            TotalChunks = chunks.Count,
            Chunks = chunks
                .Take(3)
                .Select((chunk, index) => new
                {
                    Numero = index + 1,
                    Caracteres = chunk.Length,
                    Contenido = chunk
                })
        });
    }

    public class SimilarityRequest
    {
        public string TextA { get; set; } = string.Empty;
        public string TextB { get; set; } = string.Empty;
    }


    [HttpPost("test-pdf-embeddings")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> TestPdfEmbeddings(IFormFile file)
    {        
        if (file == null || file.Length == 0)
            return BadRequest("Debes seleccionar un archivo PDF.");

        if (!Path.GetExtension(file.FileName)
            .Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("El archivo debe ser un PDF.");
        }

        await using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);

        // 1. Extraer texto de cada página.
        var paginas = document.GetPages()
            .Select(pagina => pagina.Text)
            .ToList();

        // 2. Limpiar el texto.
        var cleaner = new TextCleaner();
        var textoLimpio = cleaner.CleanPages(paginas);

        // 3. Dividir el texto en chunks.
        var chunker = new TextChunker();
        var chunks = chunker.SplitText(textoLimpio);

        // 4. Generar un embedding por cada chunk.
        int embeddingsGenerados = 0;
        var dimensionesEncontradas = new HashSet<int>();
        float[]? primerEmbedding = null;

        foreach (var chunk in chunks)
        {
            var embedding = await _embeddingService.GenerateAsync(chunk);

            if (embedding.Length == 0)
                return StatusCode(500,
                    $"Ollama devolvió un embedding vacío en el chunk {embeddingsGenerados + 1}.");

            primerEmbedding ??= embedding;

            dimensionesEncontradas.Add(embedding.Length);
            embeddingsGenerados++;
        }

        return Ok(new
        {
            TotalPaginas = document.NumberOfPages,
            CaracteresOriginales = paginas.Sum(pagina => pagina.Length),
            CaracteresLimpios = textoLimpio.Length,
            TotalChunks = chunks.Count,
            EmbeddingsGenerados = embeddingsGenerados,
            DimensionesEncontradas = dimensionesEncontradas,
            PrimerEmbedding = primerEmbedding?
                .Take(10)
                .ToArray()
        });
    }


    [HttpPost("test-qdrant/index-pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> IndexPdfInQdrant(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debes seleccionar un archivo PDF.");

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        { 
            return BadRequest("El archivo debe ser un PDF.");
        }

        await using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);

        // 1. Extraer y limpiar el texto.
        var paginas = document.GetPages()
            .Select(pagina => pagina.Text)
            .ToList();

        var cleaner = new TextCleaner();
        var textoLimpio = cleaner.CleanPages(paginas);

        // 2. Dividir el texto en chunks.
        var chunker = new TextChunker();
        var chunks = chunker.SplitText(textoLimpio);

        if (chunks.Count == 0)
            return BadRequest("El PDF no contiene texto para indexar.");

        // 3. Generar embeddings y construir los puntos.
        var documentId = Guid.NewGuid().ToString();
        var points = new List<PointStruct>(chunks.Count);
        const int dimensionesEsperadas = 768;

        for (int i = 0; i < chunks.Count; i++)
        {
            var embedding = await _embeddingService.GenerateAsync(chunks[i]);

            if (embedding.Length != dimensionesEsperadas)
            {
                return StatusCode(500,
                    $"El embedding del chunk {i + 1} tiene " +
                    $"{embedding.Length} dimensiones; se esperaban " +
                    $"{dimensionesEsperadas}.");
            }

            points.Add(new PointStruct
            {
                Id = Guid.NewGuid(),
                Vectors = embedding,
                Payload =
            {
                ["documentId"] = documentId,
                ["fileName"] = Path.GetFileName(file.FileName),
                ["chunkIndex"] = i,
                ["text"] = chunks[i]
            }
            });
        }

        // 4. Guardar los puntos en Qdrant.
        await _qdrantService.UpsertPointsAsync(points);

        return Ok(new
        {
            DocumentId = documentId,
            FileName = Path.GetFileName(file.FileName),
            TotalPaginas = document.NumberOfPages,
            TotalChunks = chunks.Count,
            PuntosInsertados = points.Count,
            DimensionesVector = dimensionesEsperadas
        });
    }


    [HttpGet("test-qdrant")]
    public async Task<IActionResult> TestQdrant()
    {
        var collections = await _qdrantService.ListCollectionsAsync();

        return Ok(new
        {
            Connected = true,
            Collections = collections
        });
    }

    [HttpPost("test-qdrant/create-collection")]
    public async Task<IActionResult> CreateQdrantCollection()
    {
        await _qdrantService.CreateCollectionAsync();

        return Ok(new
        {
            Created = true,
            CollectionName = "learningagent_documents",
            VectorSize = 768,
            Distance = "Cosine"
        });
    }

    [HttpPost("test-qdrant/insert-points")]
    public async Task<IActionResult> InsertTestPoints()
    {
        const int dimensions = 768;

        // Vector 1: dirección principal en la dimensión 0.
        var vector1 = new float[dimensions];
        vector1[0] = 1f;

        // Vector 2: similar al vector 1.
        var vector2 = new float[dimensions];
        vector2[0] = 0.9f;
        vector2[1] = 0.1f;

        // Vector 3: dirección principal en la dimensión 1.
        var vector3 = new float[dimensions];
        vector3[1] = 1f;

        var points = new List<PointStruct>
    {
        new()
        {
            Id = 1001,
            Vectors = vector1,
            Payload =
            {
                ["documentId"] = "test-document",
                ["fileName"] = "qdrant-test.pdf",
                ["chunkIndex"] = 0,
                ["text"] = "Conceptos básicos de .NET."
            }
        },
        new()
        {
            Id = 1002,
            Vectors = vector2,
            Payload =
            {
                ["documentId"] = "test-document",
                ["fileName"] = "qdrant-test.pdf",
                ["chunkIndex"] = 1,
                ["text"] = "Introducción a C# y .NET."
            }
        },
        new()
        {
            Id = 1003,
            Vectors = vector3,
            Payload =
            {
                ["documentId"] = "test-document",
                ["fileName"] = "qdrant-test.pdf",
                ["chunkIndex"] = 2,
                ["text"] = "Conceptos de bases de datos."
            }
        }
    };

        await _qdrantService.UpsertPointsAsync(points);

        return Ok(new
        {
            Inserted = points.Count,
            PointIds = new[] { 1001, 1002, 1003 },
            VectorDimensions = dimensions
        });
    }

    [HttpGet("test-qdrant/search")]
    public async Task<IActionResult> SearchQdrant()
    {
        const int dimensions = 768;

        var queryVector = new float[dimensions];
        queryVector[0] = 1f;

        var results = await _qdrantService.SearchPointsAsync(queryVector, 3);

        return Ok(results.Select(point => new
        {
            Id = point.Id,
            Score = point.Score
        }));
    }


    public class SearchDocumentRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    [HttpPost("test-qdrant/search-document")]
    public async Task<IActionResult> SearchDocument([FromBody] SearchDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Debes proporcionar una pregunta.");

        // 1. Convertir la pregunta en un embedding.
        var embedding = await _embeddingService.GenerateAsync(request.Query);            

        // 2. Buscar los 5 vectores más similares.
        var results = await _qdrantService.SearchPointsAsync(embedding, 5);

        // 3. Devolver el texto y los metadatos recuperados.
        var matches = results.Select(point => new
        {
            Id = point.Id,
            Score = point.Score,
            FileName = point.Payload.TryGetValue(
                "fileName", out var fileName)
                    ? fileName.StringValue
                    : string.Empty,
            ChunkIndex = point.Payload.TryGetValue(
                "chunkIndex", out var chunkIndex)
                    ? chunkIndex.IntegerValue
                    : 0,
            Text = point.Payload.TryGetValue(
                "text", out var text)
                    ? text.StringValue
                    : string.Empty
        });

        return Ok(new
        {
            Query = request.Query,
            Results = matches
        });
    }


    public class RetrievalRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    [HttpPost("test-retrieval")]
    public async Task<IActionResult> TestRetrieval(
        [FromBody] RetrievalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest("Debes proporcionar una pregunta.");

        var context = await _retrievalService.RetrieveContextAsync(request.Query);            

        return Ok(new
        {
            Query = request.Query,
            Context = context
        });
    }

}
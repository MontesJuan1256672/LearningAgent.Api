
namespace LearningAgent.Api.Services.Embedding;

public class TextChunker
{
    public List<string> SplitText(
        string text,
        int chunkSize = 1000,
        int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        if (chunkSize <= 0)
            throw new ArgumentException("El tamaño del chunk debe ser mayor que cero.");                

        if (overlap < 0 || overlap >= chunkSize)
            throw new ArgumentException("El overlap debe ser menor que el tamaño del chunk.");                

        var chunks = new List<string>();
        int start = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);

            // Buscar un espacio o salto de línea cerca del límite.
            if (end < text.Length)
            {
                int minEnd = start + (chunkSize * 80 / 100);
                int breakPosition = -1;

                for (int i = end; i > minEnd; i--)
                {
                    if (char.IsWhiteSpace(text[i - 1]))
                    {
                        breakPosition = i;
                        break;
                    }
                }

                if (breakPosition > start)
                    end = breakPosition;
            }

            string chunk = text[start..end].Trim();

            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            if (end >= text.Length)
                break;

            // Mantener el solapamiento entre fragmentos.
            int nextStart = end - overlap;

            // Evitar comenzar en medio de una palabra.
            while (nextStart > start &&
                   nextStart < text.Length &&
                   !char.IsWhiteSpace(text[nextStart - 1]) &&
                   !char.IsWhiteSpace(text[nextStart]))
            {
                nextStart--;
            }

            start = nextStart;
        }

        return chunks;
    }
}
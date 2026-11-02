namespace LearningAgent.Api.Services.Embedding;

public class EmbeddingSimilarity
{
    public double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Los vectores deben tener la misma dimensión.");                

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
            throw new ArgumentException("Los vectores no pueden tener magnitud cero.");
        
        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));       
    }
}

using System.Text.RegularExpressions;

namespace LearningAgent.Api.Services.Embedding;

public class TextCleaner
{
    public string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Replace("\r\n", "\n")
                   .Replace('\r', '\n')
                   .Replace('\t', ' ');

        var lines = text.Split('\n')
            .Select(line => Regex.Replace(line.Trim(), @"[ ]{2,}", " "))
            .ToList();

        // Reducir líneas vacías consecutivas.
        var result = new List<string>();
        bool previousLineWasEmpty = false;

        foreach (var line in lines)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(line);

            if (isEmpty)
            {
                if (!previousLineWasEmpty)
                    result.Add(string.Empty);
            }
            else
            {
                result.Add(line);
            }

            previousLineWasEmpty = isEmpty;
        }

        return string.Join("\n", result).Trim();
    }

    public string CleanPages(IEnumerable<string> pages)
    {
        var pageLines = pages
            .Select(page => page.Replace("\r\n", "\n")
                                .Replace('\r', '\n')
                                .Split('\n')
                                .Select(line => line.Trim())
                                .ToList())
            .ToList();

        // Obtener la primera y última línea no vacía de cada página.
        var edgeLines = pageLines
            .SelectMany(lines => new[]
            {
                lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)),
                lines.LastOrDefault(line => !string.IsNullOrWhiteSpace(line))
            })
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(NormalizeHeaderFooter)
            .GroupBy(line => line)
            .Where(group => group.Count() >= 3)
            .Select(group => group.Key)
            .ToHashSet();

        var cleanedPages = pageLines.Select(lines =>
        {
            // Eliminar encabezados y pies repetidos.
            var first = lines.FindIndex(
                line => !string.IsNullOrWhiteSpace(line));

            var last = lines.FindLastIndex(
                line => !string.IsNullOrWhiteSpace(line));

            if (first >= 0 && edgeLines.Contains(NormalizeHeaderFooter(lines[first])))
                lines[first] = string.Empty;

            if (last >= 0 && edgeLines.Contains(NormalizeHeaderFooter(lines[last])))
                lines[last] = string.Empty;

            return CleanText(string.Join("\n", lines));
        });

        return string.Join("\n\n", cleanedPages)
            .Trim();
    }

    private static string NormalizeHeaderFooter(string line)
    {
        // Permite comparar encabezados que solo cambian por el número de página.
        line = Regex.Replace(line, @"\s+", " ").Trim();

        return Regex.Replace(line, @"\s+\d+\s*$", "");
    }
}
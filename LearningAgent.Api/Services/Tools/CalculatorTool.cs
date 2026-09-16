using LearningAgent.Api.Services.Tool;
using System.Globalization;
using System.Text.Json;

namespace LearningAgent.Api.Services.Tools;

public class CalculatorTool : ITool
{
    public ToolDefinition Definition => new()
    {
        Name = "calculator",

        Description = "Realiza operaciones matemáticas simples.",

        Parameters = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                expression = new
                {
                    type = "string",
                    description = "Expresión matemática que se desea calcular."
                }
            },
            required = new[]
            {
                "expression"
            }
        })
    };

    public Task<string> ExecuteAsync(string arguments)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(arguments);

            if (!document.RootElement.TryGetProperty("expression", out JsonElement expressionElement))
            {
                return Task.FromResult("Falta el parámetro 'expression'.");
            }

            string? expression = expressionElement.GetString();

            if (string.IsNullOrWhiteSpace(expression))
            {
                return Task.FromResult("El parámetro 'expression' no puede estar vacío.");
            }

            string[] operadores = { "+", "-", "*", "/" };

            foreach (string operador in operadores)
            {
                int posicion = expression.IndexOf(operador);

                if (posicion > 0)
                {
                    string izquierda = expression[..posicion].Trim();
                    string derecha = expression[(posicion + 1)..].Trim();

                    if (!double.TryParse(
                        izquierda,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double numeroIzquierdo))
                    {
                        return Task.FromResult("La expresión contiene un número inválido.");
                    }

                    if (!double.TryParse(
                        derecha,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double numeroDerecho))
                    {
                        return Task.FromResult("La expresión contiene un número inválido.");
                    }

                    double resultado = operador switch
                    {
                        "+" => numeroIzquierdo + numeroDerecho,
                        "-" => numeroIzquierdo - numeroDerecho,
                        "*" => numeroIzquierdo * numeroDerecho,

                        "/" when numeroDerecho != 0 =>
                            numeroIzquierdo / numeroDerecho,

                        "/" => double.NaN,

                        _ => double.NaN
                    };

                    if (double.IsNaN(resultado))
                    {
                        return Task.FromResult("No se puede dividir entre cero.");
                    }

                    return Task.FromResult(resultado.ToString(CultureInfo.InvariantCulture));
                }
            }

            return Task.FromResult("No se pudo interpretar la expresión matemática.");
        }
        catch (JsonException)
        {
            return Task.FromResult("Los argumentos de la herramienta no tienen un formato JSON válido.");
        }
    }
}
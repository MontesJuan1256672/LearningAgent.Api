using LearningAgent.Api.Services.Tool;
using System.Globalization;
using System.Text.Json;

namespace LearningAgent.Api.Services.Tools;

public class CalculatorTool : ITool
{
    public ToolDefinition Definition => new()
    {
        Name = "calculator",

        Description =
         "Calcula expresiones matemáticas usando +, -, *, / y paréntesis. " +
         "Respeta la precedencia matemática. Si el usuario pide realizar " +
         "una operación y después otra usando el resultado anterior, " +
         "construye una expresión equivalente usando paréntesis.",

        Parameters = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                expression = new
                {
                    type = "string",
                    description =
                     "Expresión matemática completa que se desea calcular. " +
                     "Puede contener números, +, -, *, / y paréntesis. " +
                     "Usa paréntesis para representar operaciones que deben " +
                     "realizarse antes que otras."
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

            if (!document.RootElement.TryGetProperty(
                "expression",
                out JsonElement expressionElement))
            {
                return Task.FromResult("Falta el parámetro 'expression'.");
            }

            string? expression = expressionElement.GetString();

            if (string.IsNullOrWhiteSpace(expression))
            {
                return Task.FromResult("El parámetro 'expression' no puede estar vacío.");          
            }

            var parser = new MathParser(expression);

            double result = parser.Parse();

            return Task.FromResult(result.ToString(CultureInfo.InvariantCulture));
        }
        catch (JsonException)
        {
            return Task.FromResult("Los argumentos de la herramienta no tienen un formato JSON válido.");
        }
        catch (FormatException ex)
        {
            return Task.FromResult($"Expresión matemática inválida: {ex.Message}");
        }
        catch (DivideByZeroException)
        {
            return Task.FromResult("No se puede dividir entre cero.");
        }
    }

    private class MathParser
    {
        private readonly string _expression;
        private int _position;

        public MathParser(string expression)
        {
            _expression = expression;
            _position = 0;
        }

        public double Parse()
        {
            double result = ParseExpression();

            SkipWhiteSpace();

            if (_position < _expression.Length)
            {
                throw new FormatException( $"Carácter inesperado '{_expression[_position]}'.");
            }

            return result;
        }

        private double ParseExpression()
        {
            double result = ParseTerm();

            while (true)
            {
                SkipWhiteSpace();

                if (Match('+'))
                {
                    result += ParseTerm();
                }
                else if (Match('-'))
                {
                    result -= ParseTerm();
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private double ParseTerm()
        {
            double result = ParseFactor();

            while (true)
            {
                SkipWhiteSpace();

                if (Match('*'))
                {
                    result *= ParseFactor();
                }
                else if (Match('/'))
                {
                    double divisor = ParseFactor();

                    if (divisor == 0)
                    {
                        throw new DivideByZeroException();
                    }

                    result /= divisor;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private double ParseFactor()
        {
            SkipWhiteSpace();

            if (Match('+'))
            {
                return ParseFactor();
            }

            if (Match('-'))
            {
                return -ParseFactor();
            }

            if (Match('('))
            {
                double result = ParseExpression();

                SkipWhiteSpace();

                if (!Match(')'))
                {
                    throw new FormatException(
                        "Falta cerrar el paréntesis.");
                }

                return result;
            }

            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhiteSpace();

            int start = _position;

            while (_position < _expression.Length &&
                   (char.IsDigit(_expression[_position]) ||
                    _expression[_position] == '.'))
            {
                _position++;
            }

            if (start == _position)
            {
                throw new FormatException("Se esperaba un número.");     
            }

            string numberText = _expression[start.._position];
               
            if (!double.TryParse(
                numberText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double number))
            {
                throw new FormatException(
                    $"Número inválido '{numberText}'.");
            }

            return number;
        }

        private bool Match(char expected)
        {
            if (_position < _expression.Length && _expression[_position] == expected)      
            {
                _position++;
                return true;
            }

            return false;
        }

        private void SkipWhiteSpace()
        {
            while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))     
            {
                _position++;
            }
        }
    }
}
using System.ComponentModel;

namespace MigracaoDados.Domain.Importacao;

public static class ValidationErrorTypeExtensions
{
    public static string GetDescription(this ValidationErrorType errorType)
    {
        var field = typeof(ValidationErrorType).GetField(errorType.ToString());
        var attribute = field?
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault();

        return attribute?.Description ?? errorType.ToString();
    }
}

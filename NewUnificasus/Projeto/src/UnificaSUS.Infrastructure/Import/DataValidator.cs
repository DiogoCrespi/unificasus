using UnificaSUS.Core.Import;

namespace UnificaSUS.Infrastructure.Import;

/// <summary>
/// Validador de dados importados
/// Realiza validações sem lançar exceções, retornando lista de erros
/// </summary>
public class DataValidator
{
    private readonly ILogger? _logger;

    public DataValidator(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Valida um registro de dados
    /// </summary>
    /// <param name="data">Dados a validar</param>
    /// <param name="metadata">Metadados da tabela</param>
    /// <returns>Resultado da validação</returns>
    public ValidationResult Validate(Dictionary<string, object?> data, ImportTableMetadata metadata)
    {
        var result = new ValidationResult { IsValid = true };

        // Valida chaves primárias
        foreach (var column in metadata.Columns.Where(c => c.IsPrimaryKey))
        {
            if (!data.ContainsKey(column.ColumnName) || data[column.ColumnName] == null)
            {
                result.IsValid = false;
                result.Errors.Add($"Chave primária '{column.ColumnName}' não pode ser nula");
            }
            else if (data[column.ColumnName] is string str && string.IsNullOrWhiteSpace(str))
            {
                result.IsValid = false;
                result.Errors.Add($"Chave primária '{column.ColumnName}' não pode ser vazia");
            }
        }

        // Valida colunas obrigatórias
        foreach (var column in metadata.Columns.Where(c => !c.AllowNull))
        {
            if (!data.ContainsKey(column.ColumnName) || data[column.ColumnName] == null)
            {
                result.IsValid = false;
                result.Errors.Add($"Coluna obrigatória '{column.ColumnName}' não pode ser nula");
            }
        }

        // Validações específicas de tipo
        foreach (var column in metadata.Columns)
        {
            if (!data.ContainsKey(column.ColumnName) || data[column.ColumnName] == null)
                continue;

            var value = data[column.ColumnName];

            switch (column.DataType.ToUpper())
            {
                case "NUMBER" or "NUMERIC":
                    if (value is not (int or decimal or double or float or long))
                    {
                        result.Warnings.Add($"Coluna '{column.ColumnName}' deveria ser numérica");
                    }
                    break;

                case "CHAR" or "VARCHAR2" or "VARCHAR":
                    if (value is string str && str.Length > column.Length)
                    {
                        result.Warnings.Add($"Coluna '{column.ColumnName}' excede tamanho máximo ({str.Length} > {column.Length})");
                    }
                    break;
            }
        }

        // Validações específicas para campos conhecidos
        ValidateKnownFields(data, result);

        if (!result.IsValid)
        {
            result.ErrorMessage = string.Join("; ", result.Errors);
        }

        return result;
    }

    /// <summary>
    /// Validações específicas para campos conhecidos (idade, datas, etc)
    /// </summary>
    private void ValidateKnownFields(Dictionary<string, object?> data, ValidationResult result)
    {
        // Valida DT_COMPETENCIA (formato AAAAMM)
        if (data.ContainsKey("DT_COMPETENCIA") && data["DT_COMPETENCIA"] is string competencia)
        {
            if (!string.IsNullOrWhiteSpace(competencia))
            {
                if (competencia.Length != 6)
                {
                    result.Warnings.Add($"DT_COMPETENCIA com formato inválido: '{competencia}' (esperado AAAAMM)");
                }
                else if (!int.TryParse(competencia, out _))
                {
                    result.Warnings.Add($"DT_COMPETENCIA não numérica: '{competencia}'");
                }
            }
        }

        // Valida idades (VL_IDADE_MINIMA, VL_IDADE_MAXIMA)
        ValidateAgeRange(data, result, "VL_IDADE_MINIMA", "VL_IDADE_MAXIMA");

        // Valida valores monetários (não podem ser negativos)
        ValidateMonetaryValue(data, result, "VL_SH");
        ValidateMonetaryValue(data, result, "VL_SA");
        ValidateMonetaryValue(data, result, "VL_SP");
    }

    /// <summary>
    /// Valida range de idade
    /// </summary>
    private void ValidateAgeRange(Dictionary<string, object?> data, ValidationResult result, string minField, string maxField)
    {
        if (data.ContainsKey(minField) && data.ContainsKey(maxField))
        {
            var min = data[minField];
            var max = data[maxField];

            if (min is int minAge && max is int maxAge)
            {
                // 9999 significa "não se aplica"
                if (minAge != 9999 && maxAge != 9999)
                {
                    if (minAge > maxAge)
                    {
                        result.Warnings.Add($"Idade mínima ({minAge}) maior que máxima ({maxAge})");
                    }

                    if (minAge < 0 || maxAge < 0)
                    {
                        result.Warnings.Add($"Idade negativa não permitida (min: {minAge}, max: {maxAge})");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Valida valor monetário
    /// </summary>
    private void ValidateMonetaryValue(Dictionary<string, object?> data, ValidationResult result, string field)
    {
        if (data.ContainsKey(field) && data[field] is decimal value)
        {
            if (value < 0)
            {
                result.Warnings.Add($"Valor monetário {field} negativo: {value}");
            }
        }
    }
}

/// <summary>
/// Resultado da validação de dados
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indica se os dados são válidos
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Lista de erros críticos
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Lista de avisos (não impedem importação)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Mensagem de erro combinada
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

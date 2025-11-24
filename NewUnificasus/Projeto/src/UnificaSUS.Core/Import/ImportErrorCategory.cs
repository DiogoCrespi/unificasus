namespace UnificaSUS.Core.Import;

/// <summary>
/// Categorias de erros de importação para facilitar identificação e correção
/// </summary>
public enum ImportErrorCategory
{
    /// <summary>
    /// Erro desconhecido ou não categorizado
    /// </summary>
    Unknown,

    /// <summary>
    /// Tabela não encontrada no banco de dados
    /// </summary>
    TableNotFound,

    /// <summary>
    /// Coluna não encontrada na tabela
    /// </summary>
    ColumnNotFound,

    /// <summary>
    /// Truncamento de string (valor excede tamanho da coluna)
    /// </summary>
    StringTruncation,

    /// <summary>
    /// Violação de constraint (chave primária, única, etc.)
    /// </summary>
    ConstraintViolation,

    /// <summary>
    /// Erro de tipo de dado (conversão inválida)
    /// </summary>
    DataTypeError,

    /// <summary>
    /// Violação de chave estrangeira (referência inválida)
    /// </summary>
    ForeignKeyViolation,

    /// <summary>
    /// Erro de encoding/caracteres inválidos
    /// </summary>
    EncodingError,

    /// <summary>
    /// Erro de validação de dados (dados inválidos)
    /// </summary>
    ValidationError,

    /// <summary>
    /// Erro de conexão com banco de dados
    /// </summary>
    ConnectionError,

    /// <summary>
    /// Erro de permissão/acesso
    /// </summary>
    PermissionError,

    /// <summary>
    /// Overflow numérico
    /// </summary>
    NumericOverflow
}

/// <summary>
/// Helper para categorizar erros de importação baseado na mensagem de erro
/// </summary>
public static class ImportErrorCategorizer
{
    /// <summary>
    /// Categoriza um erro baseado na mensagem de exceção
    /// </summary>
    public static ImportErrorCategory CategorizeError(Exception exception)
    {
        if (exception == null)
            return ImportErrorCategory.Unknown;

        var message = exception.Message.ToUpperInvariant();
        var innerMessage = exception.InnerException?.Message?.ToUpperInvariant() ?? "";

        // Tabela não encontrada
        if (message.Contains("TABLE UNKNOWN") || 
            message.Contains("TABLE NOT FOUND") ||
            message.Contains("SQL ERROR CODE = -204") ||
            message.Contains("OBJECT NOT FOUND") && message.Contains("TABLE"))
        {
            return ImportErrorCategory.TableNotFound;
        }

        // Coluna não encontrada
        if (message.Contains("COLUMN UNKNOWN") ||
            message.Contains("COLUMN NOT FOUND") ||
            message.Contains("SQL ERROR CODE = -206") ||
            message.Contains("INVALID COLUMN") ||
            (message.Contains("OBJECT NOT FOUND") && message.Contains("COLUMN")))
        {
            return ImportErrorCategory.ColumnNotFound;
        }

        // Truncamento de string
        if (message.Contains("STRING TRUNCATION") ||
            message.Contains("STRING RIGHT TRUNCATION") ||
            message.Contains("STRING LEFT TRUNCATION") ||
            message.Contains("VALUE TOO LONG"))
        {
            return ImportErrorCategory.StringTruncation;
        }

        // Overflow numérico
        if (message.Contains("NUMERIC OVERFLOW") ||
            message.Contains("ARITHMETIC EXCEPTION") ||
            message.Contains("VALUE OUT OF RANGE"))
        {
            return ImportErrorCategory.NumericOverflow;
        }

        // Violação de constraint
        if (message.Contains("VIOLATION OF") ||
            message.Contains("PRIMARY KEY") ||
            message.Contains("UNIQUE CONSTRAINT") ||
            message.Contains("CHECK CONSTRAINT") ||
            message.Contains("SQL ERROR CODE = -803") ||
            message.Contains("SQL ERROR CODE = -530"))
        {
            return ImportErrorCategory.ConstraintViolation;
        }

        // Violação de chave estrangeira
        if (message.Contains("FOREIGN KEY") ||
            message.Contains("REFERENTIAL INTEGRITY") ||
            message.Contains("SQL ERROR CODE = -530") ||
            message.Contains("INTEGRITY CONSTRAINT"))
        {
            return ImportErrorCategory.ForeignKeyViolation;
        }

        // Erro de tipo de dado
        if (message.Contains("INVALID DATA TYPE") ||
            message.Contains("TYPE MISMATCH") ||
            message.Contains("CONVERSION ERROR") ||
            message.Contains("CANNOT CONVERT"))
        {
            return ImportErrorCategory.DataTypeError;
        }

        // Erro de encoding
        if (message.Contains("INVALID CHARACTER") ||
            message.Contains("ENCODING") ||
            message.Contains("CHARSET") ||
            message.Contains("UTF") ||
            message.Contains("ISO-8859"))
        {
            return ImportErrorCategory.EncodingError;
        }

        // Erro de conexão
        if (message.Contains("CONNECTION") ||
            message.Contains("NETWORK") ||
            message.Contains("TIMEOUT") ||
            message.Contains("UNABLE TO CONNECT"))
        {
            return ImportErrorCategory.ConnectionError;
        }

        // Erro de permissão
        if (message.Contains("PERMISSION") ||
            message.Contains("ACCESS DENIED") ||
            message.Contains("UNAUTHORIZED") ||
            message.Contains("INSUFFICIENT PRIVILEGE"))
        {
            return ImportErrorCategory.PermissionError;
        }

        // Erro de validação
        if (message.Contains("VALIDATION") ||
            message.Contains("INVALID DATA") ||
            message.Contains("INVALID VALUE"))
        {
            return ImportErrorCategory.ValidationError;
        }

        return ImportErrorCategory.Unknown;
    }

    /// <summary>
    /// Obtém uma descrição amigável da categoria de erro
    /// </summary>
    public static string GetCategoryDescription(ImportErrorCategory category)
    {
        return category switch
        {
            ImportErrorCategory.Unknown => "Erro desconhecido",
            ImportErrorCategory.TableNotFound => "Tabela não encontrada",
            ImportErrorCategory.ColumnNotFound => "Coluna não encontrada",
            ImportErrorCategory.StringTruncation => "Truncamento de string",
            ImportErrorCategory.ConstraintViolation => "Violação de constraint",
            ImportErrorCategory.DataTypeError => "Erro de tipo de dado",
            ImportErrorCategory.ForeignKeyViolation => "Violação de chave estrangeira",
            ImportErrorCategory.EncodingError => "Erro de encoding",
            ImportErrorCategory.ValidationError => "Erro de validação",
            ImportErrorCategory.ConnectionError => "Erro de conexão",
            ImportErrorCategory.PermissionError => "Erro de permissão",
            ImportErrorCategory.NumericOverflow => "Overflow numérico",
            _ => "Erro desconhecido"
        };
    }

    /// <summary>
    /// Obtém sugestão de correção baseada na categoria
    /// </summary>
    public static string GetCorrectionSuggestion(ImportErrorCategory category)
    {
        return category switch
        {
            ImportErrorCategory.TableNotFound => "Verificar se a tabela existe ou será criada automaticamente",
            ImportErrorCategory.ColumnNotFound => "Verificar se a coluna existe ou será adicionada automaticamente",
            ImportErrorCategory.StringTruncation => "String será truncada automaticamente",
            ImportErrorCategory.ConstraintViolation => "Verificar dados duplicados ou valores inválidos",
            ImportErrorCategory.DataTypeError => "Verificar formato dos dados",
            ImportErrorCategory.ForeignKeyViolation => "Verificar se tabelas relacionadas existem e têm os dados referenciados",
            ImportErrorCategory.EncodingError => "Encoding será corrigido automaticamente",
            ImportErrorCategory.ValidationError => "Verificar dados de entrada",
            ImportErrorCategory.ConnectionError => "Verificar conexão com banco de dados",
            ImportErrorCategory.PermissionError => "Verificar permissões do usuário do banco",
            ImportErrorCategory.NumericOverflow => "Verificar valores numéricos que excedem o limite",
            _ => "Analisar mensagem de erro para mais detalhes"
        };
    }
}


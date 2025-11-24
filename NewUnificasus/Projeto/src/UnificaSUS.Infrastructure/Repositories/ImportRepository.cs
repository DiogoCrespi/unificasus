using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using System.Text;
using UnificaSUS.Core.Import;
using UnificaSUS.Infrastructure.Data;

namespace UnificaSUS.Infrastructure.Repositories;

/// <summary>
/// Reposit ório para importação dinâmica de dados SIGTAP no Firebird
/// Gera SQL dinamicamente baseado em metadadosde tabelas
/// </summary>
public class ImportRepository
{
    private readonly FirebirdContext _context;
    private readonly ILogger? _logger;
    private static Encoding? _cachedEncoding = null;
    private static readonly object _encodingLock = new object();

    public ImportRepository(FirebirdContext context, ILogger? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Insere ou atualiza um registro no banco
    /// </summary>
    public async Task<bool> InsertOrUpdateAsync(
        string tableName,
        Dictionary<string, object?> data,
        ImportTableMetadata metadata,
        DuplicateHandlingMode duplicateMode = DuplicateHandlingMode.Update,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.OpenAsync(cancellationToken);

            // IMPORTANTE: Se as chaves primárias não foram marcadas no metadata, identifica e marca agora
            // Isso garante que a verificação de duplicatas funcione corretamente
            var primaryKeys = metadata.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (!primaryKeys.Any())
            {
                // Identifica chaves primárias usando heurística e marca no metadata
                var identifiedKeys = IdentifyPrimaryKeys(metadata.Columns);
                foreach (var column in metadata.Columns)
                {
                    column.IsPrimaryKey = identifiedKeys.Contains(column.ColumnName);
                }
                primaryKeys = metadata.Columns.Where(c => c.IsPrimaryKey).ToList();
            }
            
            if (primaryKeys.Any())
            {
                bool exists = await RecordExistsAsync(tableName, data, primaryKeys, cancellationToken);

                if (exists)
                {
                    switch (duplicateMode)
                    {
                        case DuplicateHandlingMode.Ignore:
                            // Registro duplicado ignorado (silencioso)
                            return false;

                        case DuplicateHandlingMode.Update:
                            return await UpdateRecordAsync(tableName, data, metadata, cancellationToken);

                        case DuplicateHandlingMode.Error:
                            throw new InvalidOperationException($"Registro duplicado encontrado em {tableName}");
                    }
                }
            }

            // Se não existe, insere
            return await InsertRecordAsync(tableName, data, metadata, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorCategory = ImportErrorCategorizer.CategorizeError(ex);
            var categoryDesc = ImportErrorCategorizer.GetCategoryDescription(errorCategory);
            var suggestion = ImportErrorCategorizer.GetCorrectionSuggestion(errorCategory);
            
            _logger?.LogError(
                $"Erro ao inserir/atualizar em {tableName} [{categoryDesc}]: {ex.Message}. " +
                $"Sugestão: {suggestion}");
            throw;
        }
    }

    /// <summary>
    /// Insere múltiplos registros em lote (batch insert)
    /// </summary>
    public async Task<int> InsertBatchAsync(
        string tableName,
        List<Dictionary<string, object?>> dataList,
        ImportTableMetadata metadata,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        int successCount = 0;

        await _context.OpenAsync(cancellationToken);

        // Processa em lotes
        for (int i = 0; i < dataList.Count; i += batchSize)
        {
            var batch = dataList.Skip(i).Take(batchSize).ToList();

            using var transaction = _context.Connection!.BeginTransaction();
            
            try
            {
                foreach (var data in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    bool success = await InsertRecordAsync(tableName, data, metadata, cancellationToken);
                    if (success)
                        successCount++;
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger?.LogError($"Erro no lote {i / batchSize + 1} de {tableName}: {ex.Message}");
                throw;
            }
        }

        return successCount;
    }

    /// <summary>
    /// Verifica se um registro já existe baseado nas chaves primárias
    /// </summary>
    private async Task<bool> RecordExistsAsync(
        string tableName,
        Dictionary<string, object?> data,
        List<ImportColumnMetadata> primaryKeys,
        CancellationToken cancellationToken)
    {
        var whereConditions = new List<string>();
        
        foreach (var pk in primaryKeys)
        {
            whereConditions.Add($"{pk.ColumnName} = @{pk.ColumnName}");
        }

        var sql = $@"
            SELECT COUNT(*)
            FROM {tableName}
            WHERE {string.Join(" AND ", whereConditions)}";

        using var command = new FbCommand(sql, _context.Connection);
        
        foreach (var pk in primaryKeys)
        {
            var value = data.ContainsKey(pk.ColumnName) ? data[pk.ColumnName] : null;
            command.Parameters.AddWithValue($"@{pk.ColumnName}", value ?? DBNull.Value);
        }

        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Erro ao verificar existência de registro em {tableName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Insere um registro no banco
    /// </summary>
    private async Task<bool> InsertRecordAsync(
        string tableName,
        Dictionary<string, object?> data,
        ImportTableMetadata metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtém colunas existentes no banco para filtrar
            var existingColumns = await GetExistingColumnsAsync(tableName, cancellationToken);
            var existingColumnsUpper = existingColumns.Select(c => c.ToUpper()).ToHashSet();
            
            // Filtra apenas colunas que existem nos metadados, têm dados E existem no banco
            var validColumns = metadata.Columns
                .Where(c => data.ContainsKey(c.ColumnName) && existingColumnsUpper.Contains(c.ColumnName.ToUpper()))
                .ToList();

            if (!validColumns.Any())
            {
                _logger?.LogWarning($"Nenhuma coluna válida para inserir em {tableName}");
                return false;
            }
            
            // Log de colunas que foram filtradas (não existem no banco)
            var filteredColumns = metadata.Columns
                .Where(c => data.ContainsKey(c.ColumnName) && !existingColumnsUpper.Contains(c.ColumnName.ToUpper()))
                .ToList();
            if (filteredColumns.Any())
            {
                _logger?.LogWarning($"Colunas filtradas (não existem no banco) em {tableName}: {string.Join(", ", filteredColumns.Select(c => c.ColumnName))}");
            }

            // CONVERTER descrições para MAIÚSCULAS antes de inserir
            // Aplica para: RL_PROCEDIMENTO_CID (NO_CID) e RL_PROCEDIMENTO_OCUPACAO (NO_OCUPACAO)
            string? insertDescriptionColumn = null;
            if (tableName.Equals("RL_PROCEDIMENTO_CID", StringComparison.OrdinalIgnoreCase))
            {
                insertDescriptionColumn = "NO_CID";
            }
            else if (tableName.Equals("RL_PROCEDIMENTO_OCUPACAO", StringComparison.OrdinalIgnoreCase))
            {
                insertDescriptionColumn = "NO_OCUPACAO";
            }
            
            // Converte descrição para MAIÚSCULAS se existir
            if (insertDescriptionColumn != null && data.ContainsKey(insertDescriptionColumn) && data[insertDescriptionColumn] is string insertDescValue)
            {
                var upperInsertDesc = insertDescValue.ToUpper();
                if (insertDescValue != upperInsertDesc)
                {
                    _logger?.LogDebug(
                        $"Convertendo {insertDescriptionColumn} para MAIÚSCULAS na inserção em {tableName}: " +
                        $"'{insertDescValue.Substring(0, Math.Min(50, insertDescValue.Length))}...' -> " +
                        $"'{upperInsertDesc.Substring(0, Math.Min(50, upperInsertDesc.Length))}...'");
                    data[insertDescriptionColumn] = upperInsertDesc;
                }
            }

            // Valida e ajusta tamanho de strings antes de inserir
            // IMPORTANTE: Firebird conta BYTES, não caracteres. Caracteres acentuados ocupam mais bytes.
            foreach (var column in validColumns)
            {
                if (data.ContainsKey(column.ColumnName) && data[column.ColumnName] is string strValue)
                {
                    // Obtém tamanho real da coluna no banco (se disponível)
                    var dbColumnLength = await GetColumnLengthAsync(tableName, column.ColumnName, cancellationToken);
                    
                    // Usa o menor tamanho entre layout e banco para garantir que não exceda
                    // Se não conseguir obter do banco, usa o layout menos 1 como margem de segurança
                    var maxBytes = 0;
                    if (dbColumnLength > 0 && column.Length > 0)
                    {
                        maxBytes = Math.Min(dbColumnLength, column.Length);
                    }
                    else if (dbColumnLength > 0)
                    {
                        maxBytes = dbColumnLength;
                    }
                    else if (column.Length > 0)
                    {
                        // Se não conseguiu obter do banco, usa layout menos 1 como segurança
                        maxBytes = column.Length - 1;
                    }
                    
                    // Verifica tamanho em BYTES (Firebird conta bytes, não caracteres)
                    if (maxBytes > 0)
                    {
                        // IMPORTANTE: O Firebird salva strings como UTF-8 (confirmado pelo teste de acentuação)
                        // UTF-8: caracteres acentuados ocupam 2 bytes (ex: Ç = 0xC3 0x87)
                        // Precisamos calcular o tamanho usando UTF-8 para corresponder ao que será salvo
                        var encoding = Encoding.UTF8;
                        var byteCount = encoding.GetByteCount(strValue);
                        
                        // Aplica margem de segurança para caracteres acentuados em UTF-8
                        // Cada caractere acentuado ocupa 2 bytes, então a margem precisa ser maior
                        var hasAccents = strValue.Any(c => "áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ".Contains(c));
                        // Em UTF-8, cada acento = 2 bytes, então margem de segurança maior
                        var safeMaxBytes = hasAccents && byteCount >= maxBytes 
                            ? Math.Max(1, maxBytes - 4)  // Margem de 4 bytes para acentos em UTF-8
                            : maxBytes;
                        
                        // Trunca se exceder o limite seguro
                        if (byteCount > safeMaxBytes)
                        {
                            _logger?.LogWarning(
                                $"String truncada na coluna {column.ColumnName} da tabela {tableName}: " +
                                $"{byteCount} bytes excede máximo seguro {safeMaxBytes} bytes (limite: {maxBytes} bytes, {strValue.Length} caracteres, acentos: {hasAccents}). " +
                                $"Valor original: '{strValue.Substring(0, Math.Min(50, strValue.Length))}...'");
                            
                            // Trunca baseado em bytes, não caracteres - usa o limite seguro
                            var truncated = TruncateStringByBytes(strValue, safeMaxBytes, encoding);
                            var truncatedByteCount = encoding.GetByteCount(truncated);
                            
                            // Verifica se o truncamento funcionou corretamente
                            if (truncatedByteCount > safeMaxBytes)
                            {
                                _logger?.LogError(
                                    $"ERRO: Truncamento falhou! Coluna {column.ColumnName}: " +
                                    $"truncado para {safeMaxBytes} bytes mas ainda tem {truncatedByteCount} bytes. " +
                                    $"Valor: '{truncated.Substring(0, Math.Min(50, truncated.Length))}...'");
                            }
                            
                            data[column.ColumnName] = truncated;
                        }
                        else if (byteCount >= maxBytes && hasAccents)
                        {
                            // Aplica margem preventiva para valores com acentos (silencioso)
                            var truncated = TruncateStringByBytes(strValue, safeMaxBytes, encoding);
                            data[column.ColumnName] = truncated;
                        }
                        // Valor com exatamente o tamanho máximo - não precisa de log
                    }
                }
            }

            var columnNames = string.Join(", ", validColumns.Select(c => c.ColumnName));
            var parameterNames = string.Join(", ", validColumns.Select(c => $"@{c.ColumnName}"));

            var sql = $@"
                INSERT INTO {tableName} ({columnNames})
                VALUES ({parameterNames})";

            using var command = new FbCommand(sql, _context.Connection);

            foreach (var column in validColumns)
            {
                var value = data[column.ColumnName];
                var dbValue = ConvertToDbValue(value, column, tableName);
                command.Parameters.AddWithValue($"@{column.ColumnName}", dbValue);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            var errorCategory = ImportErrorCategorizer.CategorizeError(ex);
            var categoryDesc = ImportErrorCategorizer.GetCategoryDescription(errorCategory);
            var suggestion = ImportErrorCategorizer.GetCorrectionSuggestion(errorCategory);
            
            // Log detalhado dos valores que causaram o erro (especialmente para truncamento)
            if (errorCategory == ImportErrorCategory.StringTruncation)
            {
                var encoding = Encoding.GetEncoding("ISO-8859-1");
                var problematicValues = new List<string>();
                
                // Usa metadata.Columns que está disponível no escopo do método
                foreach (var column in metadata.Columns)
                {
                    if (data.ContainsKey(column.ColumnName))
                    {
                        var value = data[column.ColumnName]?.ToString() ?? "";
                        var byteCount = encoding.GetByteCount(value);
                        if (column.Length > 0 && byteCount >= column.Length)
                        {
                            problematicValues.Add(
                                $"{column.ColumnName}: '{value}' ({value.Length} chars, {byteCount} bytes, limite: {column.Length}, tipo: {column.DataType})");
                        }
                    }
                }
                
                if (problematicValues.Any())
                {
                    _logger?.LogError(
                        $"Erro ao inserir registro em {tableName} [{categoryDesc}]: {ex.Message}. " +
                        $"Valores problemáticos: {string.Join("; ", problematicValues)}. " +
                        $"Sugestão: {suggestion}");
                }
                else
                {
                    _logger?.LogError(
                        $"Erro ao inserir registro em {tableName} [{categoryDesc}]: {ex.Message}. " +
                        $"Sugestão: {suggestion}");
                }
            }
            else
            {
                _logger?.LogError(
                    $"Erro ao inserir registro em {tableName} [{categoryDesc}]: {ex.Message}. " +
                    $"Sugestão: {suggestion}");
            }
            
            throw;
        }
    }

    /// <summary>
    /// Atualiza um registro existente
    /// </summary>
    private async Task<bool> UpdateRecordAsync(
        string tableName,
        Dictionary<string, object?> data,
        ImportTableMetadata metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            // Obtém colunas existentes no banco para filtrar
            var existingColumns = await GetExistingColumnsAsync(tableName, cancellationToken);
            var existingColumnsUpper = existingColumns.Select(c => c.ToUpper()).ToHashSet();
            
            // IMPORTANTE: Se as chaves primárias não foram marcadas no metadata, identifica e marca agora
            var primaryKeys = metadata.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (!primaryKeys.Any())
            {
                // Identifica chaves primárias usando heurística e marca no metadata
                var identifiedKeys = IdentifyPrimaryKeys(metadata.Columns);
                foreach (var column in metadata.Columns)
                {
                    column.IsPrimaryKey = identifiedKeys.Contains(column.ColumnName);
                }
                primaryKeys = metadata.Columns.Where(c => c.IsPrimaryKey).ToList();
            }
            
            // CONVERTER descrições para MAIÚSCULAS antes de inserir/atualizar
            // Aplica para: RL_PROCEDIMENTO_CID (NO_CID) e RL_PROCEDIMENTO_OCUPACAO (NO_OCUPACAO)
            string? descriptionColumn = null;
            if (tableName.Equals("RL_PROCEDIMENTO_CID", StringComparison.OrdinalIgnoreCase))
            {
                descriptionColumn = "NO_CID";
            }
            else if (tableName.Equals("RL_PROCEDIMENTO_OCUPACAO", StringComparison.OrdinalIgnoreCase))
            {
                descriptionColumn = "NO_OCUPACAO";
            }
            
            // Converte descrição para MAIÚSCULAS se existir
            if (descriptionColumn != null && data.ContainsKey(descriptionColumn) && data[descriptionColumn] is string descValue)
            {
                var upperDesc = descValue.ToUpper();
                if (descValue != upperDesc)
                {
                    _logger?.LogDebug(
                        $"Convertendo {descriptionColumn} para MAIÚSCULAS em {tableName}: " +
                        $"'{descValue.Substring(0, Math.Min(50, descValue.Length))}...' -> " +
                        $"'{upperDesc.Substring(0, Math.Min(50, upperDesc.Length))}...'");
                    data[descriptionColumn] = upperDesc;
                }
            }
            
            var updateColumns = metadata.Columns
                .Where(c => !c.IsPrimaryKey && data.ContainsKey(c.ColumnName) && existingColumnsUpper.Contains(c.ColumnName.ToUpper()))
                .ToList();

            if (!updateColumns.Any())
            {
                // Nenhuma coluna para atualizar (silencioso)
                return false;
            }

            // Valida e ajusta tamanho de strings antes de atualizar
            // IMPORTANTE: Firebird conta BYTES, não caracteres. Caracteres acentuados ocupam mais bytes.
            foreach (var column in updateColumns)
            {
                if (data.ContainsKey(column.ColumnName) && data[column.ColumnName] is string strValue)
                {
                    // Obtém tamanho real da coluna no banco (se disponível)
                    var dbColumnLength = await GetColumnLengthAsync(tableName, column.ColumnName, cancellationToken);
                    
                    // Usa o menor tamanho entre layout e banco para garantir que não exceda
                    // Se não conseguir obter do banco, usa o layout menos 1 como margem de segurança
                    var maxBytes = 0;
                    if (dbColumnLength > 0 && column.Length > 0)
                    {
                        maxBytes = Math.Min(dbColumnLength, column.Length);
                    }
                    else if (dbColumnLength > 0)
                    {
                        maxBytes = dbColumnLength;
                    }
                    else if (column.Length > 0)
                    {
                        // Se não conseguiu obter do banco, usa layout menos 1 como segurança
                        maxBytes = column.Length - 1;
                    }
                    
                    // Verifica tamanho em BYTES (Firebird conta bytes, não caracteres)
                    if (maxBytes > 0)
                    {
                        // IMPORTANTE: O Firebird salva strings como UTF-8 (confirmado pelo teste de acentuação)
                        // UTF-8: caracteres acentuados ocupam 2 bytes (ex: Ç = 0xC3 0x87)
                        // Precisamos calcular o tamanho usando UTF-8 para corresponder ao que será salvo
                        var encoding = Encoding.UTF8;
                        var byteCount = encoding.GetByteCount(strValue);
                        
                        // Aplica margem de segurança para caracteres acentuados em UTF-8
                        // Cada caractere acentuado ocupa 2 bytes, então a margem precisa ser maior
                        var hasAccents = strValue.Any(c => "áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ".Contains(c));
                        // Em UTF-8, cada acento = 2 bytes, então margem de segurança maior
                        var safeMaxBytes = hasAccents && byteCount >= maxBytes 
                            ? Math.Max(1, maxBytes - 4)  // Margem de 4 bytes para acentos em UTF-8
                            : maxBytes;
                        
                        // Trunca se exceder o limite seguro
                        if (byteCount > safeMaxBytes)
                        {
                            _logger?.LogWarning(
                                $"String truncada na coluna {column.ColumnName} da tabela {tableName}: " +
                                $"{byteCount} bytes excede máximo seguro {safeMaxBytes} bytes (limite: {maxBytes} bytes, {strValue.Length} caracteres, acentos: {hasAccents}). " +
                                $"Valor original: '{strValue.Substring(0, Math.Min(50, strValue.Length))}...'");
                            
                            // Trunca baseado em bytes, não caracteres - usa o limite seguro
                            var truncated = TruncateStringByBytes(strValue, safeMaxBytes, encoding);
                            var truncatedByteCount = encoding.GetByteCount(truncated);
                            
                            // Verifica se o truncamento funcionou corretamente
                            if (truncatedByteCount > safeMaxBytes)
                            {
                                _logger?.LogError(
                                    $"ERRO: Truncamento falhou! Coluna {column.ColumnName}: " +
                                    $"truncado para {safeMaxBytes} bytes mas ainda tem {truncatedByteCount} bytes. " +
                                    $"Valor: '{truncated.Substring(0, Math.Min(50, truncated.Length))}...'");
                            }
                            
                            data[column.ColumnName] = truncated;
                        }
                        else if (byteCount >= maxBytes && hasAccents)
                        {
                            // Aplica margem preventiva para valores com acentos (silencioso)
                            var truncated = TruncateStringByBytes(strValue, safeMaxBytes, encoding);
                            data[column.ColumnName] = truncated;
                        }
                        // Valor com exatamente o tamanho máximo - não precisa de log
                    }
                }
            }

            var setClause = string.Join(", ", updateColumns.Select(c => $"{c.ColumnName} = @{c.ColumnName}"));
            var whereClause = string.Join(" AND ", primaryKeys.Select(pk => $"{pk.ColumnName} = @PK_{pk.ColumnName}"));

            var sql = $@"
                UPDATE {tableName}
                SET {setClause}
                WHERE {whereClause}";

            using var command = new FbCommand(sql, _context.Connection);

            // Parâmetros para SET
            foreach (var column in updateColumns)
            {
                var value = data[column.ColumnName];
                var dbValue = ConvertToDbValue(value, column, tableName);
                command.Parameters.AddWithValue($"@{column.ColumnName}", dbValue);
            }

            // Parâmetros para WHERE
            foreach (var pk in primaryKeys)
            {
                var value = data.ContainsKey(pk.ColumnName) ? data[pk.ColumnName] : null;
                var dbValue = ConvertToDbValue(value, pk, tableName);
                command.Parameters.AddWithValue($"@PK_{pk.ColumnName}", dbValue);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            var errorCategory = ImportErrorCategorizer.CategorizeError(ex);
            var categoryDesc = ImportErrorCategorizer.GetCategoryDescription(errorCategory);
            var suggestion = ImportErrorCategorizer.GetCorrectionSuggestion(errorCategory);
            
            // Log detalhado dos valores que causaram o erro (especialmente para truncamento)
            if (errorCategory == ImportErrorCategory.StringTruncation)
            {
                var encoding = Encoding.GetEncoding("ISO-8859-1");
                var problematicValues = new List<string>();
                
                // Usa metadata.Columns que está disponível no escopo do método
                foreach (var column in metadata.Columns)
                {
                    if (data.ContainsKey(column.ColumnName))
                    {
                        var value = data[column.ColumnName]?.ToString() ?? "";
                        var byteCount = encoding.GetByteCount(value);
                        if (column.Length > 0 && byteCount >= column.Length)
                        {
                            problematicValues.Add(
                                $"{column.ColumnName}: '{value}' ({value.Length} chars, {byteCount} bytes, limite: {column.Length}, tipo: {column.DataType})");
                        }
                    }
                }
                
                if (problematicValues.Any())
                {
                    _logger?.LogError(
                        $"Erro ao atualizar registro em {tableName} [{categoryDesc}]: {ex.Message}. " +
                        $"Valores problemáticos: {string.Join("; ", problematicValues)}. " +
                        $"Sugestão: {suggestion}");
                }
                else
                {
                    _logger?.LogError(
                        $"Erro ao atualizar registro em {tableName} [{categoryDesc}]: {ex.Message}. " +
                        $"Sugestão: {suggestion}");
                }
            }
            else
            {
                _logger?.LogError(
                    $"Erro ao atualizar registro em {tableName} [{categoryDesc}]: {ex.Message}. " +
                    $"Sugestão: {suggestion}");
            }
            
            throw;
        }
    }

    /// <summary>
    /// Converte valor para tipo compatível com banco de dados
    /// </summary>
    private object ConvertToDbValue(object? value, ImportColumnMetadata column, string? tableName = null)
    {
        if (value == null)
            return DBNull.Value;

        // Se já é do tipo correto, retorna
        if (value is DBNull)
            return DBNull.Value;

        try
        {
            var dataType = column.DataType.ToUpper();
            
            if (dataType == "NUMBER" || dataType == "NUMERIC")
            {
                return value is decimal || value is int || value is long || value is double
                    ? value
                    : DBNull.Value;
            }
            
            if (dataType == "CHAR" || dataType == "VARCHAR2" || dataType == "VARCHAR")
            {
                var strValue = value?.ToString();
                if (strValue == null)
                    return DBNull.Value;
                
                    // Garante truncamento se necessário (última linha de defesa)
                // Firebird conta BYTES, não caracteres
                if (column.Length > 0)
                {
                    // IMPORTANTE: O Firebird salva strings como UTF-8 (confirmado pelo teste de acentuação)
                    // UTF-8: caracteres acentuados ocupam 2 bytes (ex: Ç = 0xC3 0x87)
                    // Precisamos calcular o tamanho usando UTF-8 para corresponder ao que será salvo
                    var encoding = Encoding.UTF8;
                    var byteCount = encoding.GetByteCount(strValue);
                    
                    // Aplica margem de segurança para caracteres acentuados em UTF-8
                    // Cada caractere acentuado ocupa 2 bytes, então a margem precisa ser maior
                    var hasAccents = strValue.Any(c => "áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ".Contains(c));
                    // Em UTF-8, cada acento = 2 bytes, então margem de segurança maior
                    var safeMaxBytes = hasAccents && byteCount >= column.Length 
                        ? Math.Max(1, column.Length - 4)  // Margem de 4 bytes para acentos em UTF-8
                        : column.Length;
                    
                    // Trunca se exceder o limite seguro
                    if (byteCount > safeMaxBytes)
                    {
                        _logger?.LogWarning($"Truncamento final na coluna {column.ColumnName} (tabela {tableName}): {byteCount} bytes ({strValue.Length} caracteres, acentos: {hasAccents}) -> {safeMaxBytes} bytes (limite: {column.Length})");
                        var truncated = TruncateStringByBytes(strValue, safeMaxBytes, encoding);
                        var truncatedByteCount = encoding.GetByteCount(truncated);
                        
                        // Verifica se o truncamento funcionou
                        if (truncatedByteCount > safeMaxBytes)
                        {
                            _logger?.LogError($"ERRO: Truncamento final falhou! Coluna {column.ColumnName}: ainda tem {truncatedByteCount} bytes após truncar para {safeMaxBytes}");
                        }
                        
                        return truncated;
                    }
                    else if (byteCount == column.Length && hasAccents)
                    {
                        // Aplica margem preventiva para valores com exatamente o tamanho máximo e acentos
                        // Aplicando margem preventiva para valores com acentos (silencioso)
                        return TruncateStringByBytes(strValue, safeMaxBytes, encoding);
                    }
                }
                
                return strValue;
            }
            
            if (dataType == "DATE")
            {
                return value?.ToString() is not null ? value.ToString()! : DBNull.Value;
            }
            
            return value;
        }
        catch
        {
            _logger?.LogWarning($"Erro ao converter valor para {column.DataType}, usando DBNull");
            return DBNull.Value;
        }
    }

    /// <summary>
    /// Limpa dados de uma tabela para uma competência específica
    /// </summary>
    public async Task<int> DeleteByCompetenciaAsync(
        string tableName,
        string competencia,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.OpenAsync(cancellationToken);

            var sql = $"DELETE FROM {tableName} WHERE DT_COMPETENCIA = @competencia";

            using var command = new FbCommand(sql, _context.Connection);
            command.Parameters.AddWithValue("@competencia", competencia);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            
            _logger?.LogInformation($"Deletados {rowsAffected} registros de {tableName} (competência {competencia})");
            
            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao deletar dados de {tableName}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Obtém contagem de registros de uma tabela
    /// </summary>
    public async Task<int> GetRecordCountAsync(
        string tableName,
        string? competencia = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.OpenAsync(cancellationToken);

            var sql = competencia != null
                ? $"SELECT COUNT(*) FROM {tableName} WHERE DT_COMPETENCIA = @competencia"
                : $"SELECT COUNT(*) FROM {tableName}";

            using var command = new FbCommand(sql, _context.Connection);
            
            if (competencia != null)
            {
                command.Parameters.AddWithValue("@competencia", competencia);
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao contar registros de {tableName}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Verifica se uma tabela existe no banco de dados
    /// </summary>
    public async Task<bool> TableExistsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.OpenAsync(cancellationToken);

            // Query para verificar se a tabela existe no Firebird
            var sql = @"
                SELECT COUNT(*) 
                FROM RDB$RELATIONS 
                WHERE RDB$RELATION_NAME = UPPER(@tableName) 
                AND RDB$SYSTEM_FLAG = 0";

            using var command = new FbCommand(sql, _context.Connection);
            command.Parameters.AddWithValue("@tableName", tableName);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao verificar existência da tabela {tableName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifica se múltiplas tabelas existem no banco de dados
    /// Retorna um dicionário com o nome da tabela e se ela existe
    /// </summary>
    public async Task<Dictionary<string, bool>> CheckTablesExistAsync(
        IEnumerable<string> tableNames,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, bool>();
        
        foreach (var tableName in tableNames)
        {
            var exists = await TableExistsAsync(tableName, cancellationToken);
            result[tableName] = exists;
        }
        
        return result;
    }

    /// <summary>
    /// Cria uma tabela no banco de dados baseado nos metadados
    /// </summary>
    public async Task<bool> CreateTableAsync(
        ImportTableMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.OpenAsync(cancellationToken);

            // Verifica se a tabela já existe
            if (await TableExistsAsync(metadata.TableName, cancellationToken))
            {
                _logger?.LogInformation($"Tabela {metadata.TableName} já existe, verificando colunas faltantes...");
                
                // Verifica e adiciona colunas faltantes
                await AddMissingColumnsAsync(metadata, cancellationToken);
                
                // Ajusta tamanho de colunas que ficaram menores que o layout original
                await EnsureColumnLengthsAsync(metadata, cancellationToken);
                return true;
            }

            _logger?.LogInformation($"Criando tabela {metadata.TableName}...");

            // Identifica chaves primárias (heurística: primeira coluna que começa com CO_ geralmente é PK)
            var primaryKeys = IdentifyPrimaryKeys(metadata.Columns);
            
            // Constrói SQL CREATE TABLE
            var columnDefinitions = new List<string>();
            
            foreach (var column in metadata.Columns)
            {
                var sqlType = ConvertToFirebirdType(column);
                var nullable = column.AllowNull && !primaryKeys.Contains(column.ColumnName) ? "" : " NOT NULL";
                
                // Não adiciona PRIMARY KEY aqui, será adicionado no final se houver
                columnDefinitions.Add($"{column.ColumnName} {sqlType}{nullable}");
            }

            // Adiciona constraint de chave primária se houver
            var primaryKeyConstraint = "";
            if (primaryKeys.Any())
            {
                var pkColumns = string.Join(", ", primaryKeys);
                primaryKeyConstraint = $",\n                    CONSTRAINT PK_{metadata.TableName} PRIMARY KEY ({pkColumns})";
            }

            var createTableSql = $@"
                CREATE TABLE {metadata.TableName} (
                    {string.Join(",\n                    ", columnDefinitions)}{primaryKeyConstraint}
                )";

            using var command = new FbCommand(createTableSql, _context.Connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger?.LogInformation($"Tabela {metadata.TableName} criada com sucesso!");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao criar tabela {metadata.TableName}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Identifica chaves primárias baseado em heurísticas
    /// Padrões identificados:
    /// - Tabelas simples (TB_*): Primeira coluna CO_* é PK única
    /// - Tabelas relacionais (RL_*): Primeiras 2-3 colunas CO_* são PK composta
    /// - Tabelas com chaves compostas: Múltiplas colunas CO_* no início
    /// </summary>
    private List<string> IdentifyPrimaryKeys(List<ImportColumnMetadata> columns)
    {
        var primaryKeys = new List<string>();

        if (!columns.Any())
            return primaryKeys;

        var coColumns = columns.Where(c => 
            c.ColumnName.StartsWith("CO_", StringComparison.OrdinalIgnoreCase)).ToList();

        // Heurística 1: Para tabelas relacionais (RL_*), geralmente tem PK composta
        // Ex: RL_PROCEDIMENTO_CID: CO_PROCEDIMENTO + CO_CID + DT_COMPETENCIA (IMPORTANTE: inclui competência!)
        // Ex: RL_PROCEDIMENTO_RENASES: CO_PROCEDIMENTO + CO_RENASES + DT_COMPETENCIA
        // Ex: RL_PROCEDIMENTO_SERVICO: CO_PROCEDIMENTO + CO_SERVICO + CO_CLASSIFICACAO + DT_COMPETENCIA
        if (coColumns.Count >= 2)
        {
            // Para tabelas relacionais, pega as primeiras 2-3 colunas CO_ que aparecem no início
            var firstCoColumns = columns
                .Where(c => c.ColumnName.StartsWith("CO_", StringComparison.OrdinalIgnoreCase))
                .Take(3) // Máximo 3 colunas CO_ para PK composta
                .Select(c => c.ColumnName)
                .ToList();
            
            // IMPORTANTE: Se a tabela tem DT_COMPETENCIA, inclui na chave primária lógica
            // Isso previne duplicatas entre competências diferentes
            var dtCompetenciaColumn = columns.FirstOrDefault(c => 
                c.ColumnName.Equals("DT_COMPETENCIA", StringComparison.OrdinalIgnoreCase));
            
            if (dtCompetenciaColumn != null && !firstCoColumns.Contains("DT_COMPETENCIA"))
            {
                firstCoColumns.Add("DT_COMPETENCIA");
            }
            
            if (firstCoColumns.Any())
            {
                primaryKeys.AddRange(firstCoColumns);
                return primaryKeys;
            }
        }

        // Heurística 2: Para tabelas simples (TB_*), primeira coluna CO_ é PK única
        // Ex: TB_RENASES: CO_RENASES
        // Ex: TB_CID: CO_CID
        // Ex: TB_HABILITACAO: CO_HABILITACAO
        var firstCoColumn = coColumns.FirstOrDefault();
        if (firstCoColumn != null)
        {
            primaryKeys.Add(firstCoColumn.ColumnName);
            return primaryKeys;
        }

        // Heurística 3: Se não encontrou CO_, usa a primeira coluna como PK
        primaryKeys.Add(columns[0].ColumnName);

        return primaryKeys;
    }

    /// <summary>
    /// Verifica se uma coluna existe em uma tabela
    /// </summary>
    private async Task<bool> ColumnExistsAsync(
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        try
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM RDB$RELATION_FIELDS 
                WHERE RDB$RELATION_NAME = UPPER(@tableName) 
                AND RDB$FIELD_NAME = UPPER(@columnName)";
            
            using var command = new FbCommand(sql, _context.Connection);
            command.Parameters.AddWithValue("@tableName", tableName);
            command.Parameters.AddWithValue("@columnName", columnName);
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Erro ao verificar existência da coluna {columnName} em {tableName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtém lista de colunas existentes em uma tabela
    /// </summary>
    private async Task<List<string>> GetExistingColumnsAsync(
        string tableName,
        CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        
        try
        {
            var sql = @"
                SELECT RDB$FIELD_NAME 
                FROM RDB$RELATION_FIELDS 
                WHERE RDB$RELATION_NAME = UPPER(@tableName)
                ORDER BY RDB$FIELD_POSITION";
            
            using var command = new FbCommand(sql, _context.Connection);
            command.Parameters.AddWithValue("@tableName", tableName);
            
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var columnName = reader.GetString(0).Trim();
                columns.Add(columnName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Erro ao obter colunas de {tableName}: {ex.Message}");
        }
        
        return columns;
    }

    /// <summary>
    /// Obtém o tamanho real de uma coluna no banco de dados
    /// </summary>
    private async Task<int> GetColumnLengthAsync(
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Firebird armazena nomes de colunas com espaços à direita, então precisamos usar TRIM
            var sql = @"
                SELECT F.RDB$FIELD_LENGTH
                FROM RDB$RELATION_FIELDS RF
                JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
                WHERE TRIM(RF.RDB$RELATION_NAME) = UPPER(@tableName)
                AND TRIM(RF.RDB$FIELD_NAME) = UPPER(@columnName)";
            
            using var command = new FbCommand(sql, _context.Connection);
            command.Parameters.AddWithValue("@tableName", tableName);
            command.Parameters.AddWithValue("@columnName", columnName);
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result != null && result != DBNull.Value)
            {
                var length = Convert.ToInt32(result);
                // Tamanho da coluna obtido (silencioso)
                return length;
            }
        }
        catch
        {
            // Erro ao obter tamanho da coluna (silencioso - usa tamanho do layout)
        }
        
        return 0; // Retorna 0 se não conseguir obter (usa tamanho do layout)
    }

    /// <summary>
    /// Adiciona colunas faltantes em uma tabela existente
    /// </summary>
    private async Task AddMissingColumnsAsync(
        ImportTableMetadata metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingColumns = await GetExistingColumnsAsync(metadata.TableName, cancellationToken);
            // Normaliza nomes de colunas para comparação (case-insensitive)
            var existingColumnsUpper = existingColumns.Select(c => c.ToUpper()).ToHashSet();
            var missingColumns = metadata.Columns
                .Where(c => !existingColumnsUpper.Contains(c.ColumnName.ToUpper()))
                .ToList();
            
            if (!missingColumns.Any())
            {
                // Tabela já possui todas as colunas necessárias (silencioso)
                return;
            }
            
            _logger?.LogInformation($"Encontradas {missingColumns.Count} colunas faltantes em {metadata.TableName}: {string.Join(", ", missingColumns.Select(c => c.ColumnName))}");
            
            // Identifica chaves primárias para não torná-las nullable se forem PK
            var primaryKeys = IdentifyPrimaryKeys(metadata.Columns);
            
            foreach (var column in missingColumns)
            {
                try
                {
                    var sqlType = ConvertToFirebirdType(column);
                    // No Firebird, para permitir NULL, simplesmente não especificamos NOT NULL
                    // Para NOT NULL, especificamos explicitamente
                    var nullable = column.AllowNull && !primaryKeys.Contains(column.ColumnName) ? "" : " NOT NULL";
                    
                    // Gera SQL em uma única linha para evitar problemas de parsing
                    var alterTableSql = $"ALTER TABLE {metadata.TableName} ADD {column.ColumnName} {sqlType}{nullable}";
                    
                    // Executando ALTER TABLE (silencioso)
                    
                    using var command = new FbCommand(alterTableSql, _context.Connection);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    
                    _logger?.LogInformation($"Coluna {column.ColumnName} adicionada à tabela {metadata.TableName}");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Erro ao adicionar coluna {column.ColumnName} em {metadata.TableName}: {ex.Message}");
                    // Continua com próxima coluna
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao verificar/adicionar colunas faltantes em {metadata.TableName}: {ex.Message}");
            // Não lança exceção, apenas loga o erro
        }
    }

    /// <summary>
    /// Ajusta o tamanho das colunas existentes para garantir que atendam ao layout
    /// </summary>
    private async Task EnsureColumnLengthsAsync(
        ImportTableMetadata metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var column in metadata.Columns)
            {
                if (column.Length <= 0)
                    continue;

                // Ajusta apenas colunas textuais (CHAR/VARCHAR)
                if (!column.DataType.Equals("CHAR", StringComparison.OrdinalIgnoreCase) &&
                    !column.DataType.Equals("VARCHAR2", StringComparison.OrdinalIgnoreCase) &&
                    !column.DataType.Equals("VARCHAR", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var existingLength = await GetColumnLengthAsync(metadata.TableName, column.ColumnName, cancellationToken);

                // Se o tamanho existente é 8, provavelmente é BLOB (Firebird retorna 8 para BLOB)
                // Pula essas colunas pois não podem ser alteradas
                if (existingLength == 8)
                {
                    // Coluna é BLOB, pulando ajuste de tamanho (silencioso)
                    continue;
                }

                if (existingLength > 0 && existingLength < column.Length)
                {
                    try
                    {
                        var sqlType = ConvertToFirebirdType(column);
                        var alterSql = $"ALTER TABLE {metadata.TableName} ALTER COLUMN {column.ColumnName} TYPE {sqlType}";

                        using var command = new FbCommand(alterSql, _context.Connection);
                        await command.ExecuteNonQueryAsync(cancellationToken);

                        _logger?.LogInformation($"Tamanho da coluna {column.ColumnName} em {metadata.TableName} ajustado de {existingLength} para {column.Length}");
                    }
                    catch (Exception ex)
                    {
                        // Se o erro for sobre BLOB/ARRAY, apenas loga como debug (não é um erro crítico)
                        if (ex.Message.Contains("BLOB", StringComparison.OrdinalIgnoreCase) || 
                            ex.Message.Contains("ARRAY", StringComparison.OrdinalIgnoreCase))
                        {
                            // Coluna é BLOB/ARRAY, não pode ser alterada (silencioso)
                        }
                        else
                        {
                            _logger?.LogWarning($"Erro ao ajustar tamanho da coluna {column.ColumnName} em {metadata.TableName}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Erro ao garantir tamanho das colunas em {metadata.TableName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém o encoding Windows-1252 de forma segura, com fallback para ISO-8859-1
    /// Usa cache estático para evitar múltiplas tentativas e logs repetidos
    /// </summary>
    private Encoding GetWindows1252Encoding()
    {
        // Se já está em cache, retorna imediatamente
        if (_cachedEncoding != null)
            return _cachedEncoding;

        // Thread-safe: apenas uma thread tenta obter o encoding
        lock (_encodingLock)
        {
            // Double-check: outra thread pode ter setado enquanto esperava
            if (_cachedEncoding != null)
                return _cachedEncoding;

            try
            {
                // Tenta obter Windows-1252 usando código de página (mais confiável)
                _cachedEncoding = Encoding.GetEncoding(1252);
                return _cachedEncoding;
            }
            catch
            {
                try
                {
                    // Tenta pelo nome
                    _cachedEncoding = Encoding.GetEncoding("Windows-1252");
                    return _cachedEncoding;
                }
                catch
                {
                    // Fallback para ISO-8859-1 (compatível para a maioria dos caracteres brasileiros)
                    // ISO-8859-1 e Windows-1252 são idênticos para a maioria dos caracteres acentuados
                    // Log apenas uma vez (em nível debug, não warning)
                    // Windows-1252 não disponível, usando ISO-8859-1 como fallback (silencioso)
                    _cachedEncoding = Encoding.GetEncoding("ISO-8859-1");
                    return _cachedEncoding;
                }
            }
        }
    }

    /// <summary>
    /// Trunca uma string baseado no tamanho em bytes, não caracteres
    /// Firebird conta bytes, então caracteres acentuados podem exceder o limite
    /// </summary>
    private string TruncateStringByBytes(string value, int maxBytes, Encoding encoding)
    {
        if (string.IsNullOrEmpty(value) || maxBytes <= 0)
            return value ?? string.Empty;
        
        var bytes = encoding.GetBytes(value);
        if (bytes.Length <= maxBytes)
            return value;
        
        // Trunca os bytes
        var truncatedBytes = new byte[maxBytes];
        Array.Copy(bytes, truncatedBytes, maxBytes);
        
        // Tenta converter de volta para string
        // Se houver caracteres incompletos, remove-os iterativamente
        string result;
        int currentMax = maxBytes;
        
        do
        {
            var tempBytes = new byte[currentMax];
            Array.Copy(bytes, tempBytes, currentMax);
            result = encoding.GetString(tempBytes);
            
            // Remove caracteres de substituição no final (caracteres incompletos)
            while (result.Length > 0 && (result[result.Length - 1] == '\uFFFD' || result[result.Length - 1] == '?'))
            {
                result = result.Substring(0, result.Length - 1);
                currentMax--;
            }
            
            // Se ainda exceder, reduz mais um byte
            if (encoding.GetByteCount(result) > maxBytes && currentMax > 1)
            {
                currentMax--;
            }
            else
            {
                break;
            }
        } while (currentMax > 0);
        
        // Garante que o resultado final não exceda o limite
        var finalBytes = encoding.GetByteCount(result);
        if (finalBytes > maxBytes)
        {
            // Último recurso: trunca caractere por caractere até caber
            for (int i = result.Length - 1; i >= 0; i--)
            {
                var test = result.Substring(0, i);
                if (encoding.GetByteCount(test) <= maxBytes)
                {
                    return test;
                }
            }
            return string.Empty;
        }
        
        return result;
    }

    /// <summary>
    /// Converte tipo de dados do metadata para tipo SQL do Firebird
    /// </summary>
    private string ConvertToFirebirdType(ImportColumnMetadata column)
    {
        var dataType = column.DataType.ToUpper();
        var length = column.Length > 0 ? column.Length : 255;

        return dataType switch
        {
            "NUMBER" or "NUMERIC" => length <= 9 ? "INTEGER" : "BIGINT",
            "CHAR" => length <= 32767 ? $"CHAR({length})" : "BLOB SUB_TYPE TEXT",
            "VARCHAR2" or "VARCHAR" => length <= 32767 
                ? $"VARCHAR({length})" 
                : "BLOB SUB_TYPE TEXT",
            "DATE" => "DATE",
            "TIMESTAMP" => "TIMESTAMP",
            _ => length <= 32767 ? $"VARCHAR({length})" : "BLOB SUB_TYPE TEXT" // Default para string
        };
    }
}

/// <summary>
/// Modo de tratamento de duplicatas
/// </summary>
public enum DuplicateHandlingMode
{
    Ignore,
    Update,
    Error
}

using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using System.Text;
using UnificaSUS.Infrastructure.Data;

namespace UnificaSUS.Infrastructure.Helpers;

/// <summary>
/// Detecta o charset usado no banco de dados Firebird
/// </summary>
public class CharsetDetector
{
    private readonly FirebirdContext _context;
    private readonly ILogger<CharsetDetector>? _logger;
    private string? _detectedCharset = null;

    public CharsetDetector(FirebirdContext context, ILogger<CharsetDetector>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Detecta o charset padrão do banco ou de um campo específico
    /// </summary>
    public async Task<string?> DetectCharsetAsync(CancellationToken cancellationToken = default)
    {
        if (_detectedCharset != null)
            return _detectedCharset;

        try
        {
            await _context.OpenAsync(cancellationToken);

            // Tenta obter o charset do campo NO_PROCEDIMENTO da tabela TB_PROCEDIMENTO
            const string sql = @"
                SELECT 
                    CS.RDB$CHARACTER_SET_NAME
                FROM RDB$RELATION_FIELDS RF
                JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
                LEFT JOIN RDB$CHARACTER_SETS CS ON F.RDB$CHARACTER_SET_ID = CS.RDB$CHARACTER_SET_ID
                WHERE RF.RDB$RELATION_NAME = 'TB_PROCEDIMENTO'
                  AND RF.RDB$FIELD_NAME = 'NO_PROCEDIMENTO'";

            using var command = new FbCommand(sql, _context.Connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var charsetName = reader.IsDBNull(0) ? null : reader.GetString(0)?.Trim();
                _detectedCharset = charsetName;
                
                _logger?.LogInformation("Charset detectado: {Charset}", charsetName ?? "NONE");
                return charsetName;
            }

            // Se não encontrou, tenta obter o charset padrão do banco
            const string sqlDefault = @"
                SELECT RDB$CHARACTER_SET_NAME 
                FROM RDB$DATABASE";

            using var commandDefault = new FbCommand(sqlDefault, _context.Connection);
            using var readerDefault = await commandDefault.ExecuteReaderAsync(cancellationToken);

            if (await readerDefault.ReadAsync(cancellationToken))
            {
                var charsetName = readerDefault.IsDBNull(0) ? null : readerDefault.GetString(0)?.Trim();
                _detectedCharset = charsetName;
                
                _logger?.LogInformation("Charset padrão do banco: {Charset}", charsetName ?? "NONE");
                return charsetName;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Erro ao detectar charset do banco. Usando Windows-1252 como padrão.");
        }

        // Se não conseguir detectar, assume Windows-1252 (padrão para bancos brasileiros)
        _detectedCharset = "WIN1252";
        return _detectedCharset;
    }
}



Plano de Implementação: Funcionalidade de Importação Resiliente SIGTAP
Objetivo
Implementar uma funcionalidade robusta e resiliente para importar dados do SIGTAP (Sistema de Gerenciamento da Tabela de Procedimentos, Medicamentos e OPM do SUS) no UnificaSUS, substituindo a implementação antiga que apresentava erros de conversão ("conversion error from string").

Contexto do Problema
A aplicação antiga tinha problemas de importação devido a:

Erro principal: "Claupers UnificaSus conversion error from string"
Causas identificadas: Strings vazias, formatos inválidos, caracteres especiais, incompatibilidade de tipos
Falta de resiliência: Não se adaptava a mudanças na estrutura dos arquivos SIGTAP
Estrutura dos Dados SIGTAP
Os arquivos SIGTAP vêm em formato ZIP contendo:

87 arquivos de dados (.txt) com posições fixas
87 arquivos de layout (*_layout.txt) descrevendo a estrutura
Encoding: ISO-8859-1 (Latin-1)
Formato: Posições fixas (fixed-width)
Exemplo: TabelaUnificada_202510_v2510160954.zip
User Review Required
IMPORTANT

Abordagem Incremental e Resiliente

Este plano propõe uma implementação incremental e resiliente que:

Importa dados progressivamente (não tudo de uma vez)
Continua mesmo se encontrar erros em alguns registros
Loga problemas sem interromper o processo
Adapta-se a mudanças futuras na estrutura dos arquivos
Permite rollback em caso de problemas críticos
WARNING

Decisões de Design que Requerem Aprovação

Estratégia de Importação: Proposta de importação por tabela com transações independentes
Tratamento de Erros: Registros com erro são logados mas não impedem a importação
Validação de Dados: Validação flexível que aceita valores nulos/vazios quando apropriado
Encoding: Suporte a múltiplos encodings (ISO-8859-1, UTF-8, WIN1252)
UI: Janela dedicada com progresso detalhado e logs em tempo real
Proposed Changes
Fase 1: Camada de Domínio e Infraestrutura
[NEW] 

ImportTableMetadata.cs
Classe que representa os metadados de uma tabela SIGTAP:

Nome da tabela
Arquivo de dados (.txt)
Arquivo de layout (*_layout.txt)
Lista de colunas com posições, tamanhos e tipos
Prioridade de importação (para resolver dependências)
public class ImportTableMetadata
{
    public string TableName { get; set; }
    public string DataFileName { get; set; }
    public string LayoutFileName { get; set; }
    public List<ImportColumnMetadata> Columns { get; set; }
    public int ImportPriority { get; set; } // 1 = primeiro, maior = depois
}
public class ImportColumnMetadata
{
    public string ColumnName { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public int Length { get; set; }
    public string DataType { get; set; } // VARCHAR2, NUMBER, CHAR
}
[NEW] 

LayoutParser.cs
Parser resiliente para ler arquivos de layout:

Lê arquivos *_layout.txt
Extrai metadados de colunas (nome, posição inicial, final, tamanho, tipo)
Trata erros de parsing (linhas malformadas, formatos inesperados)
Suporta múltiplos encodings
Características de Resiliência:

Ignora linhas vazias ou malformadas
Continua parsing mesmo com erros em algumas linhas
Loga avisos para layouts inesperados
[NEW] 

FixedWidthParser.cs
Parser para arquivos de posição fixa:

Lê arquivos .txt com base nos metadados de layout
Extrai valores de cada coluna usando posições fixas
Converte tipos de dados (string → número, data, etc.)
Trata erros de conversão de forma resiliente
Características de Resiliência:

Strings vazias: Retorna null ou valor padrão
Formato inválido: Loga erro e retorna null (não quebra)
Caracteres especiais: Normaliza encoding (ISO-8859-1 → UTF-8)
Tipo incompatível: Tenta conversão flexível, se falhar retorna null
Linha curta/longa: Ajusta leitura dinamicamente
public class FixedWidthParser
{
    public Dictionary<string, object> ParseLine(string line, ImportTableMetadata metadata)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var column in metadata.Columns)
        {
            try
            {
                string value = ExtractValue(line, column);
                object convertedValue = ConvertValue(value, column.DataType);
                result[column.ColumnName] = convertedValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Erro ao parsear coluna {column.ColumnName}: {ex.Message}");
                result[column.ColumnName] = null; // Continua com null
            }
        }
        
        return result;
    }
}
[NEW] 

DataValidator.cs
Validador de dados importados:

Valida tipos de dados
Valida ranges (idade, valores monetários)
Valida chaves primárias e estrangeiras
Retorna lista de erros/avisos sem lançar exceções
Regras de Validação:

Chaves primárias não podem ser nulas
Valores numéricos devem estar em ranges válidos
Datas devem estar em formato válido (AAAAMM)
Códigos devem ter tamanho correto
Fase 2: Serviço de Importação
[NEW] 

ImportService.cs
Serviço principal de importação:

Coordena todo o processo de importação
Gerencia transações por tabela
Implementa rollback em caso de erro crítico
Reporta progresso em tempo real
Gera logs detalhados
Fluxo de Importação:

Descompactar ZIP (se necessário)
Ler arquivo 

layout.txt
 principal
Descobrir todas as tabelas disponíveis
Ordenar tabelas por prioridade (dependências)
Para cada tabela:
Ler layout específico
Parsear arquivo de dados
Validar registros
Inserir no banco (transação independente)
Reportar progresso
Gerar relatório final
Estratégia de Transação:

public async Task<ImportResult> ImportTableAsync(ImportTableMetadata metadata, string dataFilePath)
{
    var result = new ImportResult { TableName = metadata.TableName };
    
    using (var transaction = await _dbContext.Database.BeginTransactionAsync())
    {
        try
        {
            var lines = File.ReadAllLines(dataFilePath, Encoding.GetEncoding("ISO-8859-1"));
            int totalLines = lines.Length;
            int successCount = 0;
            int errorCount = 0;
            
            foreach (var (line, index) in lines.Select((l, i) => (l, i)))
            {
                try
                {
                    var data = _fixedWidthParser.ParseLine(line, metadata);
                    var validationResult = _dataValidator.Validate(data, metadata);
                    
                    if (validationResult.IsValid)
                    {
                        await InsertRecordAsync(metadata.TableName, data);
                        successCount++;
                    }
                    else
                    {
                        _logger.LogWarning($"Registro inválido linha {index}: {validationResult.ErrorMessage}");
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao importar linha {index}: {ex.Message}");
                    errorCount++;
                    // Continua para próxima linha
                }
                
                // Reporta progresso
                _progressReporter.Report(new ImportProgress
                {
                    TableName = metadata.TableName,
                    ProcessedLines = index + 1,
                    TotalLines = totalLines,
                    SuccessCount = successCount,
                    ErrorCount = errorCount
                });
            }
            
            await transaction.CommitAsync();
            result.Success = true;
            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
    }
    
    return result;
}
[NEW] 

ImportProgressReporter.cs
Reportador de progresso:

Usa IProgress<ImportProgress> para reportar progresso
Thread-safe para uso em operações assíncronas
Permite cancelamento via CancellationToken
Fase 3: Repositórios de Importação
[NEW] 

ImportRepository.cs
Repositório genérico para inserção de dados:

Gera SQL dinâmico baseado nos metadados
Usa parametrização para evitar SQL injection
Suporta inserção em lote (batch insert) para performance
Trata duplicatas (INSERT OR UPDATE)
Estratégia de Inserção:

public async Task InsertOrUpdateAsync(string tableName, Dictionary<string, object> data)
{
    // Verifica se registro já existe (baseado em PK)
    bool exists = await RecordExistsAsync(tableName, data);
    
    if (exists)
    {
        // UPDATE
        await UpdateRecordAsync(tableName, data);
    }
    else
    {
        // INSERT
        await InsertRecordAsync(tableName, data);
    }
}
Fase 4: Interface de Usuário
[NEW] 

ImportWindow.xaml
Janela dedicada para importação:

Seleção de arquivo: Botão para selecionar ZIP ou pasta descompactada
Opções de importação:
Importar todas as tabelas ou selecionar específicas
Modo de importação: Completo, Incremental, Apenas novas
Tratamento de duplicatas: Ignorar, Atualizar, Erro
Progresso:
Barra de progresso geral
Lista de tabelas com status individual
Contador de registros (sucesso/erro)
Logs:
TextBox com logs em tempo real
Opção de salvar logs em arquivo
Controles:
Botão Iniciar/Pausar/Cancelar
Botão Rollback (se houver erro)
Layout Proposto:

┌─────────────────────────────────────────────────┐
│ Importação de Dados SIGTAP                      │
├─────────────────────────────────────────────────┤
│ Arquivo: [C:\...\TabelaUnificada...zip] [...]  │
│                                                  │
│ Opções:                                          │
│ ☑ Importar todas as tabelas                     │
│ ○ Modo: Completo  ○ Incremental  ○ Apenas novas│
│ ○ Duplicatas: Ignorar  ● Atualizar  ○ Erro     │
│                                                  │
│ Progresso Geral: [████████░░] 80%               │
│                                                  │
│ Tabelas:                                         │
│ ✓ TB_GRUPO (8/8)                                │
│ ✓ TB_SUB_GRUPO (156/156)                        │
│ ⚙ TB_PROCEDIMENTO (3245/4500) ...              │
│ ⏸ TB_CID (aguardando...)                        │
│                                                  │
│ Logs:                                            │
│ ┌─────────────────────────────────────────────┐ │
│ │ [INFO] Iniciando importação...              │ │
│ │ [OK] TB_GRUPO: 8 registros importados       │ │
│ │ [WARN] TB_PROCEDIMENTO linha 1234: valor... │ │
│ └─────────────────────────────────────────────┘ │
│                                                  │
│ [Pausar] [Cancelar] [Salvar Logs]              │
└─────────────────────────────────────────────────┘
[MODIFY] 

MainWindow.xaml.cs
Atualizar método 

Importar_Click
:

private void Importar_Click(object sender, RoutedEventArgs e)
{
    var importWindow = new ImportWindow();
    importWindow.Owner = this;
    importWindow.ShowDialog();
}
Fase 5: Tratamento de Casos Especiais
[NEW] 

EncodingDetector.cs
Detector automático de encoding:

Tenta detectar encoding do arquivo (ISO-8859-1, UTF-8, WIN1252)
Usa heurísticas para identificar encoding correto
Fallback para encoding padrão se detecção falhar
Estratégia:

Tenta ler como ISO-8859-1 (padrão SIGTAP)
Se encontrar caracteres inválidos, tenta UTF-8
Se ainda falhar, tenta WIN1252
Loga encoding detectado
[NEW] 

StructureChangeDetector.cs
Detector de mudanças na estrutura:

Compara layout atual com layout anterior (se existir)
Detecta:
Novas tabelas
Novas colunas
Colunas removidas
Mudanças de tipo
Mudanças de posição
Gera relatório de mudanças
Permite importação adaptativa
Estratégia de Adaptação:

Nova tabela: Cria tabela automaticamente ou loga aviso
Nova coluna: Adiciona coluna ou ignora (configurável)
Coluna removida: Mantém coluna no banco (não remove)
Mudança de tipo: Tenta conversão, loga aviso se incompatível
Mudança de posição: Usa novo layout automaticamente
Fase 6: Logging e Auditoria
[NEW] 

ImportLog.cs
Entidade para log de importações:

public class ImportLog
{
    public int Id { get; set; }
    public DateTime DataImportacao { get; set; }
    public string ArquivoOrigem { get; set; }
    public string Competencia { get; set; }
    public string Status { get; set; } // Sucesso, Parcial, Erro
    public int TotalTabelas { get; set; }
    public int TabelasImportadas { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosImportados { get; set; }
    public int RegistrosComErro { get; set; }
    public string LogDetalhado { get; set; } // JSON com detalhes
    public TimeSpan TempoDecorrido { get; set; }
}
Tabela no banco: TB_IMPORT_LOG

[NEW] 

ImportLogRepository.cs
Repositório para persistir logs de importação.

Configuração e Prioridades de Importação
[NEW] 

ImportConfiguration.json
Arquivo de configuração para importação:

{
  "defaultEncoding": "ISO-8859-1",
  "batchSize": 1000,
  "enableStructureDetection": true,
  "importPriorities": {
    "TB_FINANCIAMENTO": 1,
    "TB_RUBRICA": 1,
    "TB_GRUPO": 2,
    "TB_SUB_GRUPO": 3,
    "TB_FORMA_ORGANIZACAO": 4,
    "TB_PROCEDIMENTO": 5,
    "TB_CID": 2,
    "RL_PROCEDIMENTO_CID": 6,
    "TB_SERVICO": 2,
    "TB_SERVICO_CLASSIFICACAO": 3,
    "RL_PROCEDIMENTO_SERVICO": 6
  },
  "requiredTables": [
    "TB_GRUPO",
    "TB_SUB_GRUPO",
    "TB_FORMA_ORGANIZACAO",
    "TB_PROCEDIMENTO"
  ],
  "optionalTables": [
    "TB_CID",
    "RL_PROCEDIMENTO_CID",
    "TB_OCUPACAO",
    "RL_PROCEDIMENTO_OCUPACAO"
  ]
}
Verification Plan
Automated Tests
1. Testes Unitários
Comando: dotnet test C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\src\UnificaSUS.Tests\UnificaSUS.Tests.csproj

Criar testes para:

LayoutParserTests.cs:

TestParseValidLayout(): Testa parsing de layout válido
TestParseMalformedLayout(): Testa resiliência com layout malformado
TestParseEmptyLayout(): Testa layout vazio
FixedWidthParserTests.cs:

TestParseValidLine(): Testa parsing de linha válida
TestParseEmptyString(): Testa string vazia
TestParseInvalidNumber(): Testa conversão de número inválido
TestParseSpecialCharacters(): Testa caracteres especiais (acentos)
TestParseShortLine(): Testa linha mais curta que esperado
TestParseLongLine(): Testa linha mais longa que esperado
DataValidatorTests.cs:

TestValidatePrimaryKey(): Testa validação de chave primária
TestValidateNumericRange(): Testa validação de ranges numéricos
TestValidateDate(): Testa validação de data (AAAAMM)
EncodingDetectorTests.cs:

TestDetectISO88591(): Testa detecção de ISO-8859-1
TestDetectUTF8(): Testa detecção de UTF-8
TestDetectWIN1252(): Testa detecção de WIN1252
2. Testes de Integração
Comando: dotnet test C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\src\UnificaSUS.IntegrationTests\UnificaSUS.IntegrationTests.csproj

Criar testes para:

ImportServiceIntegrationTests.cs:
TestImportSingleTable(): Testa importação de uma tabela simples
TestImportWithDependencies(): Testa importação respeitando dependências
TestImportWithErrors(): Testa importação com registros com erro
TestImportRollback(): Testa rollback em caso de erro crítico
TestImportCancellation(): Testa cancelamento de importação
Dados de Teste: Criar arquivos de teste em src\UnificaSUS.Tests\TestData\:


tb_grupo_layout.txt
: Layout de teste

tb_grupo.txt
: Dados de teste (5-10 registros)
tb_grupo_invalid.txt: Dados com erros propositais
Manual Verification
1. Teste de Importação Completa
Pré-requisitos:

Arquivo ZIP SIGTAP disponível: 

C:\Program Files\claupers\unificasus\TabelaUnificada_202510_v2510160954.zip
Banco de dados vazio ou com backup
Passos:

Executar aplicação: 

C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\EXECUTAR_APLICACAO.bat
Clicar no botão "Importar" na barra de ferramentas
Na janela de importação:
Selecionar arquivo ZIP ou pasta descompactada
Manter opções padrão (Importar todas, Modo Completo, Atualizar duplicatas)
Clicar em "Iniciar"
Observar progresso:
Barra de progresso deve avançar
Lista de tabelas deve mostrar status
Logs devem aparecer em tempo real
Aguardar conclusão (pode levar 5-10 minutos)
Verificar resultado:
Status deve ser "Concluído com sucesso" ou "Concluído com avisos"
Verificar contadores de sucesso/erro
Salvar logs para análise
Critérios de Sucesso:

✅ Importação completa sem erros críticos
✅ Todas as tabelas principais importadas (TB_GRUPO, TB_SUB_GRUPO, TB_FORMA_ORGANIZACAO, TB_PROCEDIMENTO)
✅ Logs mostram avisos mas não erros fatais
✅ Dados visíveis na interface principal (navegar por grupos/procedimentos)
2. Teste de Resiliência com Dados Corrompidos
Passos:

Criar cópia do arquivo 

tb_procedimento.txt
Editar arquivo e introduzir erros propositais:
Linha 10: Remover metade da linha
Linha 20: Colocar letras em campo numérico
Linha 30: Deixar linha vazia
Linha 40: Adicionar caracteres especiais inválidos
Executar importação
Verificar que:
Importação NÃO para completamente
Erros são logados
Registros válidos são importados
Contador de erros mostra 4 erros
Critérios de Sucesso:

✅ Importação continua apesar dos erros
✅ Registros válidos são importados
✅ Erros são logados com detalhes (número da linha, motivo)
✅ Contador de erros correto
3. Teste de Detecção de Mudanças de Estrutura
Passos:

Importar versão antiga (ex: 202410)
Importar versão nova (ex: 202510)
Verificar logs para mensagens de mudanças detectadas:
"Nova coluna detectada: CAMPO_X"
"Tabela nova detectada: TB_NOVA"
Verificar que importação se adapta automaticamente
Critérios de Sucesso:

✅ Mudanças são detectadas e logadas
✅ Importação se adapta automaticamente
✅ Dados antigos não são perdidos
4. Teste de Cancelamento
Passos:

Iniciar importação
Aguardar 10-20 segundos
Clicar em "Cancelar"
Verificar que:
Importação para imediatamente
Transação atual é revertida
Tabelas já importadas permanecem (não há rollback global)
Status mostra "Cancelado pelo usuário"
Critérios de Sucesso:

✅ Cancelamento funciona imediatamente
✅ Sem corrupção de dados
✅ Status correto
5. Teste de Encoding (Acentuação)
Passos:

Após importação, buscar procedimento com acentuação:
Exemplo: "ATIVIDADE EDUCATIVA / ORIENTAÇÃO"
Verificar que acentos estão corretos (não aparecem caracteres estranhos)
Verificar em diferentes tabelas:
TB_PROCEDIMENTO
TB_CID
TB_OCUPACAO
Critérios de Sucesso:

✅ Acentos exibidos corretamente
✅ Sem caracteres estranhos (�, Ã§, etc.)
✅ Encoding consistente em todas as tabelas
Verificação de Dados Importados
Queries SQL para validação (executar via isql ou aplicação):

-- 1. Verificar total de procedimentos importados
SELECT COUNT(*) FROM TB_PROCEDIMENTO WHERE DT_COMPETENCIA = '202510';
-- 2. Verificar grupos importados
SELECT CO_GRUPO, NO_GRUPO FROM TB_GRUPO WHERE DT_COMPETENCIA = '202510' ORDER BY CO_GRUPO;
-- 3. Verificar procedimentos com valores
SELECT COUNT(*) FROM TB_PROCEDIMENTO 
WHERE DT_COMPETENCIA = '202510' 
  AND (VL_SA > 0 OR VL_SH > 0 OR VL_SP > 0);
-- 4. Verificar relacionamentos CID
SELECT COUNT(*) FROM RL_PROCEDIMENTO_CID WHERE DT_COMPETENCIA = '202510';
-- 5. Verificar integridade referencial
SELECT COUNT(*) FROM TB_PROCEDIMENTO p
LEFT JOIN TB_FINANCIAMENTO f ON p.CO_FINANCIAMENTO = f.CO_FINANCIAMENTO
WHERE p.DT_COMPETENCIA = '202510' AND f.CO_FINANCIAMENTO IS NULL;
-- Resultado esperado: 0 (sem procedimentos com financiamento inválido)
Valores Esperados (baseado em TabelaUnificada_202510_v2510160954):

TB_GRUPO: ~8 registros
TB_SUB_GRUPO: ~150-200 registros
TB_FORMA_ORGANIZACAO: ~1000-1500 registros
TB_PROCEDIMENTO: ~4000-5000 registros
TB_CID: ~15000-20000 registros
RL_PROCEDIMENTO_CID: ~50000-100000 registros
Cronograma Estimado
Fase 1 (Domínio e Infraestrutura): 2-3 dias
Fase 2 (Serviço de Importação): 2-3 dias
Fase 3 (Repositórios): 1-2 dias
Fase 4 (Interface): 2-3 dias
Fase 5 (Casos Especiais): 1-2 dias
Fase 6 (Logging): 1 dia
Testes e Ajustes: 2-3 dias
Total: 11-17 dias de desenvolvimento

Próximos Passos Após Aprovação
Criar estrutura de pastas e arquivos base
Implementar LayoutParser e testes
Implementar FixedWidthParser e testes
Implementar ImportService básico
Criar UI básica para testes
Iterar com testes reais
Adicionar funcionalidades avançadas (detecção de mudanças, etc.)
Polimento e documentação
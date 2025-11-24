 Fase 2: Serviço de Importação (90%)

ImportService.cs
Serviço principal de coordenação

Funcionalidades:

✅ Descoberta automática de tabelas (busca *_layout.txt)
✅ Ordenação por prioridade de dependências
✅ Importação sequencial com progresso
✅ Suporte a cancelamento via CancellationToken
✅ Reportagem de progresso via IProgress<ImportProgress>
✅ Tratamento resiliente de erros por linha
✅ Logging detalhado
Métodos principais:

// Importa todas as tabelas de um diretório
Task<List<ImportResult>> ImportAllTablesAsync(
    string sigtapDirectory,
    IProgress<ImportProgress>? progress = null,
    CancellationToken cancellationToken = default)
// Importa uma tabela específica
Task<ImportResult> ImportTableAsync(
    ImportTableMetadata metadata,
    string dataFilePath,
    IProgress<ImportProgress>? progress = null,
    CancellationToken cancellationToken = default)
Sistema de Prioridades:

Prioridade 1: Tabelas base (TB_FINANCIAMENTO, TB_RUBRICA, etc.)
Prioridade 2: Estrutura hierárquica (TB_GRUPO, TB_CID, etc.)
Prioridade 3: Segundo nível (TB_SUB_GRUPO, TB_SERVICO_CLASSIFICACAO)
Prioridade 4: Terceiro nível (TB_FORMA_ORGANIZACAO)
Prioridade 5: Procedimentos (TB_PROCEDIMENTO, TB_DESCRICAO)
Prioridade 6: Relacionamentos (RL_*)

ImportConfiguration.cs
Configurações de importação

Propriedades:

DefaultEncoding: Encoding padrão (ISO-8859-1)
BatchSize: Tamanho de lote para inserção (1000)
EnableStructureDetection: Detecta mudanças na estrutura
ImportPriorities: Prioridades customizadas por tabela
RequiredTables: Tabelas obrigatórias
OptionalTables: Tabelas opcionais
DuplicateMode: Como tratar duplicatas (Ignore/Update/Error)
MaxErrorsBeforeStop: Limite de erros (0 = sem limite)
LogDirectory: Diretório para logs
Enums:

public enum DuplicateHandlingMode
{
    Ignore,  // Ignora duplicatas
    Update,  // Atualiza duplicatas
    Error    // Erro ao encontrar duplicata
}
public enum ImportMode
{
    Full,        // Limpa e reimporta tudo
    Incremental, // Apenas novos registros
    NewOnly      // Ignora existentes
}
Serialização JSON:

var config = ImportConfiguration.LoadFromFile("ImportConfiguration.json");
config.SaveToFile("ImportConfiguration.json");

SigtapFileExtractor.cs
Utilitário para manipulação de arquivos

Funcionalidades:

✅ Extração de arquivos ZIP
✅ Validação de diretórios SIGTAP
✅ Obtenção de informações (versão, competência)
✅ Limpeza de arquivos temporários
Métodos principais:

// Extrai ZIP para diretório
string ExtractZipFile(string zipFilePath, string? targetDirectory = null)
// Verifica se é diretório SIGTAP válido
bool IsValidSigtapDirectory(string directory)
// Obtém informações do arquivo
SigtapFileInfo GetFileInfo(string directoryOrZip)
// Limpa diretórios temporários
void CleanupTempDirectories()
Validação:

Verifica presença de arquivos *_layout.txt
Verifica presença de versao/config.inf/LEIA_ME.TXT

ImportConfiguration.json
Arquivo de configuração padrão

Contém:

Prioridades de todas as ~40 tabelas SIGTAP
Tabelas obrigatórias (4): TB_GRUPO, TB_SUB_GRUPO, TB_FORMA_ORGANIZACAO, TB_PROCEDIMENTO
Tabelas opcionais (8): TB_CID, RL_PROCEDIMENTO_CID, etc.
Configurações padrão
📊 Estrutura de Arquivos Atualizada
src/
├── UnificaSUS.Core/Import/
│   ├── ImportColumnMetadata.cs      ✅
│   ├── ImportTableMetadata.cs       ✅
│   ├── ImportResult.cs              ✅
│   └── ImportProgress.cs            ✅
│
├── UnificaSUS.Infrastructure/Import/
│   ├── LayoutParser.cs               ✅
│   ├── FixedWidthParser.cs           ✅
│   ├── DataValidator.cs              ✅
│   ├── EncodingDetector.cs           ✅
│   └── ParserValidationTest.cs       ✅
│
└── UnificaSUS.Application/Services/Import/
    ├── ImportService.cs               ✅
    ├── ImportConfiguration.cs         ✅
    ├── SigtapFileExtractor.cs         ✅
    └── ImportConfiguration.json       ✅
Total: 13 arquivos criados

🔄 Fluxo de Importação Completo
1. Usuário seleciona arquivo ZIP ou pasta
   ↓
2. SigtapFileExtractor valida e extrai (se ZIP)
   ↓
3. ImportService.ImportAllTablesAsync()
   ├─ Descobre tabelas (LayoutParser)
   ├─ Ordena por prioridade
   └─ Para cada tabela:
       ├─ Detecta encoding (EncodingDetector)
       ├─ Lê layout (LayoutParser)
       ├─ Para cada linha:
       │   ├─ Parse (FixedWidthParser)
       │   ├─ Validação (DataValidator)
       │   ├─ Inserção no banco ⚠️ TODO Fase 3
       │   └─ Reporta progresso
       └─ Retorna ImportResult
   ↓
4. Exibe resultados e logs para usuário
⚠️ Fase 3 necessária: Implementar ImportRepository para inserção no banco

🎯 Características Implementadas
Requisito	Status
String Vazia	✅ Tratado
Formato Inválido	✅ Tratado
Caracteres Especiais	✅ Encoding auto
Tipo Incompatível	✅ Conversão flexível
Novas Tabelas	✅ Descobre auto
Novas Colunas	✅ Ignora desconhecidas
Mudança Sequência	✅ Usa posições fixas
Cancelamento	✅ CancellationToken
Progresso	✅ IProgress
Priorização	✅ Sistema completo
📝 Próximos Passos
Fase 3: Repositórios (Próxima)
 ImportRepository.cs - Inserção dinâmica no banco
 Geração de SQL dinâmico
 Tratamento de duplicatas (INSERT OR UPDATE)
 Batch insert para performance
Fase 4: Interface de Usuário
 ImportWindow.xaml - Janela dedicada
 Binding de progresso visual
 Logs em tempo real
 Controles (Iniciar/Pausar/Cancelar)
Última atualização: 21/11/2025 20:05
Progresso geral: ~40% (Fases 1-2 de 6 concluídas)
Walkthrough: Implementação da Funcionalidade de Importação SIGTAP
Resumo do Progresso
Status Atual: Fase 3 (Repositórios) - 100% concluída | Progresso: ~55%

✅ Fases Concluídas
Fase 1: Camada de Domínio (100%) - 9 arquivos
Fase 2: Serviço de Importação (100%) - 4 arquivos
Fase 3: Repositórios (100%) - 1 arquivo
✅ Fase 3: Repositórios (NEW!)

ImportRepository.cs
Repositório de importação dinâmica

Funcionalidades:

✅ Inserção/atualização automática baseada em metadados
✅ Geração dinâmica de SQL (INSERT/UPDATE)
✅ Verificação de duplicatas por chaves primárias
✅ Batch insert para performance (lotes de 1000)
✅ Tratamento de transações
✅ Delete por competência
✅ Count de registros
Métodos principais:

// Insere ou atualiza registro
Task<bool> InsertOrUpdateAsync(
    string tableName,
    Dictionary<string, object?> data,
    ImportTableMetadata metadata,
    DuplicateHandlingMode duplicateMode,
    CancellationToken cancellationToken)
// Batch insert
Task<int> InsertBatchAsync(
    string tableName,
    List<Dictionary<string, object?>> dataList,
    ImportTableMetadata metadata,
    int batchSize = 1000,
    CancellationToken cancellationToken)
// Delete por competência
Task<int> DeleteByCompetenciaAsync(
    string tableName,
    string competencia,
    CancellationToken cancellationToken)
Geração Dinâmica de SQL:

// Exemplo de INSERT gerado dinamicamente:
INSERT INTO TB_PROCEDIMENTO (CO_PROCEDIMENTO, NO_PROCEDIMENTO, VL_SA, VL_SH, DT_COMPETENCIA)
VALUES (@CO_PROCEDIMENTO, @NO_PROCEDIMENTO, @VL_SA, @VL_SH, @DT_COMPETENCIA)
// Exemplo de UPDATE gerado:
UPDATE TB_PROCEDIMENTO
SET NO_PROCEDIMENTO = @NO_PROCEDIMENTO, VL_SA = @VL_SA
WHERE CO_PROCEDIMENTO = @PK_CO_PROCEDIMENTO AND DT_COMPETENCIA = @PK_DT_COMPETENCIA
Tratamento de Duplicatas:

DuplicateHandlingMode.Ignore: Ignora registro duplicado
DuplicateHandlingMode.Update: Atualiza registro existente
DuplicateHandlingMode.Error: Lança exceção
Conversão de Tipos:

NUMBER/NUMERIC → decimal/int/long
VARCHAR2/CHAR → string
DATE → string (formato AAAAMM)
Valores null → DBNull.Value
Integração com 

ImportService
O 

ImportService
 foi atualizado para usar 

ImportRepository
:

// Construtor aceita repository opcional
public ImportService(ILogger? logger = null, ImportRepository? importRepository = null)
// Durante importação, insere no banco
if (_importRepository != null)
{
    await _importRepository.InsertOrUpdateAsync(
        metadata.TableName,
        data,
        metadata,
        DuplicateHandlingMode.Update,
        cancellationToken);
}
Modo Simulação: Se 

ImportRepository
 for null, funciona sem banco (apenas validação)

📊 Arquitetura Completa (Backend)
┌─────────────────────────────────────────────┐
│ ImportService (Coordenador Principal)       │
├─────────────────────────────────────────────┤
│ - Descoberta de tabelas                     │
│ - Ordenação por prioridade                  │
│ - Processamento com progresso               │
│ - Cancelamento via CancellationToken        │
└──────────────┬──────────────────────────────┘
               │
    ┌──────────┼──────────┬──────────────────┐
    │          │          │                  │
┌───▼───┐  ┌──▼───┐  ┌───▼────┐  ┌─────────▼──────┐
│Layout │  │Fixed │  │Data    │  │Encoding        │
│Parser │  │Width │  │Validator│  │Detector        │
└───┬───┘  │Parser│  └───┬────┘  └────────────────┘
    │      └──┬───┘      │
    │         │          │
    └─────────┼──────────┘
              │
       ┌──────▼──────┐
       │   ImportRepository   │
       ├─────────────────────┤
       │ - SQL dinâmico      │
       │ - INSERT/UPDATE     │
       │ - Batch processing  │
       └──────┬──────────────┘
              │
       ┌──────▼──────────┐
       │ FirebirdContext  │
       │ (Banco Firebird) │
       └──────────────────┘
🔄 Fluxo Completo de Importação
1. Usuário seleciona ZIP/pasta
   ↓
2. SigtapFileExtractor extrai arquivos
   ↓
3. ImportService descobre tabelas (LayoutParser)
   ↓
4. Ordena por prioridade (1-6)
   ↓
5. Para cada tabela:
   ├─ Detecta encoding
   ├─ Lê layout
   ├─ Para cada linha:
   │   ├─ Parse (FixedWidthParser)
   │   ├─ Validação (DataValidator)
   │   ├─ Conversão de tipos
   │   ├─ INSERT/UPDATE (ImportRepository) ✅ NOVO
   │   └─ Reporta progresso
   └─ Retorna ImportResult
   ↓
6. Exibe resultados
📊 Estrutura Completa de Arquivos
Total: 14 arquivos (3 fases backend completas)

src/
├── UnificaSUS.Core/Import/            (4 arquivos)
│   ├── ImportColumnMetadata.cs        ✅
│   ├── ImportTableMetadata.cs         ✅
│   ├── ImportResult.cs                ✅
│   └── ImportProgress.cs              ✅
│
├── UnificaSUS.Infrastructure/
│   ├── Import/                        (5 arquivos)
│   │   ├── LayoutParser.cs            ✅
│   │   ├── FixedWidthParser.cs        ✅
│   │   ├── DataValidator.cs           ✅
│   │   ├── EncodingDetector.cs        ✅
│   │   └── ParserValidationTest.cs    ✅
│   │
│   └── Repositories/                  (1 arquivo)
│       └── ImportRepository.cs        ✅ NOVO
│
└── UnificaSUS.Application/Services/Import/  (4 arquivos)
    ├── ImportService.cs               ✅ (atualizado)
    ├── ImportConfiguration.cs         ✅
    ├── SigtapFileExtractor.cs         ✅
    └── ImportConfiguration.json       ✅
🎯 Próximos Passos
Fase 4: Interface de Usuário (Próxima)

 ImportWindow.xaml - Janela WPF dedicada
 ImportWindow.xaml.cs - Code-behind
 Binding de progresso visual
 Logs em tempo real
 Controles (Iniciar/Pausar/Cancelar)
Funcionalidades pendentes:

Fase 5: Detector de mudanças de estrutura
Fase 6: Sistema de logging e auditoria persistente
Última atualização: 21/11/2025 20:12
Progresso geral: ~55% (Fases 1-3 de 6 concluídas - Backend completo!)
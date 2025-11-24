# Análise de Duplicatas na Importação - TB_CID

## Problema Identificado

O log de importação mostra que foram importados **685 registros** de TB_CID, mas houve **14 erros de truncamento**. Precisamos validar se:

1. Os registros foram duplicados ou atualizados corretamente
2. As chaves primárias estão sendo identificadas corretamente
3. O modo `DuplicateHandlingMode.Update` está funcionando

## Estrutura da TB_CID

Baseado no layout (`tb_cid_layout.txt`):
- `CO_CID` (4) - Código CID
- `NO_CID` (100) - Nome/Descrição
- `TP_AGRAVO` (1) - Tipo de agravo
- `TP_SEXO` (1) - Tipo de sexo
- `TP_ESTADIO` (1) - Tipo de estádio
- `VL_CAMPOS_IRRADIADOS` (4) - Valor campos irradiados

**IMPORTANTE**: A tabela TB_CID **NÃO possui DT_COMPETENCIA** no layout, mas o banco pode ter essa coluna adicionada automaticamente.

## Chave Primária Esperada

Baseado na heurística do código (`IdentifyPrimaryKeys`):
- Para tabelas simples (TB_*): Primeira coluna `CO_*` é PK única
- **TB_CID**: Chave primária deveria ser apenas `CO_CID`

**PROBLEMA POTENCIAL**: 
- O código `IdentifyPrimaryKeys` retorna os nomes das colunas, mas **não atualiza** `IsPrimaryKey = true` nas colunas do metadata
- O método `RecordExistsAsync` usa `metadata.Columns.Where(c => c.IsPrimaryKey)`, que pode retornar vazio se `IsPrimaryKey` não foi definido
- Se não houver chaves primárias identificadas, o código sempre insere (não verifica duplicatas)

## Scripts de Validação

Execute o arquivo `verificar_duplicatas.sql` para verificar:

1. **Estrutura da tabela**: Verificar quais são as chaves primárias reais no banco
2. **Contagem de registros**: Total de registros importados para competência 202510
3. **Duplicatas**: Verificar se há registros duplicados baseado na chave primária esperada
4. **Registros problemáticos**: Verificar os CIDs mencionados no log (A150, A155, etc.)

## Próximos Passos

1. Executar o script SQL para verificar duplicatas
2. Se houver duplicatas, corrigir o código para atualizar `IsPrimaryKey` no metadata após identificar as chaves
3. Se não houver duplicatas, os erros de truncamento são o problema principal (já corrigido com UTF-8)


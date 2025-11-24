# Análise de Impacto: Criação de Índices no Banco de Dados

## ⚠️ Contexto Importante

**A aplicação original ainda utiliza o mesmo banco de dados e continuará utilizando.**

Qualquer alteração no banco de dados deve ser analisada para garantir que:
- ✅ Não altera o funcionamento da aplicação original
- ✅ Não causa lentidão em operações existentes
- ✅ Não quebra funcionalidades existentes

---

## 📊 Impacto de Índices no Firebird

### ✅ O que Índices NÃO Fazem (Seguro)

1. **Não alteram dados**: Índices são estruturas auxiliares, não modificam dados
2. **Não alteram queries existentes**: Queries continuam funcionando exatamente igual
3. **Não quebram aplicações**: Aplicações antigas continuam funcionando normalmente
4. **São transparentes**: Aplicações não precisam saber que índices existem

### ⚠️ Possíveis Impactos (Precisa Monitorar)

1. **INSERT mais lento**: Cada INSERT precisa atualizar os índices
   - **Impacto**: Mínimo a moderado, dependendo do volume
   - **Mitigação**: Índices compostos são mais eficientes que múltiplos índices simples

2. **UPDATE mais lento**: UPDATE em colunas indexadas precisa atualizar índices
   - **Impacto**: Mínimo, apenas se UPDATE afetar colunas indexadas
   - **Mitigação**: Índices em colunas raramente atualizadas

3. **Espaço em disco**: Índices ocupam espaço adicional
   - **Impacto**: Baixo, geralmente 10-30% do tamanho da tabela
   - **Mitigação**: Monitorar espaço disponível

4. **Bloqueios**: Criação de índices pode bloquear tabelas temporariamente
   - **Impacto**: Apenas durante a criação (uma vez)
   - **Mitigação**: Criar em horário de baixo uso

---

## 🔍 Análise dos Índices Propostos

### Índice 1: `IDX_RL_PROCEDIMENTO_CID_PROC_COMP`
```sql
CREATE INDEX IDX_RL_PROCEDIMENTO_CID_PROC_COMP ON RL_PROCEDIMENTO_CID 
    (CO_PROCEDIMENTO, DT_COMPETENCIA);
```

**Tabela**: `RL_PROCEDIMENTO_CID` (tabela de relacionamento)

**Análise de Impacto**:
- ✅ **SELECT**: Melhora significativamente queries de busca por procedimento + competência
- ⚠️ **INSERT**: Impacto mínimo - tabela de relacionamento geralmente tem poucos INSERTs
- ⚠️ **UPDATE**: Impacto mínimo - colunas indexadas raramente são atualizadas
- ✅ **DELETE**: Impacto mínimo - DELETE também se beneficia do índice

**Risco para Aplicação Original**: **BAIXO**
- Tabela de relacionamento (poucos INSERTs/UPDATEs)
- Aplicação original provavelmente já faz SELECTs similares (se beneficiará)

---

### Índice 2: `IDX_RL_PROCEDIMENTO_CID_CID_COMP`
```sql
CREATE INDEX IDX_RL_PROCEDIMENTO_CID_CID_COMP ON RL_PROCEDIMENTO_CID 
    (CO_CID, DT_COMPETENCIA);
```

**Tabela**: `RL_PROCEDIMENTO_CID` (mesma tabela do índice 1)

**Análise de Impacto**:
- ✅ **SELECT**: Melhora queries de busca por CID + competência
- ⚠️ **INSERT**: Impacto mínimo - mesmo que índice 1
- ⚠️ **UPDATE**: Impacto mínimo - mesmo que índice 1
- ✅ **DELETE**: Impacto mínimo - mesmo que índice 1

**Risco para Aplicação Original**: **BAIXO**
- Mesma tabela, mesmo padrão de uso
- Dois índices na mesma tabela podem ter impacto cumulativo em INSERTs, mas ainda é baixo

---

## 📈 Recomendações

### ✅ Recomendado: Criar Índices

**Justificativa**:
1. Tabela `RL_PROCEDIMENTO_CID` é uma tabela de relacionamento
2. Tabelas de relacionamento geralmente têm:
   - Muitos SELECTs (buscas)
   - Poucos INSERTs (apenas durante importação)
   - Raros UPDATEs (relacionamentos raramente mudam)
3. Benefício para ambas aplicações (original e nova)
4. Risco baixo de impacto negativo

### ⚠️ Precauções

1. **Criar em horário de baixo uso**
   - Durante criação, tabela pode ficar bloqueada temporariamente
   - Aplicação original pode ter timeout se houver queries ativas

2. **Monitorar após criação**
   - Verificar se INSERTs na aplicação original ficaram mais lentos
   - Verificar se há erros ou timeouts

3. **Ter plano de rollback**
   - Se houver problemas, índices podem ser removidos:
   ```sql
   DROP INDEX IDX_RL_PROCEDIMENTO_CID_PROC_COMP;
   DROP INDEX IDX_RL_PROCEDIMENTO_CID_CID_COMP;
   ```

4. **Verificar índices existentes primeiro**
   - Pode ser que índices similares já existam
   - Índices duplicados são desperdício

---

## 🧪 Plano de Teste

### Antes de Criar

1. ✅ Executar `verificar_indices_existentes.sql` para ver índices atuais
2. ✅ Verificar se índices propostos já existem
3. ✅ Analisar padrão de uso da aplicação original (se possível)

### Durante Criação

1. ⚠️ Executar em horário de baixo uso
2. ⚠️ Monitorar logs da aplicação original
3. ⚠️ Verificar se há bloqueios ou timeouts

### Após Criação

1. ✅ Testar queries na nova aplicação (deve estar mais rápido)
2. ✅ Testar funcionalidades críticas da aplicação original
3. ✅ Monitorar performance de INSERTs (se aplicação original faz muitos)
4. ✅ Verificar espaço em disco

---

## 🔄 Plano de Rollback

Se houver problemas, remover índices:

```sql
-- Remover índices (se necessário)
DROP INDEX IDX_RL_PROCEDIMENTO_CID_PROC_COMP;
DROP INDEX IDX_RL_PROCEDIMENTO_CID_CID_COMP;
```

**Impacto do Rollback**: Nenhum - apenas volta ao estado anterior

---

## 📝 Checklist de Segurança

Antes de executar criação de índices:

- [ ] Backup do banco de dados realizado
- [ ] Índices existentes verificados (não criar duplicados)
- [ ] Horário de baixo uso identificado
- [ ] Aplicação original pode ser pausada temporariamente (se necessário)
- [ ] Plano de rollback documentado
- [ ] Equipe da aplicação original notificada (se aplicável)
- [ ] Monitoramento configurado para após criação

---

## 🎯 Conclusão

**Recomendação**: ✅ **CRIAR ÍNDICES COM PRECAUÇÕES**

**Razão**: 
- Benefício alto para performance
- Risco baixo de impacto negativo
- Fácil reversão se necessário
- Tabela de relacionamento (baixo impacto em INSERTs)

**Condições**:
- Executar em horário de baixo uso
- Ter backup antes
- Monitorar após criação
- Ter plano de rollback pronto

---

**Última Atualização**: 2025-01-22
**Versão**: 1.0


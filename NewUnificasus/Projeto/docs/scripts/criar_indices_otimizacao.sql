-- Script: Criar Índices para Otimização de Queries
-- Objetivo: Criar índices compostos para melhorar performance das queries mais lentas
-- ATENÇÃO: Execute este script apenas após verificar índices existentes
-- Verifique se os índices já existem antes de criar

-- ============================================
-- 1. Índice para BuscarCID10RelacionadosAsync
-- Query: WHERE pc.CO_PROCEDIMENTO = @coProcedimento AND pc.DT_COMPETENCIA = @competencia
-- ============================================
-- Índice composto em RL_PROCEDIMENTO_CID para otimizar busca por procedimento e competência
CREATE INDEX IDX_RL_PROCEDIMENTO_CID_PROC_COMP ON RL_PROCEDIMENTO_CID 
    (CO_PROCEDIMENTO, DT_COMPETENCIA);

-- ============================================
-- 2. Índice para BuscarPorCIDAsync
-- Query: WHERE pc.DT_COMPETENCIA = @competencia AND pc.CO_CID = @cid
-- ============================================
-- Índice composto em RL_PROCEDIMENTO_CID para otimizar busca por CID e competência
CREATE INDEX IDX_RL_PROCEDIMENTO_CID_CID_COMP ON RL_PROCEDIMENTO_CID 
    (CO_CID, DT_COMPETENCIA);

-- ============================================
-- 3. Índice em TB_CID (se CO_CID não for PK)
-- Query: JOIN TB_CID c ON pc.CO_CID = c.CO_CID
-- ============================================
-- Verificar se CO_CID já é chave primária antes de criar
-- Se não for, criar índice para otimizar JOIN
-- CREATE INDEX IDX_TB_CID_CO_CID ON TB_CID (CO_CID);

-- ============================================
-- 4. Índice em TB_PROCEDIMENTO (se CO_PROCEDIMENTO não for PK)
-- Query: JOIN TB_PROCEDIMENTO pr ON pr.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
-- ============================================
-- Verificar se CO_PROCEDIMENTO já é chave primária antes de criar
-- Se não for, criar índice para otimizar JOIN
-- CREATE INDEX IDX_TB_PROCEDIMENTO_CO_PROC ON TB_PROCEDIMENTO (CO_PROCEDIMENTO);

-- ============================================
-- 5. Índice adicional para ordenação em BuscarCID10RelacionadosAsync
-- Query: ORDER BY pc.ST_PRINCIPAL DESC, c.CO_CID
-- ============================================
-- Este índice pode ajudar na ordenação, mas o índice principal já deve ser suficiente
-- CREATE INDEX IDX_RL_PROCEDIMENTO_CID_PRINCIPAL ON RL_PROCEDIMENTO_CID 
--     (CO_PROCEDIMENTO, DT_COMPETENCIA, ST_PRINCIPAL DESC);

-- ============================================
-- NOTAS IMPORTANTES:
-- ============================================
-- 1. Execute primeiro o script verificar_indices_existentes.sql
-- 2. Verifique se os índices já existem antes de criar
-- 3. Índices compostos devem ter a ordem dos campos conforme a query
-- 4. Para queries com WHERE em múltiplos campos, o índice composto deve ter esses campos na mesma ordem
-- 5. Após criar índices, execute UPDATE STATISTICS ou SET STATISTICS INDEX para atualizar estatísticas


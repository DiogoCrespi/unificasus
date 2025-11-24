-- Script: Validar Uso de Índices nas Queries
-- Objetivo: Verificar se o Firebird está usando os índices criados

-- ============================================
-- 1. Verificar plano de execução da query BuscarCID10RelacionadosAsync
-- ============================================
-- Query simulada: WHERE pc.CO_PROCEDIMENTO = ? AND pc.DT_COMPETENCIA = ?
SET PLAN ON;

SELECT 
    c.CO_CID,
    CAST(c.NO_CID AS BLOB) AS NO_CID_BLOB,
    c.NO_CID,
    pc.ST_PRINCIPAL
FROM RL_PROCEDIMENTO_CID pc
INNER JOIN TB_CID c ON pc.CO_CID = c.CO_CID
WHERE pc.CO_PROCEDIMENTO = '0301010010'
  AND pc.DT_COMPETENCIA = '202510'
ORDER BY pc.ST_PRINCIPAL DESC, c.CO_CID;

SET PLAN OFF;

-- ============================================
-- 2. Verificar plano de execução da query BuscarPorCIDAsync
-- ============================================
-- Query simulada: WHERE pc.DT_COMPETENCIA = ? AND pc.CO_CID = ?
SET PLAN ON;

SELECT DISTINCT
    pr.CO_PROCEDIMENTO,
    pr.NO_PROCEDIMENTO
FROM TB_PROCEDIMENTO pr
INNER JOIN RL_PROCEDIMENTO_CID pc ON pr.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
WHERE pc.DT_COMPETENCIA = '202510'
  AND pc.CO_CID = 'A00'
ORDER BY pr.CO_PROCEDIMENTO
ROWS 10;

SET PLAN OFF;

-- ============================================
-- 3. Verificar estatísticas dos índices
-- ============================================
SELECT 
    TRIM(I.RDB$INDEX_NAME) AS INDICE,
    TRIM(I.RDB$RELATION_NAME) AS TABELA,
    I.RDB$STATISTICS AS ESTATISTICAS
FROM RDB$INDICES I
WHERE I.RDB$RELATION_NAME = 'RL_PROCEDIMENTO_CID'
  AND I.RDB$SYSTEM_FLAG = 0
  AND I.RDB$INDEX_NAME IN ('IDX_RL_PCID_PROC_COMP', 'IDX_RL_PCID_CID_COMP');

-- ============================================
-- 4. Atualizar estatísticas dos índices (recomendado após criação)
-- ============================================
-- Nota: Firebird atualiza estatísticas automaticamente, mas podemos forçar
-- SET STATISTICS INDEX IDX_RL_PCID_PROC_COMP;
-- SET STATISTICS INDEX IDX_RL_PCID_CID_COMP;


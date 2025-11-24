-- ============================================================================
-- Script SIMPLES para DELETAR duplicatas de CID10
-- Mantém apenas o primeiro registro (menor INDICE) de cada grupo
-- ============================================================================

-- 1. VERIFICAR total de duplicatas ANTES
SELECT 
    'ANTES - Total duplicatas' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

-- 2. DELETAR duplicatas - mantém apenas o primeiro (menor INDICE)
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE NOT IN (
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID
      WHERE DT_COMPETENCIA = '202510'
      GROUP BY CO_PROCEDIMENTO, CO_CID, DT_COMPETENCIA
  );

-- 3. VERIFICAR após deleção
SELECT 
    'DEPOIS - Verificacao' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS_RESTANTES
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';


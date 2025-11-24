-- ============================================================================
-- Script para DELETAR duplicatas de CID10 - TODOS OS CASOS
-- Remove duplicatas mantendo apenas o registro com menor INDICE
-- ============================================================================

-- 1. VERIFICAR total de duplicatas antes
SELECT 
    'ANTES - Total duplicatas' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';

-- 2. Listar alguns exemplos de duplicatas
SELECT 
    FIRST 20
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_DUPLICATAS,
    MIN(INDICE) AS INDICE_MANTIDO,
    LIST(INDICE, ', ') AS INDICES_REMOVIDOS
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;

-- 3. DELETAR duplicatas - mantém apenas o primeiro (menor INDICE) de cada grupo
-- ATENÇÃO: Este comando DELETA dados! Execute apenas após validar os resultados acima!
-- Descomente para executar:
/*
DELETE FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510'
  AND INDICE NOT IN (
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID
      WHERE DT_COMPETENCIA = '202510'
      GROUP BY CO_PROCEDIMENTO, CO_CID, DT_COMPETENCIA
  );
*/

-- 4. VERIFICAR após deleção
SELECT 
    'DEPOIS - Verificacao' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS,
    COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS REGISTROS_UNICOS,
    COUNT(*) - COUNT(DISTINCT CO_PROCEDIMENTO || '|' || CO_CID || '|' || DT_COMPETENCIA) AS TOTAL_DUPLICATAS_RESTANTES
FROM RL_PROCEDIMENTO_CID
WHERE DT_COMPETENCIA = '202510';


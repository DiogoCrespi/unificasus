-- ============================================================================
-- Script para DELETAR duplicatas de CID10 - TESTE COM C73
-- Este script DELETA as duplicatas mantendo apenas o primeiro registro
-- ============================================================================

-- 1. VERIFICAR duplicatas ANTES
SELECT 
    'ANTES - Duplicatas C73' AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS
FROM RL_PROCEDIMENTO_CID
WHERE CO_CID = 'C73'
  AND CO_PROCEDIMENTO = '0201010038'
  AND DT_COMPETENCIA = '202510';

-- 2. DELETAR duplicatas - mantém apenas o primeiro (menor INDICE)
DELETE FROM RL_PROCEDIMENTO_CID
WHERE CO_CID = 'C73'
  AND CO_PROCEDIMENTO = '0201010038'
  AND DT_COMPETENCIA = '202510'
  AND INDICE NOT IN (
      SELECT MIN(INDICE)
      FROM RL_PROCEDIMENTO_CID
      WHERE CO_CID = 'C73'
        AND CO_PROCEDIMENTO = '0201010038'
        AND DT_COMPETENCIA = '202510'
  );

-- 3. VERIFICAR após deleção
SELECT 
    'DEPOIS - Verificacao' AS STATUS,
    COUNT(*) AS TOTAL_RESTANTE
FROM RL_PROCEDIMENTO_CID
WHERE CO_CID = 'C73'
  AND CO_PROCEDIMENTO = '0201010038'
  AND DT_COMPETENCIA = '202510';


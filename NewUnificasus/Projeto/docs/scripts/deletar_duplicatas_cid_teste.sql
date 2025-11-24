-- ============================================================================
-- Script para DELETAR duplicatas de CID10 - TESTE COM UM EXEMPLO
-- Testa primeiro com C73 no procedimento 0201010038
-- ============================================================================

-- 1. VERIFICAR duplicatas do CID C73 no procedimento 0201010038
SELECT 
    'ANTES - Duplicatas C73' AS STATUS,
    INDICE,
    CO_CID,
    CO_PROCEDIMENTO,
    DT_COMPETENCIA,
    NO_CID,
    CASE 
        WHEN UPPER(TRIM(NO_CID)) = TRIM(NO_CID) THEN 'MAIUSCULAS'
        ELSE 'Minusculas/Misturadas'
    END AS TIPO,
    LENGTH(NO_CID) AS TAMANHO
FROM RL_PROCEDIMENTO_CID
WHERE CO_CID = 'C73'
  AND CO_PROCEDIMENTO = '0201010038'
  AND DT_COMPETENCIA = '202510'
ORDER BY INDICE;

-- 2. Verificar qual é a descrição correta em TB_CID
SELECT 
    'Descricao correta em TB_CID' AS INFO,
    CO_CID,
    NO_CID AS DESCRICAO_CORRETA
FROM TB_CID
WHERE CO_CID = 'C73';

-- 3. Contar quantas duplicatas serão removidas
SELECT 
    'Total duplicatas a remover' AS INFO,
    CAST(COUNT(*) - 1 AS INTEGER) AS TOTAL_A_REMOVER
FROM RL_PROCEDIMENTO_CID
WHERE CO_CID = 'C73'
  AND CO_PROCEDIMENTO = '0201010038'
  AND DT_COMPETENCIA = '202510';

-- 4. DELETAR duplicatas - mantém apenas o primeiro (menor INDICE)
-- ATENÇÃO: Descomente para executar!
/*
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
*/

-- 5. VERIFICAR após deleção (se executou)
SELECT 
    'DEPOIS - Verificacao' AS STATUS,
    COUNT(*) AS TOTAL_RESTANTE,
    MIN(INDICE) AS INDICE_MANTIDO,
    MAX(INDICE) AS INDICE_MAXIMO
FROM RL_PROCEDIMENTO_CID
WHERE CO_CID = 'C73'
  AND CO_PROCEDIMENTO = '0201010038'
  AND DT_COMPETENCIA = '202510';


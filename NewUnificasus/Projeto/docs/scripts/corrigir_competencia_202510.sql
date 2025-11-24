-- ============================================
-- SCRIPT PARA CORRIGIR COMPETÊNCIA 202510 NA LISTAGEM
-- Baseado no log: ImportLog_20251122_070908.txt
-- Competência: 202510 (Outubro de 2025)
-- ============================================
-- 
-- PROBLEMA: A listagem de competências busca de TB_PROCEDIMENTO usando:
-- SELECT DISTINCT DT_COMPETENCIA FROM TB_PROCEDIMENTO WHERE DT_COMPETENCIA IS NOT NULL
-- 
-- SOLUÇÃO: Garantir que os procedimentos importados tenham DT_COMPETENCIA = '202510'
-- ============================================

-- ============================================
-- PASSO 1: DIAGNÓSTICO
-- ============================================

-- 1.1 Verificar total de procedimentos
SELECT COUNT(*) AS TOTAL_PROCEDIMENTOS FROM TB_PROCEDIMENTO;

-- 1.2 Verificar procedimentos com competência 202510
SELECT COUNT(*) AS COM_COMPETENCIA_202510 
FROM TB_PROCEDIMENTO 
WHERE DT_COMPETENCIA = '202510';

-- 1.3 Verificar procedimentos sem competência ou com competência diferente
SELECT 
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL
FROM TB_PROCEDIMENTO
GROUP BY DT_COMPETENCIA
ORDER BY DT_COMPETENCIA DESC;

-- 1.4 Verificar se 202510 aparece na listagem (como o sistema faz)
SELECT DISTINCT DT_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
ORDER BY DT_COMPETENCIA DESC;

-- ============================================
-- PASSO 2: CORREÇÃO
-- ============================================
-- ATENÇÃO: Execute apenas se os procedimentos realmente pertencem à competência 202510
-- Baseado no log, foram importados 4947 procedimentos para a competência 202510

-- 2.1 Atualizar procedimentos sem competência para 202510
-- (Execute apenas se tiver certeza que esses procedimentos são da competência 202510)
UPDATE TB_PROCEDIMENTO
SET DT_COMPETENCIA = '202510'
WHERE DT_COMPETENCIA IS NULL 
   OR DT_COMPETENCIA = ''
   OR LENGTH(TRIM(DT_COMPETENCIA)) = 0;

-- 2.2 Se houver procedimentos com competência diferente mas que deveriam ser 202510,
-- descomente e ajuste conforme necessário:
-- UPDATE TB_PROCEDIMENTO
-- SET DT_COMPETENCIA = '202510'
-- WHERE DT_COMPETENCIA = 'OUTRA_COMPETENCIA';  -- Substitua pela competência incorreta

-- ============================================
-- PASSO 3: VALIDAÇÃO
-- ============================================

-- 3.1 Verificar novamente se 202510 aparece na listagem
SELECT DISTINCT DT_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
ORDER BY DT_COMPETENCIA DESC;

-- 3.2 Contar procedimentos por competência após correção
SELECT 
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_REGISTROS
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
GROUP BY DT_COMPETENCIA
ORDER BY DT_COMPETENCIA DESC;

-- 3.3 Verificar TB_COMPETENCIA_ATIVA (registro de controle)
SELECT 
    DT_COMPETENCIA,
    ST_ATIVA,
    DT_ATIVACAO
FROM TB_COMPETENCIA_ATIVA
WHERE DT_COMPETENCIA = '202510';

-- ============================================
-- INSTRUÇÕES DE USO:
-- ============================================
-- 1. Execute o PASSO 1 primeiro para ver o estado atual
-- 2. Se a competência 202510 não aparecer na listagem:
--    a) Verifique quantos procedimentos têm DT_COMPETENCIA NULL ou vazia
--    b) Se esses procedimentos são da competência 202510, execute o PASSO 2.1
-- 3. Execute o PASSO 3 para confirmar que a correção funcionou
-- 4. Reinicie a aplicação e verifique se a competência aparece na listagem
-- ============================================


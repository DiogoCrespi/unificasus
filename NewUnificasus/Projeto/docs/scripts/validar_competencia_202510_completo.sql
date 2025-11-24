-- ============================================
-- SCRIPT COMPLETO PARA VALIDAR E CORRIGIR COMPETÊNCIA 202510
-- Baseado no log: ImportLog_20251122_070908.txt
-- Competência: 202510 (Outubro de 2025)
-- ============================================

-- ============================================
-- PARTE 1: DIAGNÓSTICO
-- ============================================

-- 1.1 Verificar se TB_PROCEDIMENTO tem a coluna DT_COMPETENCIA
SELECT 
    RF.RDB$FIELD_NAME AS CAMPO,
    CASE 
        WHEN F.RDB$FIELD_TYPE = 37 THEN 'VARCHAR'
        WHEN F.RDB$FIELD_TYPE = 14 THEN 'CHAR'
        ELSE 'OUTRO'
    END AS TIPO,
    F.RDB$FIELD_LENGTH AS TAMANHO
FROM RDB$RELATION_FIELDS RF
LEFT JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
WHERE RF.RDB$RELATION_NAME = 'TB_PROCEDIMENTO'
  AND RF.RDB$FIELD_NAME = 'DT_COMPETENCIA'
ORDER BY RF.RDB$FIELD_POSITION;

-- 1.2 Verificar total de procedimentos importados
SELECT COUNT(*) AS TOTAL_PROCEDIMENTOS
FROM TB_PROCEDIMENTO;

-- 1.3 Verificar procedimentos com competência 202510
SELECT COUNT(*) AS PROCEDIMENTOS_202510
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510';

-- 1.4 Verificar procedimentos sem competência
SELECT COUNT(*) AS PROCEDIMENTOS_SEM_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NULL OR DT_COMPETENCIA = '';

-- 1.5 Listar todas as competências disponíveis (como o sistema faz)
SELECT DISTINCT DT_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
ORDER BY DT_COMPETENCIA DESC;

-- 1.6 Verificar se 202510 está na listagem
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ COMPETÊNCIA 202510 ENCONTRADA'
        ELSE '✗ COMPETÊNCIA 202510 NÃO ENCONTRADA'
    END AS STATUS,
    COUNT(*) AS TOTAL_REGISTROS
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA = '202510';

-- ============================================
-- PARTE 2: CORREÇÃO (se necessário)
-- ============================================

-- 2.1 Se a coluna DT_COMPETENCIA não existir, criar (NÃO EXECUTAR se já existir)
/*
ALTER TABLE TB_PROCEDIMENTO
ADD DT_COMPETENCIA VARCHAR(6);
*/

-- 2.2 Se os procedimentos não tiverem DT_COMPETENCIA preenchida, atualizar
-- ATENÇÃO: Execute apenas se os procedimentos realmente pertencem à competência 202510
/*
UPDATE TB_PROCEDIMENTO
SET DT_COMPETENCIA = '202510'
WHERE DT_COMPETENCIA IS NULL 
   OR DT_COMPETENCIA = '';
*/

-- 2.3 Verificar TB_COMPETENCIA_ATIVA
SELECT 
    DT_COMPETENCIA,
    ST_ATIVA,
    DT_ATIVACAO
FROM TB_COMPETENCIA_ATIVA
WHERE DT_COMPETENCIA = '202510';

-- 2.4 Registrar na TB_COMPETENCIA_ATIVA se não existir
-- (Isso não faz aparecer na listagem, mas registra para controle)
/*
INSERT INTO TB_COMPETENCIA_ATIVA (DT_COMPETENCIA, ST_ATIVA, DT_ATIVACAO)
SELECT '202510', 'N', CURRENT_TIMESTAMP
FROM RDB$DATABASE
WHERE NOT EXISTS (
    SELECT 1 FROM TB_COMPETENCIA_ATIVA WHERE DT_COMPETENCIA = '202510'
);
*/

-- ============================================
-- PARTE 3: VERIFICAÇÃO FINAL
-- ============================================

-- 3.1 Verificar novamente se 202510 aparece na listagem
SELECT DISTINCT DT_COMPETENCIA
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
ORDER BY DT_COMPETENCIA DESC;

-- 3.2 Contar registros por competência
SELECT 
    DT_COMPETENCIA,
    COUNT(*) AS TOTAL_REGISTROS
FROM TB_PROCEDIMENTO
WHERE DT_COMPETENCIA IS NOT NULL
GROUP BY DT_COMPETENCIA
ORDER BY DT_COMPETENCIA DESC;

-- ============================================
-- INSTRUÇÕES:
-- ============================================
-- 1. Execute a PARTE 1 primeiro para diagnóstico
-- 2. Se a competência 202510 não aparecer:
--    a) Verifique se TB_PROCEDIMENTO tem a coluna DT_COMPETENCIA
--    b) Verifique se os procedimentos têm DT_COMPETENCIA preenchida
--    c) Se necessário, execute a PARTE 2 (com cuidado!)
-- 3. Execute a PARTE 3 para verificar se a correção funcionou
-- ============================================


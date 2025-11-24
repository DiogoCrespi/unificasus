-- Script para verificar duplicatas na tabela TB_CID
-- Baseado na importação de 202510

-- 1. Verificar estrutura da tabela TB_CID (chaves primárias)
SELECT 
    rc.RDB$CONSTRAINT_NAME AS constraint_name,
    rc.RDB$CONSTRAINT_TYPE AS constraint_type,
    s.RDB$FIELD_NAME AS field_name
FROM RDB$RELATION_CONSTRAINTS rc
JOIN RDB$INDEX_SEGMENTS s ON rc.RDB$INDEX_NAME = s.RDB$INDEX_NAME
WHERE rc.RDB$RELATION_NAME = 'TB_CID'
  AND rc.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
ORDER BY s.RDB$FIELD_POSITION;

-- 2. Contar total de registros na TB_CID para competência 202510
SELECT COUNT(*) AS total_registros
FROM TB_CID
WHERE DT_COMPETENCIA = '202510';

-- 3. Verificar duplicatas baseado em CO_CID + TP_AGRAVO + TP_SEXO + TP_ESTADIO + DT_COMPETENCIA
-- (chaves primárias típicas da TB_CID)
SELECT 
    CO_CID,
    TP_AGRAVO,
    TP_SEXO,
    TP_ESTADIO,
    DT_COMPETENCIA,
    COUNT(*) AS quantidade
FROM TB_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO, DT_COMPETENCIA
HAVING COUNT(*) > 1
ORDER BY quantidade DESC, CO_CID;

-- 4. Ver exemplos de registros duplicados (se houver)
SELECT 
    CO_CID,
    TP_AGRAVO,
    TP_SEXO,
    TP_ESTADIO,
    DT_COMPETENCIA,
    CAST(NO_CID AS VARCHAR(100)) AS NO_CID,
    INDICE
FROM TB_CID
WHERE DT_COMPETENCIA = '202510'
  AND (CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO, DT_COMPETENCIA) IN (
    SELECT CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO, DT_COMPETENCIA
    FROM TB_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
  )
ORDER BY CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO, INDICE;

-- 5. Verificar se há registros com mesmo CO_CID mas diferentes valores de NO_CID
-- (isso indicaria que a atualização não funcionou)
SELECT 
    CO_CID,
    TP_AGRAVO,
    TP_SEXO,
    TP_ESTADIO,
    DT_COMPETENCIA,
    COUNT(DISTINCT CAST(NO_CID AS VARCHAR(200))) AS diferentes_descricoes,
    COUNT(*) AS total_registros
FROM TB_CID
WHERE DT_COMPETENCIA = '202510'
GROUP BY CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO, DT_COMPETENCIA
HAVING COUNT(DISTINCT CAST(NO_CID AS VARCHAR(200))) > 1
ORDER BY diferentes_descricoes DESC, CO_CID;

-- 6. Verificar registros específicos mencionados no log (linhas com erro)
-- Exemplos: A150, A155, A158, A159, A163, A164, A168, A169
SELECT 
    CO_CID,
    TP_AGRAVO,
    TP_SEXO,
    TP_ESTADIO,
    DT_COMPETENCIA,
    CAST(NO_CID AS VARCHAR(100)) AS NO_CID,
    INDICE
FROM TB_CID
WHERE DT_COMPETENCIA = '202510'
  AND CO_CID IN ('A150', 'A155', 'A158', 'A159', 'A163', 'A164', 'A168', 'A169')
ORDER BY CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO, INDICE;

-- 7. Verificar quantos registros únicos existem (sem duplicatas)
SELECT 
    COUNT(DISTINCT CO_CID || '|' || TP_AGRAVO || '|' || TP_SEXO || '|' || TP_ESTADIO || '|' || DT_COMPETENCIA) AS registros_unicos,
    COUNT(*) AS total_registros,
    COUNT(*) - COUNT(DISTINCT CO_CID || '|' || TP_AGRAVO || '|' || TP_SEXO || '|' || TP_ESTADIO || '|' || DT_COMPETENCIA) AS duplicatas
FROM TB_CID
WHERE DT_COMPETENCIA = '202510';


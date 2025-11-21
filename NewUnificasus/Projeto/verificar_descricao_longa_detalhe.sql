-- Verificar todos os campos da tabela TB_DETALHE para encontrar o campo de descrição longa

-- 1. Ver estrutura completa de TB_DETALHE
SELECT FIRST 1 * FROM TB_DETALHE WHERE DT_COMPETENCIA = '201108';

-- 2. Buscar campos que podem conter descrição longa (MEMO/BLOB)
SELECT 
    d.CO_DETALHE,
    d.NO_DETALHE,
    d.DT_COMPETENCIA,
    d.DE_DETALHE
FROM TB_DETALHE d
WHERE d.DT_COMPETENCIA = '201108'
ORDER BY d.CO_DETALHE
ROWS 5;

-- 3. Ver exemplo específico do detalhe "020" (MONITORAMENTO DO CEO)
SELECT 
    CO_DETALHE,
    NO_DETALHE,
    DE_DETALHE,
    DT_COMPETENCIA
FROM TB_DETALHE
WHERE CO_DETALHE = '020'
  AND DT_COMPETENCIA = '201108';

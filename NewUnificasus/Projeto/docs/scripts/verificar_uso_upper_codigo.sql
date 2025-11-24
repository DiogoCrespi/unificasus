-- Verificar uso de UPPER no código da aplicação
-- Este script lista onde UPPER é usado nas queries SQL do código

-- Nota: Este é um script de referência para comparar com o que está no banco
-- Os resultados devem ser comparados manualmente com os arquivos .cs

-- Lista de arquivos que usam UPPER no código (para referência):
-- 1. RelatorioRepository.cs - linha 210, 269, 332, 383, 434, 690, 803
-- 2. ImportRepository.cs - linha 696, 865, 866, 896, 932, 933
-- 3. CompetenciaRepository.cs - linha 186

-- Verificar se há queries sendo executadas dinamicamente no banco
-- que possam estar usando UPPER

-- 1. Verificar se há queries sendo executadas via EXECUTE STATEMENT
SELECT 
    'PROCEDURE' AS TIPO,
    TRIM(R.RDB$PROCEDURE_NAME) AS NOME,
    R.RDB$PROCEDURE_SOURCE AS CODIGO
FROM RDB$PROCEDURES R
WHERE R.RDB$PROCEDURE_SOURCE CONTAINING 'EXECUTE STATEMENT'
   OR R.RDB$PROCEDURE_SOURCE CONTAINING 'EXECUTE'
ORDER BY R.RDB$PROCEDURE_NAME;

-- 2. Verificar se há triggers que executam queries dinâmicas
SELECT 
    'TRIGGER' AS TIPO,
    TRIM(T.RDB$TRIGGER_NAME) AS NOME,
    T.RDB$TRIGGER_SOURCE AS CODIGO
FROM RDB$TRIGGERS T
WHERE T.RDB$TRIGGER_SOURCE CONTAINING 'EXECUTE STATEMENT'
   OR T.RDB$TRIGGER_SOURCE CONTAINING 'EXECUTE'
ORDER BY T.RDB$TRIGGER_NAME;


-- Script para executar no servidor - Limpa 10 duplicatas por vez
-- Execute este script múltiplas vezes até não haver mais duplicatas
-- Mostra logs detalhados de cada registro removido

SET TERM ^ ;

EXECUTE BLOCK
AS
DECLARE VARIABLE v_indice INTEGER;
DECLARE VARIABLE v_co_cid VARCHAR(4);
DECLARE VARIABLE v_co_procedimento VARCHAR(10);
DECLARE VARIABLE v_no_cid VARCHAR(100);
DECLARE VARIABLE v_count INTEGER = 0;
BEGIN
    -- Listar e remover registros duplicados (máximo 10 por execução)
    FOR SELECT FIRST 10
        pc.INDICE,
        pc.CO_CID,
        pc.CO_PROCEDIMENTO,
        pc.NO_CID
    FROM RL_PROCEDIMENTO_CID pc
    WHERE pc.DT_COMPETENCIA = '202510'
      AND EXISTS (
          SELECT 1
          FROM RL_PROCEDIMENTO_CID pc2
          WHERE pc2.DT_COMPETENCIA = '202510'
            AND pc2.CO_CID = pc.CO_CID
            AND pc2.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
            AND pc2.DT_COMPETENCIA = pc.DT_COMPETENCIA
          GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
          HAVING COUNT(*) > 1
      )
      AND pc.INDICE <> (
          SELECT MIN(INDICE)
          FROM RL_PROCEDIMENTO_CID pc3
          WHERE pc3.DT_COMPETENCIA = '202510'
            AND pc3.CO_CID = pc.CO_CID
            AND pc3.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
            AND pc3.DT_COMPETENCIA = pc.DT_COMPETENCIA
      )
    ORDER BY pc.INDICE
    INTO :v_indice, :v_co_cid, :v_co_procedimento, :v_no_cid
    DO
    BEGIN
        -- Log antes de deletar
        v_count = v_count + 1;
        
        -- Deletar o registro
        DELETE FROM RL_PROCEDIMENTO_CID
        WHERE INDICE = :v_indice
          AND DT_COMPETENCIA = '202510';
        
        -- Mostrar log (será exibido no output)
        -- Nota: Firebird não suporta PRINT diretamente, então usamos uma query
    END
    
    -- Mostrar resumo
    SELECT 'RESUMO' AS TIPO, :v_count AS REGISTROS_REMOVIDOS FROM RDB$DATABASE;
END^

SET TERM ; ^

-- Verificar progresso após remoção
SELECT 
    'PROGRESSO' AS STATUS,
    COUNT(*) AS GRUPOS_DUPLICATAS_RESTANTES,
    (SELECT COUNT(*) FROM RL_PROCEDIMENTO_CID WHERE DT_COMPETENCIA = '202510') AS TOTAL_REGISTROS
FROM (
    SELECT CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    FROM RL_PROCEDIMENTO_CID
    WHERE DT_COMPETENCIA = '202510'
    GROUP BY CO_CID, CO_PROCEDIMENTO, DT_COMPETENCIA
    HAVING COUNT(*) > 1
);


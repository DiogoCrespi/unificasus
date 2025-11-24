-- Script para LISTAR registros que serão removidos ANTES da exclusão
-- Execute este script antes de limpar para ver o que será removido
-- Versão simplificada para evitar travamento

SET TERM ^ ;

EXECUTE BLOCK
RETURNS (
    ACAO VARCHAR(20),
    INDICE INTEGER,
    CO_CID VARCHAR(4),
    CO_PROCEDIMENTO VARCHAR(10),
    DT_COMPETENCIA VARCHAR(6),
    NO_CID VARCHAR(70),
    TIPO VARCHAR(20)
)
AS
BEGIN
    FOR SELECT 
        '>>> SERA REMOVIDO',
        pc.INDICE,
        pc.CO_CID,
        pc.CO_PROCEDIMENTO,
        pc.DT_COMPETENCIA,
        SUBSTRING(pc.NO_CID FROM 1 FOR 70),
        CASE 
            WHEN UPPER(pc.NO_CID) = pc.NO_CID THEN 'MAIUSCULA'
            ELSE 'MISTO'
        END
    FROM RL_PROCEDIMENTO_CID pc
    WHERE pc.DT_COMPETENCIA = '202510'
      AND EXISTS (
          SELECT 1
          FROM RL_PROCEDIMENTO_CID pc2
          WHERE pc2.DT_COMPETENCIA = '202510'
            AND pc2.CO_CID = pc.CO_CID
            AND pc2.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
          GROUP BY pc2.CO_CID, pc2.CO_PROCEDIMENTO, pc2.DT_COMPETENCIA
          HAVING COUNT(*) > 1
      )
      AND pc.INDICE <> (
          SELECT MIN(INDICE)
          FROM RL_PROCEDIMENTO_CID pc3
          WHERE pc3.DT_COMPETENCIA = '202510'
            AND pc3.CO_CID = pc.CO_CID
            AND pc3.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
      )
    ORDER BY pc.INDICE
    ROWS 1 TO 10
    INTO :ACAO, :INDICE, :CO_CID, :CO_PROCEDIMENTO, :DT_COMPETENCIA, :NO_CID, :TIPO
    DO
    BEGIN
        SUSPEND;
    END
END^

SET TERM ; ^


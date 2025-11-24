-- Aumenta o tamanho das colunas que contêm texto com acentos
-- Para acomodar caracteres acentuados que podem ocupar mais bytes
-- Execute sempre conectado ao banco correto e preferencialmente após um backup.

SET AUTODDL OFF;
SET TERM ^^ ;

/* ---------------------
   TB_RUBRICA - Aumenta NO_RUBRICA para 200 bytes
   (acomoda acentos e margem de segurança)
--------------------- */
EXECUTE BLOCK AS
DECLARE VARIABLE CURRENT_LENGTH INTEGER;
BEGIN
    SELECT F.RDB$FIELD_LENGTH
      FROM RDB$RELATION_FIELDS RF
      JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
     WHERE TRIM(RF.RDB$RELATION_NAME) = 'TB_RUBRICA'
       AND TRIM(RF.RDB$FIELD_NAME) = 'NO_RUBRICA'
      INTO :CURRENT_LENGTH;

    IF (COALESCE(:CURRENT_LENGTH, 0) < 200) THEN
    BEGIN
        EXECUTE STATEMENT 'ALTER TABLE TB_RUBRICA ALTER COLUMN NO_RUBRICA TYPE VARCHAR(200)';
    END
END^^

/* ---------------------
   TB_CID - Aumenta NO_CID para 200 bytes
   (acomoda acentos e margem de segurança)
--------------------- */
EXECUTE BLOCK AS
DECLARE VARIABLE CURRENT_LENGTH INTEGER;
BEGIN
    SELECT F.RDB$FIELD_LENGTH
      FROM RDB$RELATION_FIELDS RF
      JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
     WHERE TRIM(RF.RDB$RELATION_NAME) = 'TB_CID'
       AND TRIM(RF.RDB$FIELD_NAME) = 'NO_CID'
      INTO :CURRENT_LENGTH;

    IF (COALESCE(:CURRENT_LENGTH, 0) < 200) THEN
    BEGIN
        EXECUTE STATEMENT 'ALTER TABLE TB_CID ALTER COLUMN NO_CID TYPE VARCHAR(200)';
    END
END^^

SET TERM ; ^^
COMMIT;
SET AUTODDL ON;


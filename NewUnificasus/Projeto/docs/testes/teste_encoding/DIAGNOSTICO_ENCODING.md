# Diagnóstico de Problemas de Encoding

## Problema Reportado

Ainda aparecem símbolos estranhos como:
- "ATENO BSICA" (deveria ser "ATENÇÃO BÁSICA")

## Possíveis Causas

1. **Os dados no banco podem estar em uma codificação diferente de Windows-1252**
2. **O CAST para BLOB pode não estar funcionando corretamente para alguns campos**
3. **O driver Firebird pode estar fazendo conversões intermediárias**

## Soluções Implementadas

### 1. Melhorias no FirebirdReaderHelper

- Adicionada verificação de caracteres de substituição Unicode (`\uFFFD`)
- Múltiplas estratégias de leitura de bytes
- Fallback para diferentes codificações
- Detecção de acentos para validar a codificação correta

### 2. Estratégias de Leitura

1. **Leitura direta como byte[]** - Mais confiável
2. **GetBytes()** - Para campos VARCHAR
3. **GetChars()** - Alternativa para obter bytes via caracteres
4. **Reconversão via Latin1** - Preserva bytes 1:1
5. **Múltiplas codificações** - Tenta Windows-1252, Latin1, DOS, etc.

## Como Diagnosticar

### Opção 1: Verificar os Bytes Brutos

Execute uma query SQL diretamente no banco:

```sql
SELECT 
    CO_GRUPO,
    CAST(NO_GRUPO AS BLOB) AS NO_GRUPO_BLOB,
    NO_GRUPO
FROM TB_GRUPO
WHERE NO_GRUPO CONTAINING 'ATEN'
ROWS 1;
```

### Opção 2: Usar o Utilitário de Teste

O arquivo `TesteEncodingManual.cs` pode ser usado para diagnosticar o problema.

### Opção 3: Verificar o Charset do Banco

Execute o script `VERIFICAR_CHARSET_BANCO.sql` para verificar qual charset o banco está usando.

## Próximos Passos

Se o problema persistir:

1. **Verificar o charset real do banco** - Pode não ser Windows-1252
2. **Testar diferentes codificações** - O banco pode estar usando Latin1, UTF-8, etc.
3. **Verificar se os dados foram importados corretamente** - Os dados podem ter sido corrompidos na importação
4. **Considerar mudar Charset=NONE para Charset=WIN1252** - Pode resolver, mas pode causar outros problemas

## Notas

- O caractere `\uFFFD` () é o caractere de substituição Unicode que aparece quando há problemas de encoding
- O caractere `?` também pode aparecer quando há problemas de encoding
- A detecção de acentos ajuda a validar se a codificação está correta


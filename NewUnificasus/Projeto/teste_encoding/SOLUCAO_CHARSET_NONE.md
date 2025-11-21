# Solução para Charset=NONE

## Problema

O banco Firebird não suporta `Charset=WIN1252`, então voltamos para `Charset=NONE` e fazemos conversão manual.

## Solução Implementada

### 1. String de Conexão
- Usa `Charset=NONE` para evitar erro de charset
- O Firebird retorna os dados como bytes brutos

### 2. Queries com CAST para BLOB
Todas as queries de texto usam `CAST` para BLOB para garantir acesso aos bytes brutos:
- `CAST(NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB`
- `CAST(NO_GRUPO AS BLOB) AS NO_GRUPO_BLOB`
- `CAST(NO_SUB_GRUPO AS BLOB) AS NO_SUB_GRUPO_BLOB`
- `CAST(NO_FORMA_ORGANIZACAO AS BLOB) AS NO_FORMA_ORGANIZACAO_BLOB`

### 3. Helper Prioriza Leitura de Bytes
O `FirebirdReaderHelper` agora:
1. **Prioriza leitura como byte[]** - Mais confiável com Charset=NONE
2. **Usa GetBytes()** - Para ler bytes brutos mesmo quando driver retorna string
3. **Converte para Windows-1252** - Após obter os bytes brutos

### 4. Leitura nos Repositórios
Os repositórios priorizam leitura do BLOB:
```csharp
NoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "NO_GRUPO_BLOB") 
         ?? FirebirdReaderHelper.GetStringSafe(reader, "NO_GRUPO")
```

## Como Funciona

1. Query retorna campo normal + BLOB (CAST)
2. Helper tenta ler BLOB primeiro (bytes brutos)
3. Se BLOB não disponível, tenta campo normal usando GetBytes()
4. Converte bytes para Windows-1252
5. Retorna string com acentos corretos

## Teste

Execute a aplicação e verifique:
- ✅ Não deve dar erro de charset
- ✅ Grupos devem aparecer com acentos: "PROMOÇÃO", "PREVENÇÃO", etc.
- ✅ Procedimentos devem aparecer com acentos: "CALÇADOS", "ORTOPÉDICOS", etc.

## Se Ainda Houver Problemas

Se ainda aparecerem caracteres `\uFFFD` ():
1. Verifique se as queries estão usando CAST para BLOB
2. Verifique se o helper está lendo o BLOB primeiro
3. Pode ser necessário ajustar a conversão de encoding



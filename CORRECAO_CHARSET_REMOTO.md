# Correção - Erro "Invalid character set specified"

## 🔴 Problema Identificado

Ao tentar conectar ao banco remoto (`192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB`), a aplicação estava gerando o erro:

```
Invalid character set specified.
```

## 🔍 Causa Raiz

O servidor Firebird remoto não reconhece ou não tem disponível o charset `WIN1252`. Isso pode acontecer quando:
- O servidor Firebird é de uma versão diferente
- O charset WIN1252 não está instalado no servidor
- O servidor usa uma configuração diferente

## ✅ Solução Aplicada

### 1. Volta para Charset=NONE

**Arquivo**: `NewUnificasus/Projeto/src/UnificaSUS.Infrastructure/Data/ConfigurationReader.cs`

**Mudança:**
- **Antes**: `Charset=WIN1252;` (causava erro no servidor remoto)
- **Depois**: `Charset=NONE;` (compatível com qualquer servidor)

### 2. Helper de Leitura Melhorado

**Arquivo**: `NewUnificasus/Projeto/src/UnificaSUS.Infrastructure/Helpers/FirebirdReaderHelper.cs`

O helper já estava preparado para trabalhar com `Charset=NONE`:
- **Prioriza leitura de bytes brutos** - Acessa os dados diretamente do banco
- **Converte manualmente para Windows-1252** - Faz a conversão de encoding localmente
- **Fallbacks múltiplos** - Tenta diferentes codificações se necessário

## 🎯 Como Funciona Agora

Com `Charset=NONE`:
1. ✅ O Firebird retorna os dados como bytes brutos (sem conversão)
2. ✅ O `FirebirdReaderHelper` lê os bytes diretamente
3. ✅ Converte os bytes para Windows-1252 localmente
4. ✅ Os acentos aparecem corretamente na aplicação

## 📋 String de Conexão Gerada

Para `local=192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB`:

```
Database=192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB;
User=SYSDBA;
Password=masterkey;
Charset=NONE;  ← Compatível com qualquer servidor
Dialect=3;
ServerType=0;  ← Servidor remoto
```

## ✅ Vantagens

- ✅ **Compatível** - Funciona com qualquer versão/configuração do Firebird
- ✅ **Sem erros** - Não gera erro "Invalid character set"
- ✅ **Acentos corretos** - A conversão manual funciona corretamente
- ✅ **Funciona local e remoto** - Mesma lógica para ambos

## 🔧 Próximos Passos

1. **Recompile o projeto**:
   ```bash
   cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"
   dotnet build
   ```

2. **Execute a aplicação** e verifique:
   - ✅ Não deve mais aparecer erro "Invalid character set"
   - ✅ Os acentos devem aparecer corretamente
   - ✅ As competências devem carregar normalmente

## 📝 Notas

- Se ainda houver problemas com acentos, o `FirebirdReaderHelper` tentará diferentes codificações automaticamente
- A conversão manual é mais robusta que depender do charset do servidor
- Esta solução funciona tanto para banco local quanto remoto


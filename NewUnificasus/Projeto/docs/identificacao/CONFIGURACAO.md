# Configuração - UnificaSUS

## 📄 Arquivo de Configuração

O arquivo `unificasus.ini` localizado em `C:\Program Files\claupers\unificasus\unificasus.ini` é usado para configurar a conexão com o banco de dados.

### Formato do Arquivo

```ini
[DB]
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

### Estrutura

- **Seção**: `[DB]`
- **Chave**: `local`
- **Valor**: String de conexão Firebird no formato `host:caminho_do_banco.gdb`

### Exemplos

#### Banco Local

```ini
[DB]
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

#### Banco Remoto

```ini
[DB]
local=192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB
```

#### Banco Embedded (Firebird Embedded)

```ini
[DB]
local=C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

## 🔧 Leitura da Configuração

A classe `ConfigurationReader` lê o arquivo e constrói a string de conexão Firebird:

```csharp
public class ConfigurationReader
{
    private const string ConfigFile = @"C:\Program Files\claupers\unificasus\unificasus.ini";
    
    public string GetConnectionString()
    {
        var config = File.ReadAllLines(ConfigFile);
        // Parse do arquivo e construção da string de conexão
    }
}
```

## 🔐 Credenciais

### Usuário Padrão Firebird

- **Usuário**: `SYSDBA`
- **Senha**: `masterkey`

**⚠️ ATENÇÃO**: Em produção, altere as credenciais padrão!

## 📝 Parâmetros de Conexão Firebird

A string de conexão completa é construída automaticamente:

```
Database={caminho_do_banco};
User=SYSDBA;
Password=masterkey;
Charset=WIN1252;
Dialect=3;
Role=;
Connection lifetime=0;
Connection timeout=15;
Pooling=true;
Packet Size=8192;
ServerType=0;
```

## 🔄 Mudança de Configuração

Para mudar o banco de dados:

1. Abra o arquivo `unificasus.ini`
2. Altere o valor da chave `local`
3. Reinicie a aplicação

**Exemplo**:

```ini
[DB]
# Antes:
local=192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB

# Depois:
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
```

## ✅ Validação

A aplicação valida:
- Existência do arquivo de configuração
- Formato correto do arquivo
- Existência do banco de dados
- Capacidade de conexão com o banco

## 📍 Localização do Arquivo

O arquivo deve estar em:

```
C:\Program Files\claupers\unificasus\unificasus.ini
```

Este caminho é fixo na aplicação. Se precisar mudar, altere a constante `ConfigFile` na classe `ConfigurationReader`.

## 🔍 Troubleshooting

### Erro: Arquivo não encontrado

**Solução**: Certifique-se de que o arquivo existe no caminho correto.

### Erro: Formato inválido

**Solução**: Verifique se o arquivo tem a estrutura correta:
```ini
[DB]
local=host:caminho
```

### Erro: Banco não encontrado

**Solução**: Verifique se o caminho do banco está correto e se o arquivo `.GDB` existe.

### Erro: Falha na conexão

**Solução**: 
- Verifique se o Firebird está rodando (se for servidor)
- Verifique as credenciais
- Verifique se o banco não está em uso por outra aplicação


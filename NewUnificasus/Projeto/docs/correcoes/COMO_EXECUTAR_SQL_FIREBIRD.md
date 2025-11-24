# Como Executar SQL no Firebird

## 📋 Opções Disponíveis

### 1. **isql (Firebird Interactive SQL)** - Linha de Comando ⚡

**Localização**: `C:\Program Files\Firebird\Firebird_3_0\isql.exe`

#### Executar um arquivo SQL:
```powershell
cd "C:\Program Files\Firebird\Firebird_3_0"
.\isql.exe -user SYSDBA -password masterkey "C:\Program Files\claupers\unificasus\NewUnificasus\backup_servidor_remoto\UNIFICASUS.GDB" -i "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto\verificar_tabelas_proc_comuns.sql"
```

#### Executar SQL interativo:
```powershell
.\isql.exe -user SYSDBA -password masterkey "C:\Program Files\claupers\unificasus\NewUnificasus\backup_servidor_remoto\UNIFICASUS.GDB"
```

Depois digite seus comandos SQL e termine com `;` e pressione Enter.

#### Para sair do isql:
```
QUIT;
```

---

### 2. **Script PowerShell Automatizado** 🚀

Use o script que criei: `executar_verificacao_proc_comuns.ps1`

**Como usar:**
1. Abra o PowerShell como Administrador
2. Navegue até a pasta do projeto:
   ```powershell
   cd "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"
   ```
3. Execute o script:
   ```powershell
   .\executar_verificacao_proc_comuns.ps1
   ```

**O script irá:**
- Verificar se o Firebird está instalado
- Executar o SQL automaticamente
- Salvar o resultado em `resultado_verificacao_proc_comuns.txt`

---

### 3. **Ferramentas Gráficas** 🖥️

#### **FlameRobin** (Recomendado - Gratuito)
- **Download**: https://www.flamerobin.org/
- **Uso**: 
  1. Instale o FlameRobin
  2. Crie uma nova conexão:
     - Host: `localhost` (ou IP do servidor)
     - Database: Caminho completo do arquivo `.GDB`
     - User: `SYSDBA`
     - Password: `masterkey`
  3. Conecte
  4. Abra o arquivo SQL (`verificar_tabelas_proc_comuns.sql`)
  5. Execute (F5 ou botão Execute)

#### **IBExpert** (Pago, mas tem versão trial)
- **Download**: https://ibexpert.com/
- Interface profissional com muitas funcionalidades

#### **DBeaver** (Gratuito, Multi-banco)
- **Download**: https://dbeaver.io/
- Suporta Firebird e muitos outros bancos

---

### 4. **Executar SQL Diretamente via PowerShell** 💻

```powershell
# Configurações
$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0"
$DatabasePath = "C:\Program Files\claupers\unificasus\NewUnificasus\backup_servidor_remoto\UNIFICASUS.GDB"
$User = "SYSDBA"
$Password = "masterkey"

# SQL a executar
$sql = @"
SELECT RF.RDB`$RELATION_NAME AS TABELA
FROM RDB`$RELATIONS RF
WHERE RF.RDB`$SYSTEM_FLAG = 0
  AND RF.RDB`$RELATION_TYPE = 0
ORDER BY RF.RDB`$RELATION_NAME;
"@

# Criar arquivo temporário
$tempFile = [System.IO.Path]::GetTempFileName()
$sql | Out-File -FilePath $tempFile -Encoding ASCII

# Executar
& "$FirebirdPath\isql.exe" -user $User -password $Password $DatabasePath -i $tempFile

# Limpar
Remove-Item $tempFile
```

---

## 🔧 Configurações Importantes

### Para Banco Local:
```
Database: C:\Program Files\claupers\unificasus\NewUnificasus\backup_servidor_remoto\UNIFICASUS.GDB
User: SYSDBA
Password: masterkey (ou a senha configurada)
```

### Para Banco Remoto:
```
Database: 192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB
User: SYSDBA
Password: masterkey (ou a senha configurada)
```

---

## 📝 Exemplo de Uso Rápido

### Ver todas as tabelas:
```sql
SELECT RF.RDB$RELATION_NAME AS TABELA
FROM RDB$RELATIONS RF
WHERE RF.RDB$SYSTEM_FLAG = 0
  AND RF.RDB$RELATION_TYPE = 0
ORDER BY RF.RDB$RELATION_NAME;
```

### Ver estrutura de uma tabela:
```sql
SELECT 
    RF.RDB$FIELD_NAME AS CAMPO,
    RF.RDB$FIELD_SOURCE AS TIPO
FROM RDB$RELATION_FIELDS RF
WHERE RF.RDB$RELATION_NAME = 'TB_PROCEDIMENTO'
ORDER BY RF.RDB$FIELD_POSITION;
```

---

## ⚠️ Dicas

1. **Sempre faça backup** antes de executar comandos que modificam dados
2. **Use transações** para comandos de UPDATE/INSERT/DELETE:
   ```sql
   SET AUTOCOMMIT OFF;
   -- Seus comandos aqui
   COMMIT; -- ou ROLLBACK;
   ```
3. **Termine comandos com `;`** no isql
4. **Use aspas duplas** para nomes de objetos que contêm espaços ou caracteres especiais

---

## 🎯 Recomendação

Para esta verificação específica, use o **script PowerShell** (`executar_verificacao_proc_comuns.ps1`) que já está configurado e pronto para usar!


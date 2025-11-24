# Guia de Backup e Restore - Firebird

## ⚠️ ATENÇÃO

**Sim, você pode substituir o arquivo `.GDB` diretamente**, mas há riscos:

### ❌ Riscos de Substituição Direta

1. **Banco em uso**: Se houver conexões ativas, o arquivo pode estar bloqueado ou corrompido
2. **Integridade**: Arquivo copiado durante uso pode estar inconsistente
3. **Permissões**: Pode perder permissões ou configurações do banco
4. **Transações**: Transações em andamento podem ser perdidas

### ✅ Método Seguro (Recomendado)

Use `gbak` (Firebird Backup/Restore) para garantir integridade.

---

## 📋 Métodos de Backup/Restore

### Método 1: Backup/Restore com GBak (RECOMENDADO)

#### Backup (Criar arquivo .fbk)

```batch
"C:\Program Files\Firebird\Firebird_3_0\gbak.exe" -b -user SYSDBA -password masterkey "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB" "E:\backup\UNIFICASUS_20251122.fbk"
```

#### Restore (Restaurar do .fbk)

```batch
"C:\Program Files\Firebird\Firebird_3_0\gbak.exe" -c -user SYSDBA -password masterkey "E:\backup\UNIFICASUS_20251122.fbk" "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB" -replace
```

**Vantagens:**
- ✅ Garante integridade do banco
- ✅ Valida dados durante restore
- ✅ Pode restaurar em versão diferente do Firebird
- ✅ Compacta o banco

---

### Método 2: Substituição Direta (RÁPIDO, mas arriscado)

#### Passos:

1. **PARAR todas as conexões ao banco:**
   - Fechar a aplicação UnificaSUS
   - Fechar qualquer outra aplicação conectada
   - Se for servidor, parar o serviço Firebird (opcional, mas recomendado)

2. **Fazer backup do banco atual:**
   ```batch
   copy "E:\claupers\unificasus\UNIFICASUS.GDB" "E:\backup\UNIFICASUS_BACKUP_%date:~-4,4%%date:~-7,2%%date:~-10,2%.GDB"
   ```

3. **Substituir o arquivo:**
   ```batch
   copy "E:\backup\UNIFICASUS_RESTORE.GDB" "E:\claupers\unificasus\UNIFICASUS.GDB" /Y
   ```

4. **Verificar permissões:**
   - O arquivo precisa ter permissões de leitura/escrita para o usuário do Firebird

5. **Reiniciar aplicação/serviço**

**⚠️ IMPORTANTE:**
- Só funciona se **NÃO houver conexões ativas**
- Pode corromper se o banco estiver em uso
- Não valida integridade dos dados

---

## 🔧 Scripts Automatizados

### Script de Backup (GBak)

```batch
@echo off
set GBAK_PATH=C:\Program Files\Firebird\Firebird_3_0\gbak.exe
set DB_HOST=192.168.0.3
set DB_PATH=E:\claupers\unificasus\UNIFICASUS.GDB
set BACKUP_DIR=E:\backup
set BACKUP_FILE=%BACKUP_DIR%\UNIFICASUS_%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%%time:~6,2%.fbk

echo Criando backup...
"%GBAK_PATH%" -b -user SYSDBA -password masterkey "%DB_HOST%:%DB_PATH%" "%BACKUP_FILE%"

if %ERRORLEVEL% EQU 0 (
    echo Backup criado com sucesso: %BACKUP_FILE%
) else (
    echo ERRO ao criar backup!
    pause
    exit /b 1
)
```

### Script de Restore (GBak)

```batch
@echo off
set GBAK_PATH=C:\Program Files\Firebird\Firebird_3_0\gbak.exe
set DB_HOST=192.168.0.3
set DB_PATH=E:\claupers\unificasus\UNIFICASUS.GDB
set BACKUP_FILE=%1

if "%BACKUP_FILE%"=="" (
    echo Uso: restaurar_banco.bat "E:\backup\UNIFICASUS_20251122.fbk"
    exit /b 1
)

echo Restaurando backup...
echo ATENCAO: Isso vai substituir o banco atual!
pause

"%GBAK_PATH%" -c -user SYSDBA -password masterkey "%BACKUP_FILE%" "%DB_HOST%:%DB_PATH%" -replace

if %ERRORLEVEL% EQU 0 (
    echo Banco restaurado com sucesso!
) else (
    echo ERRO ao restaurar backup!
    pause
    exit /b 1
)
```

### Script de Substituição Direta (RÁPIDO)

```batch
@echo off
set DB_PATH=E:\claupers\unificasus\UNIFICASUS.GDB
set BACKUP_FILE=%1

if "%BACKUP_FILE%"=="" (
    echo Uso: substituir_banco.bat "E:\backup\UNIFICASUS_RESTORE.GDB"
    exit /b 1
)

echo ========================================
echo ATENCAO: Substituicao Direta do Banco
echo ========================================
echo.
echo Isso vai substituir o banco atual pelo arquivo de backup.
echo.
echo ATENCAO: Certifique-se de que:
echo   1. Nenhuma aplicacao esta usando o banco
echo   2. Nao ha conexoes ativas
echo   3. Voce tem um backup do banco atual
echo.
pause

REM Fazer backup do banco atual
set BACKUP_ATUAL=E:\backup\UNIFICASUS_BACKUP_%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%.GDB
echo Criando backup do banco atual...
copy "%DB_PATH%" "%BACKUP_ATUAL%" /Y

if %ERRORLEVEL% NEQ 0 (
    echo ERRO ao criar backup do banco atual!
    pause
    exit /b 1
)

echo Backup do banco atual criado: %BACKUP_ATUAL%
echo.

REM Substituir
echo Substituindo banco...
copy "%BACKUP_FILE%" "%DB_PATH%" /Y

if %ERRORLEVEL% EQU 0 (
    echo Banco substituido com sucesso!
    echo.
    echo Backup do banco anterior: %BACKUP_ATUAL%
) else (
    echo ERRO ao substituir banco!
    pause
    exit /b 1
)

pause
```

---

## 📝 Checklist de Substituição

Antes de substituir o banco:

- [ ] **Fechar todas as aplicações** que usam o banco
- [ ] **Verificar conexões ativas** (se possível)
- [ ] **Fazer backup do banco atual** (sempre!)
- [ ] **Verificar integridade do arquivo de backup** (se usar .GDB direto)
- [ ] **Verificar permissões** do arquivo
- [ ] **Testar conexão** após substituição
- [ ] **Validar dados críticos** após restore

---

## 🔍 Verificar Conexões Ativas

### Via SQL (no servidor)

```sql
SELECT * FROM MON$ATTACHMENTS;
```

### Via PowerShell

```powershell
# Verificar se o arquivo está em uso
$file = "E:\claupers\unificasus\UNIFICASUS.GDB"
$processes = Get-Process | Where-Object {
    $_.Path -like "*firebird*" -or 
    $_.Modules.FileName -like "*firebird*"
}
```

---

## ⚡ Resposta Rápida

**Pergunta:** "Se eu copiar um .GDB de backup e substituir, continua rodando?"

**Resposta:** 
- ✅ **SIM**, mas:
  1. **Feche todas as aplicações** primeiro
  2. **Faça backup do banco atual** antes
  3. **Substitua o arquivo**
  4. **Reinicie a aplicação**

**Recomendação:** Use `gbak` para garantir integridade, mas substituição direta funciona se não houver conexões ativas.

---

## 🆘 Em Caso de Problemas

Se o banco não abrir após substituição:

1. **Restaurar backup anterior**
2. **Verificar logs do Firebird**
3. **Usar `gbak -v` para validar integridade**
4. **Verificar permissões do arquivo**


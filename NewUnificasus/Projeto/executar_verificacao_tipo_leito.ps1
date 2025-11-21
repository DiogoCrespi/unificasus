# Script PowerShell para executar verificação de Tipo de Leito no Firebird
# Este script executa o SQL usando isql (Firebird Interactive SQL)

# Configurações
$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0"
# Usando o banco remoto conforme unificasus.ini
$DatabasePath = "192.168.0.3:E:\claupers\unificasus\UNIFICASUS.GDB"

$User = "SYSDBA"
$Password = "masterkey"  # Altere se necessário
$ScriptPath = ".\verificar_tipo_leito.sql"

# Caminho do isql
$IsqlPath = Join-Path $FirebirdPath "isql.exe"

# Verificar se o isql existe
if (-not (Test-Path $IsqlPath)) {
    Write-Host "ERRO: isql.exe não encontrado em: $IsqlPath" -ForegroundColor Red
    Write-Host "Verifique se o Firebird está instalado corretamente." -ForegroundColor Yellow
    exit 1
}

# Verificar se o script existe
if (-not (Test-Path $ScriptPath)) {
    Write-Host "ERRO: Script não encontrado: $ScriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Executando verificação de Tipo de Leito..." -ForegroundColor Green
Write-Host "Banco: $DatabasePath" -ForegroundColor Cyan
Write-Host ""

# Executar o script SQL
# O isql precisa receber os comandos via stdin
$sqlCommands = Get-Content $ScriptPath -Raw

# Criar arquivo temporário com os comandos SQL
$tempFile = [System.IO.Path]::GetTempFileName()
$sqlCommands | Out-File -FilePath $tempFile -Encoding ASCII

try {
    # Executar isql
    $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempFile 2>&1
    
    # Exibir resultado
    Write-Host $result
    
    # Salvar resultado em arquivo
    $outputFile = "resultado_verificacao_tipo_leito.txt"
    $result | Out-File -FilePath $outputFile -Encoding UTF8
    Write-Host ""
    Write-Host "Resultado salvo em: $outputFile" -ForegroundColor Green
}
catch {
    Write-Host "ERRO ao executar o script: $_" -ForegroundColor Red
}
finally {
    # Remover arquivo temporário
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force
    }
}

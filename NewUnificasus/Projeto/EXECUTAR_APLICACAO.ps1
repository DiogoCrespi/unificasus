# Script PowerShell para executar a aplicação UnificaSUS

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "UnificaSUS - Iniciando Aplicacao" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Navegar para o diretório do projeto
Set-Location "C:\Program Files\claupers\unificasus\NewUnificasus\Projeto"

# Verificar se o .NET está instalado
Write-Host "Verificando .NET..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: .NET nao encontrado!" -ForegroundColor Red
    Write-Host "Por favor, instale o .NET 8.0 SDK" -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}
Write-Host ".NET $dotnetVersion encontrado" -ForegroundColor Green
Write-Host ""

# Verificar arquivo de configuração
Write-Host "Verificando arquivo de configuracao..." -ForegroundColor Yellow
$configFile = "C:\Program Files\claupers\unificasus\unificasus.ini"
if (-not (Test-Path $configFile)) {
    Write-Host "AVISO: Arquivo unificasus.ini nao encontrado!" -ForegroundColor Yellow
    Write-Host "Criando arquivo de configuracao padrao..." -ForegroundColor Yellow
    $configContent = @"
[DB]
local=localhost:C:\Program Files\claupers\unificasus\UNIFICASUS.GDB
"@
    $configContent | Out-File -FilePath $configFile -Encoding ASCII
    Write-Host "Arquivo criado: $configFile" -ForegroundColor Green
}
Write-Host ""

# Restaurar pacotes
Write-Host "Restaurando pacotes NuGet..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha ao restaurar pacotes!" -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}
Write-Host "Pacotes restaurados com sucesso" -ForegroundColor Green
Write-Host ""

# Compilar projeto
Write-Host "Compilando projeto..." -ForegroundColor Yellow
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha na compilacao!" -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}
Write-Host "Compilacao concluida com sucesso" -ForegroundColor Green
Write-Host ""

# Executar aplicação
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Iniciando aplicacao..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

dotnet run --project src\UnificaSUS.WPF\UnificaSUS.WPF.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERRO: Aplicacao finalizada com erro!" -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}

Write-Host ""
Write-Host "Aplicacao finalizada." -ForegroundColor Green
Write-Host "Pressione Enter para sair"


# Script PowerShell para verificar duplicatas na tabela TB_CID
# Baseado na importacao de competencia 202510

param(
    [string]$Competencia = "202510",
    [string]$DatabasePath = "",
    [string]$User = "SYSDBA",
    [string]$Password = "masterkey",
    [string]$IsqlPath = "C:\Program Files\Firebird\Firebird_3_0\isql.exe"
)

$ErrorActionPreference = "Stop"

Write-Host "=== VERIFICACAO DE DUPLICATAS - TB_CID ===" -ForegroundColor Cyan
Write-Host "Competencia: $Competencia" -ForegroundColor Yellow
Write-Host ""

# 1. Ler configuracao do banco se nao foi fornecida
if ([string]::IsNullOrEmpty($DatabasePath)) {
    Write-Host "[1/5] Lendo configuracao do banco..." -ForegroundColor Green
    $configPath = "C:\Program Files\claupers\unificasus\unificasus.ini"

    if (-not (Test-Path $configPath)) {
        Write-Host "ERRO: Arquivo de configuracao nao encontrado: $configPath" -ForegroundColor Red
        exit 1
    }

    $inDbSection = $false
    Get-Content $configPath | ForEach-Object {
        $line = $_.Trim()
        
        if ($line -eq "[DB]") {
            $inDbSection = $true
            return
        }
        
        if ($line.StartsWith("[") -and $inDbSection) {
            return
        }
        
        if ($inDbSection -and $line.StartsWith("local=", [System.StringComparison]::OrdinalIgnoreCase)) {
            $DatabasePath = $line.Substring(6).Trim()
        }
    }

    if ([string]::IsNullOrEmpty($DatabasePath)) {
        Write-Host "ERRO: Nao foi possivel encontrar o caminho do banco no arquivo de configuracao" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  Caminho do banco: $DatabasePath" -ForegroundColor Gray
} else {
    Write-Host "[1/5] Usando caminho do banco fornecido: $DatabasePath" -ForegroundColor Green
}

Write-Host ""

# 2. Verificar se isql existe
Write-Host "[2/5] Verificando isql..." -ForegroundColor Green

# IMPORTANTE: Este script NAO carrega DLLs do Firebird .NET
# Usa apenas isql (ferramenta de linha de comando) para evitar bloqueio de arquivos

if (-not (Test-Path $IsqlPath)) {
    # Tentar encontrar isql no PATH
    $isqlCommand = Get-Command isql -ErrorAction SilentlyContinue
    if ($isqlCommand) {
        $IsqlPath = $isqlCommand.Source
        Write-Host "  isql encontrado no PATH: $IsqlPath" -ForegroundColor Gray
    } else {
        Write-Host "ERRO: isql nao encontrado em: $IsqlPath" -ForegroundColor Red
        Write-Host "  Por favor, instale o Firebird ou forneca o caminho correto do isql.exe" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "  isql encontrado: $IsqlPath" -ForegroundColor Gray
    Write-Host "  (Usando isql - nao carrega DLLs, nao bloqueia arquivos)" -ForegroundColor Gray
}

Write-Host ""

# 3. Criar script SQL temporario
Write-Host "[3/5] Criando script SQL de verificacao..." -ForegroundColor Green

$sqlContent = @"
-- Verificacao de duplicatas na tabela TB_CID
-- Competencia: $Competencia

-- 1. Contar total de registros (TB_CID nao tem DT_COMPETENCIA, e uma tabela de referencia)
SELECT 'TOTAL: Total de registros na TB_CID' AS INFO FROM RDB`$DATABASE WHERE 1=0;
SELECT COUNT(*) AS total_registros FROM TB_CID;

-- 2. Estatisticas de registros unicos vs duplicados
SELECT 'ESTATISTICAS: Registros unicos vs duplicados' AS INFO FROM RDB`$DATABASE WHERE 1=0;
SELECT 
    COUNT(DISTINCT CO_CID || '|' || TP_AGRAVO || '|' || TP_SEXO || '|' || TP_ESTADIO) AS registros_unicos,
    COUNT(*) AS total_registros,
    COUNT(*) - COUNT(DISTINCT CO_CID || '|' || TP_AGRAVO || '|' || TP_SEXO || '|' || TP_ESTADIO) AS duplicatas
FROM TB_CID;

-- 3. Verificar duplicatas baseado em CO_CID + TP_AGRAVO + TP_SEXO + TP_ESTADIO
SELECT 'DUPLICATAS: Registros duplicados na TB_CID' AS INFO FROM RDB`$DATABASE WHERE 1=0;
SELECT 
    CO_CID,
    TP_AGRAVO,
    TP_SEXO,
    TP_ESTADIO,
    COUNT(*) AS quantidade
FROM TB_CID
GROUP BY CO_CID, TP_AGRAVO, TP_SEXO, TP_ESTADIO
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC, CO_CID;

EXIT;
"@

$tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
[System.IO.File]::WriteAllText($tempSqlFile, $sqlContent, [System.Text.Encoding]::ASCII)

Write-Host "  Script SQL criado: $tempSqlFile" -ForegroundColor Gray
Write-Host ""

# 4. Executar script SQL
Write-Host "[4/5] Executando verificacoes no banco..." -ForegroundColor Green
Write-Host ""

try {
    $output = & $IsqlPath -user $User -password $Password $DatabasePath -i $tempSqlFile 2>&1
    
    # Processar output
    $results = @{
        TotalRegistros = 0
        Duplicatas = 0
        RegistrosUnicos = 0
        ChavesPrimarias = @()
        ExemplosDuplicatas = @()
    }
    
    $currentSection = ""
    $outputText = $output -join "`n"
    
    # Extrair total de registros
    if ($outputText -match "TOTAL_REGISTROS\s+(\d+)") {
        $results.TotalRegistros = [int]$matches[1]
        Write-Host "  Total de registros: $($results.TotalRegistros)" -ForegroundColor Gray
    }
    
    # Extrair estatisticas (REGISTROS_UNICOS, TOTAL_REGISTROS, DUPLICATAS)
    # O formato do isql coloca os valores em linhas separadas após os cabeçalhos
    if ($outputText -match "REGISTROS_UNICOS") {
        # Procurar pelos números após os cabeçalhos
        $lines = $outputText -split "`n"
        $foundStats = $false
        foreach ($line in $lines) {
            $line = $line.Trim()
            # Procurar linha com apenas números (estatísticas)
            if ($line -match "^\s*(\d+)\s+(\d+)\s+(\d+)\s*$") {
                $results.RegistrosUnicos = [int]$matches[1]
                $results.TotalRegistros = [int]$matches[2]
                $results.Duplicatas = [int]$matches[3]
                $foundStats = $true
                Write-Host "  Registros unicos: $($results.RegistrosUnicos)" -ForegroundColor Gray
                Write-Host "  Total de registros: $($results.TotalRegistros)" -ForegroundColor Gray
                Write-Host "  Duplicatas: $($results.Duplicatas)" -ForegroundColor $(if ($results.Duplicatas -eq 0) { "Green" } else { "Red" })
                break
            }
        }
        
        # Se não encontrou no formato esperado, tentar parse manual
        if (-not $foundStats) {
            $numbers = ($outputText -split "`n" | Where-Object { $_ -match "^\s*\d+\s*$" } | ForEach-Object { [int]($_.Trim()) })
            if ($numbers.Count -ge 3) {
                $results.RegistrosUnicos = $numbers[0]
                $results.TotalRegistros = $numbers[1]
                $results.Duplicatas = $numbers[2]
                Write-Host "  Registros unicos: $($results.RegistrosUnicos)" -ForegroundColor Gray
                Write-Host "  Total de registros: $($results.TotalRegistros)" -ForegroundColor Gray
                Write-Host "  Duplicatas: $($results.Duplicatas)" -ForegroundColor $(if ($results.Duplicatas -eq 0) { "Green" } else { "Red" })
            }
        }
    }
    
    # Contar linhas de duplicatas (se houver)
    $duplicatasLines = ($outputText -split "`n" | Where-Object { $_ -match "^\s+\w+\s+\w+\s+\w+\s+\w+\s+\d+\s*$" }).Count
    if ($duplicatasLines -gt 0) {
        Write-Host "  Linhas de duplicatas encontradas: $duplicatasLines" -ForegroundColor Yellow
    }
    
    # Limpar assemblies carregados para liberar DLLs
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    
} catch {
    Write-Host "ERRO ao executar script SQL: $_" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
} finally {
    # Limpar arquivo temporario
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile -Force
    }
}

Write-Host ""

# 5. Mostrar resultados finais
Write-Host "[5/5] Resultados da verificacao:" -ForegroundColor Green
Write-Host ""
Write-Host "=== RESULTADOS ===" -ForegroundColor Cyan
Write-Host ""

if ($results.Duplicatas -eq 0) {
    Write-Host "[OK] NENHUMA DUPLICATA ENCONTRADA!" -ForegroundColor Green
    Write-Host ""
    Write-Host "A importacao esta funcionando corretamente." -ForegroundColor Green
    Write-Host "Os registros foram atualizados ao inves de duplicados." -ForegroundColor Green
} else {
    Write-Host "[ERRO] DUPLICATAS ENCONTRADAS: $($results.Duplicatas)" -ForegroundColor Red
    Write-Host ""
    Write-Host "RECOMENDACAO:" -ForegroundColor Yellow
    Write-Host "  Execute um script SQL para remover as duplicatas mantendo apenas um registro de cada." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Total de registros importados: $($results.TotalRegistros)" -ForegroundColor Cyan
Write-Host "Registros unicos: $($results.RegistrosUnicos)" -ForegroundColor Cyan
Write-Host "Duplicatas: $($results.Duplicatas)" -ForegroundColor $(if ($results.Duplicatas -eq 0) { "Green" } else { "Red" })
Write-Host ""
Write-Host "=== FIM DA VERIFICACAO ===" -ForegroundColor Cyan

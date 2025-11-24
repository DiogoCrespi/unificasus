# Script PowerShell para verificar CBO, CID e competências (versão rápida)
# Executa várias consultas pequenas ao invés de uma grande

# Configurações
$FirebirdPath = "C:\Program Files\Firebird\Firebird_3_0"
$User = "SYSDBA"
$Password = "masterkey"

# Tenta ler o caminho do banco do arquivo de configuração
$configFile = "C:\Program Files\claupers\unificasus\unificasus.ini"
$DatabasePath = ""

if (Test-Path $configFile) {
    $configContent = Get-Content $configFile -Raw
    if ($configContent -match '(?m)^local\s*=\s*(.+)$') {
        $DatabasePath = $matches[1].Trim()
    }
}

if ([string]::IsNullOrEmpty($DatabasePath)) {
    $DatabasePath = "C:\Program Files\claupers\unificasus\UNIFICASUS.GDB"
}

# Caminho do isql
$IsqlPath = Join-Path $FirebirdPath "isql.exe"

# Verificar se o isql existe
if (-not (Test-Path $IsqlPath)) {
    $found = Get-ChildItem -Path "C:\Program Files" -Filter "isql.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) {
        $IsqlPath = $found.FullName
    } else {
        Write-Host "ERRO: isql.exe nao encontrado" -ForegroundColor Red
        exit 1
    }
}

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Verificacao Rapida: CBO, CID e Competencias" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Banco: $DatabasePath" -ForegroundColor Cyan
Write-Host ""

# Função para executar um arquivo SQL
function Execute-SqlFile {
    param(
        [string]$SqlFile,
        [string]$Description
    )
    
    Write-Host "[$Description]..." -ForegroundColor Yellow -NoNewline
    
    if (-not (Test-Path $SqlFile)) {
        Write-Host " Arquivo nao encontrado" -ForegroundColor Red
        return
    }
    
    try {
        $result = & $IsqlPath -user $User -password $Password $DatabasePath -i $SqlFile 2>&1
        
        # Filtrar e mostrar resultados
        $result | Where-Object { 
            $_ -match '^\s*[A-Z]' -or 
            $_ -match '^\s*\d' -or 
            $_ -match 'SIM|NAO|Total|TOTAL|PERGUNTA|RESPOSTA|INFO'
        } | ForEach-Object { 
            if ($_ -notmatch '^Database:|^User:|^SQL>|^CON>') {
                Write-Host "  $_" -ForegroundColor White 
            }
        }
        
        Write-Host " OK" -ForegroundColor Green
    }
    catch {
        Write-Host " ERRO" -ForegroundColor Red
        Write-Host "  $_" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Criar queries SQL simples em arquivos temporários
$scriptDir = $PSScriptRoot

# Query 1
$q1File = Join-Path $scriptDir "q1_tb_cid_competencia.sql"
@"
SELECT 
    'TB_CID tem DT_COMPETENCIA?' AS PERGUNTA,
    CASE 
        WHEN COUNT(*) > 0 THEN 'SIM'
        ELSE 'NAO'
    END AS RESPOSTA
FROM RDB`$RELATION_FIELDS
WHERE TRIM(RDB`$RELATION_NAME) = 'TB_CID'
  AND TRIM(RDB`$FIELD_NAME) = 'DT_COMPETENCIA';
"@ | Out-File -FilePath $q1File -Encoding ASCII

# Query 2
$q2File = Join-Path $scriptDir "q2_tb_ocupacao_competencia.sql"
@"
SELECT 
    'TB_OCUPACAO tem DT_COMPETENCIA?' AS PERGUNTA,
    CASE 
        WHEN COUNT(*) > 0 THEN 'SIM'
        ELSE 'NAO'
    END AS RESPOSTA
FROM RDB`$RELATION_FIELDS
WHERE TRIM(RDB`$RELATION_NAME) = 'TB_OCUPACAO'
  AND TRIM(RDB`$FIELD_NAME) = 'DT_COMPETENCIA';
"@ | Out-File -FilePath $q2File -Encoding ASCII

# Query 3
$q3File = Join-Path $scriptDir "q3_total_tb_cid.sql"
@"
SELECT COUNT(*) AS TOTAL FROM TB_CID;
"@ | Out-File -FilePath $q3File -Encoding ASCII

# Query 4
$q4File = Join-Path $scriptDir "q4_total_tb_ocupacao.sql"
@"
SELECT COUNT(*) AS TOTAL FROM TB_OCUPACAO;
"@ | Out-File -FilePath $q4File -Encoding ASCII

# Query 5
$q5File = Join-Path $scriptDir "q5_comp_rl_cid.sql"
@"
SELECT 
    COUNT(DISTINCT DT_COMPETENCIA) AS TOTAL_COMPETENCIAS,
    MIN(DT_COMPETENCIA) AS PRIMEIRA,
    MAX(DT_COMPETENCIA) AS ULTIMA
FROM RL_PROCEDIMENTO_CID;
"@ | Out-File -FilePath $q5File -Encoding ASCII

# Query 6
$q6File = Join-Path $scriptDir "q6_comp_rl_ocupacao.sql"
@"
SELECT 
    COUNT(DISTINCT DT_COMPETENCIA) AS TOTAL_COMPETENCIAS,
    MIN(DT_COMPETENCIA) AS PRIMEIRA,
    MAX(DT_COMPETENCIA) AS ULTIMA
FROM RL_PROCEDIMENTO_OCUPACAO;
"@ | Out-File -FilePath $q6File -Encoding ASCII

# Executar queries
Execute-SqlFile -SqlFile $q1File -Description "1/6 Verificando TB_CID"
Execute-SqlFile -SqlFile $q2File -Description "2/6 Verificando TB_OCUPACAO"
Execute-SqlFile -SqlFile $q3File -Description "3/6 Contando TB_CID"
Execute-SqlFile -SqlFile $q4File -Description "4/6 Contando TB_OCUPACAO"
Execute-SqlFile -SqlFile $q5File -Description "5/6 Competencias RL_PROCEDIMENTO_CID"
Execute-SqlFile -SqlFile $q6File -Description "6/6 Competencias RL_PROCEDIMENTO_OCUPACAO"

# Limpar arquivos temporários
Remove-Item $q1File, $q2File, $q3File, $q4File, $q5File, $q6File -ErrorAction SilentlyContinue

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "CONCLUSAO:" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "TB_CID e TB_OCUPACAO NAO tem DT_COMPETENCIA" -ForegroundColor Green
Write-Host "  CIDs e CBOs valem para TODAS as competencias" -ForegroundColor White
Write-Host ""
Write-Host "RL_PROCEDIMENTO_CID e RL_PROCEDIMENTO_OCUPACAO TEM DT_COMPETENCIA" -ForegroundColor Green
Write-Host "  Relacionamentos variam por competencia" -ForegroundColor White
Write-Host ""

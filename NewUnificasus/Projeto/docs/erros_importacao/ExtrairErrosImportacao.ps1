# Script para extrair e organizar erros do log de importação
# Uso: .\ExtrairErrosImportacao.ps1 -LogFile "ImportLog_20251122_070908.txt" -OutputDir "erros"

param(
    [Parameter(Mandatory=$true)]
    [string]$LogFile,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "erros_importacao"
)

# Criar diretório de saída se não existir
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Host "Analisando log: $LogFile" -ForegroundColor Cyan
Write-Host "Diretório de saída: $OutputDir" -ForegroundColor Cyan

# Verificar se o arquivo existe
if (-not (Test-Path $LogFile)) {
    Write-Host "ERRO: Arquivo de log não encontrado: $LogFile" -ForegroundColor Red
    exit 1
}

# Ler o arquivo de log
$logContent = Get-Content $LogFile -Encoding UTF8
$totalLines = $logContent.Count
Write-Host "Total de linhas no log: $totalLines" -ForegroundColor Yellow

# Estruturas para armazenar erros
$errosPorTabela = @{}
$errosPorTipo = @{}
$todosErros = @()
$linhaAtual = 0
$erroAtual = $null
$contextoAtual = ""

# Processar cada linha do log
foreach ($line in $logContent) {
    $linhaAtual++
    
    # Detectar início de erro
    if ($line -match '\[ERROR\]') {
        # Se já havia um erro sendo processado, finalizar ele
        if ($erroAtual) {
            $todosErros += $erroAtual
            $erroAtual = $null
        }
        
        # Extrair informações do erro
        $erroAtual = @{
            LinhaLog = $linhaAtual
            Timestamp = ""
            TipoErro = ""
            Tabela = ""
            Mensagem = @()
            LinhaDados = ""
            Contexto = $contextoAtual
        }
        
        # Extrair timestamp
        if ($line -match '\[(\d{2}:\d{2}:\d{2})\]') {
            $erroAtual.Timestamp = $matches[1]
        }
        
        # Extrair tipo de erro e tabela
        if ($line -match 'Erro ao inserir registro em (\w+):\s*(.+)') {
            $erroAtual.TipoErro = "Inserção"
            $erroAtual.Tabela = $matches[1]
            $erroAtual.Mensagem += $matches[2]
        }
        elseif ($line -match 'Erro ao inserir/atualizar em (\w+):\s*(.+)') {
            $erroAtual.TipoErro = "Inserção/Atualização"
            $erroAtual.Tabela = $matches[1]
            $erroAtual.Mensagem += $matches[2]
        }
        elseif ($line -match 'Linha (\d+):\s*Erro ao inserir no banco:\s*(.+)') {
            $erroAtual.TipoErro = "Inserção no Banco"
            $erroAtual.LinhaDados = $matches[1]
            $erroAtual.Mensagem += $matches[2]
        }
        else {
            $erroAtual.TipoErro = "Erro Genérico"
            $erroAtual.Mensagem += ($line -replace '\[ERROR\]\s*', '')
        }
    }
    # Continuar coletando detalhes do erro (linhas seguintes sem [ERROR])
    elseif ($erroAtual -and $line -notmatch '\[(INFO|DEBUG|WARN|ERROR)\]') {
        $erroAtual.Mensagem += $line.Trim()
    }
    # Detectar contexto (qual tabela está sendo importada)
    elseif ($line -match '\[(\d+)/(\d+)\]\s*Importando\s+(\w+)') {
        $contextoAtual = $matches[3]
    }
    # Se encontrar uma linha de sucesso, finalizar o erro atual se houver
    elseif ($line -match '✓\s+(\w+):\s*\d+\s+sucessos') {
        if ($erroAtual) {
            $todosErros += $erroAtual
            $erroAtual = $null
        }
    }
}

# Adicionar último erro se houver
if ($erroAtual) {
    $todosErros += $erroAtual
}

Write-Host "`nTotal de erros encontrados: $($todosErros.Count)" -ForegroundColor Green

# Agrupar erros por tabela
foreach ($erro in $todosErros) {
    $tabela = if ($erro.Tabela) { $erro.Tabela } else { $erro.Contexto }
    
    if (-not $tabela) {
        $tabela = "DESCONHECIDA"
    }
    
    if (-not $errosPorTabela.ContainsKey($tabela)) {
        $errosPorTabela[$tabela] = @()
    }
    $errosPorTabela[$tabela] += $erro
    
    # Agrupar por tipo de erro
    $tipoErro = $erro.TipoErro
    if (-not $errosPorTipo.ContainsKey($tipoErro)) {
        $errosPorTipo[$tipoErro] = @()
    }
    $errosPorTipo[$tipoErro] += $erro
}

# Gerar relatório resumido
$resumoFile = Join-Path $OutputDir "RESUMO_ERROS.txt"
$resumo = @"
RELATÓRIO DE ERROS DE IMPORTAÇÃO
================================
Data/Hora: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Arquivo de Log: $LogFile
Total de Erros: $($todosErros.Count)

ERROS POR TABELA:
=================
"@

foreach ($tabela in ($errosPorTabela.Keys | Sort-Object)) {
    $qtd = $errosPorTabela[$tabela].Count
    $resumo += "`n$tabela : $qtd erro(s)"
}

$resumo += "`n`nERROS POR TIPO:"
$resumo += "`n==============="

foreach ($tipo in ($errosPorTipo.Keys | Sort-Object)) {
    $qtd = $errosPorTipo[$tipo].Count
    $resumo += "`n$tipo : $qtd erro(s)"
}

$resumo | Out-File -FilePath $resumoFile -Encoding UTF8
Write-Host "`nResumo salvo em: $resumoFile" -ForegroundColor Green

# Gerar arquivo detalhado por tabela
foreach ($tabela in $errosPorTabela.Keys) {
    $tabelaFile = Join-Path $OutputDir "ERROS_$tabela.txt"
    $conteudo = @"
ERROS DE IMPORTAÇÃO - $tabela
==============================
Total de erros: $($errosPorTabela[$tabela].Count)

"@
    
    $index = 1
    foreach ($erro in $errosPorTabela[$tabela]) {
        $conteudo += "`n--- ERRO #$index ---`n"
        $conteudo += "Linha no log: $($erro.LinhaLog)`n"
        $conteudo += "Timestamp: $($erro.Timestamp)`n"
        $conteudo += "Tipo: $($erro.TipoErro)`n"
        if ($erro.LinhaDados) {
            $conteudo += "Linha de dados: $($erro.LinhaDados)`n"
        }
        if ($erro.Contexto) {
            $conteudo += "Contexto: $($erro.Contexto)`n"
        }
        $conteudo += "Mensagem:`n"
        foreach ($msg in $erro.Mensagem) {
            $conteudo += "  $msg`n"
        }
        $index++
    }
    
    $conteudo | Out-File -FilePath $tabelaFile -Encoding UTF8
    Write-Host "Erros de $tabela salvos em: $tabelaFile" -ForegroundColor Yellow
}

# Gerar arquivo CSV para análise
$csvFile = Join-Path $OutputDir "ERROS_ANALISE.csv"
$csv = "Tabela,TipoErro,LinhaLog,LinhaDados,Timestamp,Mensagem`n"
foreach ($erro in $todosErros) {
    $tabela = if ($erro.Tabela) { $erro.Tabela } else { $erro.Contexto }
    if (-not $tabela) { $tabela = "DESCONHECIDA" }
    $mensagem = ($erro.Mensagem -join " | ") -replace '"', '""'
    $csv += """$tabela"",""$($erro.TipoErro)"",""$($erro.LinhaLog)"",""$($erro.LinhaDados)"",""$($erro.Timestamp)"",""$mensagem""`n"
}
$csv | Out-File -FilePath $csvFile -Encoding UTF8
Write-Host "`nArquivo CSV para análise salvo em: $csvFile" -ForegroundColor Green

Write-Host "`nProcessamento concluído!" -ForegroundColor Cyan
Write-Host "Arquivos gerados em: $OutputDir" -ForegroundColor Cyan


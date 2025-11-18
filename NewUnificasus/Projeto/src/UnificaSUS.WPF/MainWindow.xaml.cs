using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using ApplicationService = UnificaSUS.Application.Services;

namespace UnificaSUS.WPF;

public partial class MainWindow : Window
{
    private readonly ApplicationService.ProcedimentoService _procedimentoService;
    private readonly ApplicationService.CompetenciaService _competenciaService;
    private readonly ApplicationService.GrupoService _grupoService;
    private readonly ApplicationService.ProcedimentoComumService _procedimentoComumService;
    private readonly ApplicationService.RelatorioService _relatorioService;
    private readonly IConfigurationReader _configurationReader;
    
    private string _competenciaAtiva = string.Empty;
    private ObservableCollection<Grupo> _grupos = new();
    private ObservableCollection<Procedimento> _procedimentos = new();
    private List<ProcedimentoComum> _procedimentosComunsCache = new();
    private string _databasePath = "local";

    // Converter AAAAMM para MM/YYYY
    private static string FormatCompetencia(string competencia)
    {
        if (string.IsNullOrEmpty(competencia) || competencia.Length != 6)
            return competencia;
        
        var mes = competencia.Substring(4, 2);
        var ano = competencia.Substring(0, 4);
        return $"{mes}/{ano}";
    }

    // Converter MM/YYYY para AAAAMM
    private static string ParseCompetencia(string competenciaFormatada)
    {
        if (string.IsNullOrEmpty(competenciaFormatada) || !competenciaFormatada.Contains('/'))
            return competenciaFormatada;
        
        var parts = competenciaFormatada.Split('/');
        if (parts.Length != 2)
            return competenciaFormatada;
        
        var mes = parts[0].PadLeft(2, '0');
        var ano = parts[1];
        return $"{ano}{mes}";
    }

    private void AtualizarTitulo()
    {
        var competenciaFormatada = !string.IsNullOrEmpty(_competenciaAtiva) 
            ? FormatCompetencia(_competenciaAtiva) 
            : "Nenhuma";
        
        Title = $"Claupers UnificaSus - versão 3.0.0.2 -- Base de dados em {_databasePath} -- Competência ativa da tabela {competenciaFormatada}";
    }

    public MainWindow(
        ApplicationService.ProcedimentoService procedimentoService,
        ApplicationService.CompetenciaService competenciaService,
        ApplicationService.GrupoService grupoService,
        ApplicationService.ProcedimentoComumService procedimentoComumService,
        ApplicationService.RelatorioService relatorioService,
        IConfigurationReader configurationReader)
    {
        InitializeComponent();
        
        _procedimentoService = procedimentoService;
        _competenciaService = competenciaService;
        _grupoService = grupoService;
        _procedimentoComumService = procedimentoComumService;
        _relatorioService = relatorioService;
        _configurationReader = configurationReader;
        
        // Obter caminho do banco para título
        try
        {
            var dbPath = _configurationReader.GetDatabasePath();
            if (dbPath.Contains(':'))
            {
                var parts = dbPath.Split(':');
                _databasePath = parts.Length > 1 ? parts[0] : "local";
            }
            else
            {
                _databasePath = "local";
            }
        }
        catch
        {
            _databasePath = "local";
        }
        
        Loaded += MainWindow_Loaded;
        
        // Inicializar Collections
        ProcedimentosDataGrid.ItemsSource = _procedimentos;
        
        // Configurar altura automática das linhas do DataGrid e tooltip
        ProcedimentosDataGrid.LoadingRow += ProcedimentosDataGrid_LoadingRow;
        
        // Configurar menu de contexto (botão direito)
        ProcedimentosDataGrid.ContextMenuOpening += ProcedimentosDataGrid_ContextMenuOpening;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Carregar competência ativa
            await CarregarCompetenciaAtivaAsync();
            
            // Carregar competências disponíveis
            await CarregarCompetenciasDisponiveisAsync();
            
            // Carregar apenas grupos/categorias (não carrega procedimentos ainda)
            if (!string.IsNullOrEmpty(_competenciaAtiva))
            {
                await CarregarGruposAsync();
                
                // Carregar procedimentos comuns automaticamente
                await CarregarProcedimentosComunsAsync();
                
                // Não carrega procedimentos automaticamente - só quando o usuário selecionar
                StatusTextBlock.Text = "Selecione uma categoria para ver os procedimentos";
            }
            else
            {
                // Se não há competência ativa, mostrar mensagem
                MessageBox.Show(
                    "Nenhuma competência ativa encontrada.\n\nPor favor, selecione e ative uma competência usando o botão 'ATIVAR COMPETÊNCIA'.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Erro ao carregar dados:\n\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nErro interno: {ex.InnerException.Message}";
            }
            
            MessageBox.Show(errorMessage, 
                          "Erro", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private async Task CarregarCompetenciaAtivaAsync()
    {
        try
        {
            var competencia = await _competenciaService.BuscarAtivaAsync();
            if (competencia != null)
            {
                _competenciaAtiva = competencia.DtCompetencia;
                AtualizarTitulo();
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Erro ao carregar competência ativa:\n{ex.Message}";
            
            // Se a exceção interna contém mais detalhes, mostre-os
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nDetalhes: {ex.InnerException.Message}";
            }
            
            MessageBox.Show(errorMessage, 
                          "Aviso", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Warning);
            
            // Atualizar status bar
            StatusTextBlock.Text = "Erro ao conectar com o banco de dados";
        }
    }

    private async Task CarregarCompetenciasDisponiveisAsync()
    {
        try
        {
            var competencias = await _competenciaService.ListarDisponiveisAsync();
            // Converter para formato MM/YYYY para exibição
            var items = competencias.Select(c => new CompetenciaItem
            { 
                DtCompetencia = c,
                DisplayText = FormatCompetencia(c)
            }).ToList();
            
            CompetenciaComboBox.ItemsSource = items;
            CompetenciaComboBox.DisplayMemberPath = "DisplayText";
            
            if (!string.IsNullOrEmpty(_competenciaAtiva))
            {
                var item = items.FirstOrDefault(x => x.DtCompetencia == _competenciaAtiva);
                if (item != null)
                {
                    CompetenciaComboBox.SelectedItem = item;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar competências:\n{ex.Message}", 
                          "Erro", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private async Task CarregarGruposAsync()
    {
        try
        {
            var grupos = await _grupoService.BuscarTodosAsync(_competenciaAtiva);
            _grupos.Clear();
            
            foreach (var grupo in grupos)
            {
                _grupos.Add(grupo);
            }
            
            CategoriasTreeView.ItemsSource = _grupos;
        }
        catch (InvalidOperationException ex)
        {
            // Erro específico do banco de dados
            MessageBox.Show(ex.Message, 
                          "Erro de Banco de Dados", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Erro ao carregar categorias:\n\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nErro interno: {ex.InnerException.Message}";
            }
            
            MessageBox.Show(errorMessage, 
                          "Erro", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private async Task CarregarProcedimentosComunsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_competenciaAtiva))
            {
                return;
            }

            // Buscar todos os procedimentos comuns e atualizar cache
            var procedimentosComuns = await _procedimentoComumService.BuscarTodosAsync();
            _procedimentosComunsCache = procedimentosComuns.ToList();
            
            if (!procedimentosComuns.Any())
            {
                // Se não há procedimentos comuns, não faz nada
                return;
            }

            // Limpar lista de procedimentos
            _procedimentos.Clear();

            // Extrair códigos dos procedimentos comuns
            var codigos = procedimentosComuns
                .Where(p => !string.IsNullOrEmpty(p.PrcCodProc))
                .Select(p => p.PrcCodProc!)
                .ToList();

            if (codigos.Any())
            {
                // Buscar todos os procedimentos de uma vez (mais eficiente)
                var procedimentosEncontrados = await _procedimentoService.BuscarPorCodigosAsync(
                    codigos, 
                    _competenciaAtiva);

                foreach (var procedimento in procedimentosEncontrados)
                {
                    _procedimentos.Add(procedimento);
                }
            }

            // Atualizar DataGrid
            ProcedimentosDataGrid.ItemsSource = _procedimentos;
            
            // Atualizar status
            StatusTextBlock.Text = $"Carregados {_procedimentos.Count} procedimento(s) comum(ns)";
        }
        catch (Exception ex)
        {
            // Não mostrar erro para o usuário, apenas logar
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar procedimentos comuns: {ex.Message}");
        }
    }
    
    private ProcedimentoComum? BuscarProcedimentoComumPorCodigo(string codigoProcedimento)
    {
        return _procedimentosComunsCache.FirstOrDefault(p => p.PrcCodProc == codigoProcedimento);
    }

    private async void CategoriasTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // Ao selecionar um item no TreeView, carregar apenas os procedimentos relacionados
        await CarregarProcedimentosSelecionadosAsync();
    }

    private async Task CarregarProcedimentosSelecionadosAsync()
    {
        if (string.IsNullOrEmpty(_competenciaAtiva))
        {
            StatusTextBlock.Text = "Nenhuma competência ativa";
            return;
        }

        // Identifica qual item foi selecionado no TreeView
        var selectedItem = CategoriasTreeView.SelectedItem;
        
        // Se o SelectedItem for null, não faz nada
        if (selectedItem == null)
        {
            return;
        }

        // O TreeView com HierarchicalDataTemplate pode retornar o objeto diretamente
        // Mas vamos garantir que pegamos o DataContext se for um TreeViewItem
        object? dataContext = selectedItem;
        if (selectedItem is System.Windows.Controls.TreeViewItem treeViewItem)
        {
            dataContext = treeViewItem.DataContext ?? treeViewItem.Header;
        }

        if (dataContext == null)
        {
            return;
        }

        string? coGrupo = null;
        string? coSubGrupo = null;
        string? coFormaOrganizacao = null;
        string itemNome = "todos";

        try
        {
            // Debug: verificar o tipo do item selecionado
            var itemType = dataContext.GetType().Name;

            // Identifica o tipo de item selecionado
            if (dataContext is FormaOrganizacao formaOrganizacao)
            {
                // Selecionou uma forma de organização - carrega apenas os procedimentos dessa forma
                coGrupo = formaOrganizacao.CoGrupo;
                coSubGrupo = formaOrganizacao.CoSubGrupo;
                coFormaOrganizacao = formaOrganizacao.CoFormaOrganizacao;
                itemNome = formaOrganizacao.NoFormaOrganizacao ?? "Forma de Organização";
            }
            else if (dataContext is SubGrupo subGrupo)
            {
                // Selecionou um sub-grupo - carrega procedimentos desse sub-grupo (todas as formas)
                coGrupo = subGrupo.CoGrupo;
                coSubGrupo = subGrupo.CoSubGrupo;
                itemNome = subGrupo.NoSubGrupo ?? "Sub-Grupo";
            }
            else if (dataContext is Grupo grupo)
            {
                // Selecionou um grupo - carrega procedimentos desse grupo (todos os sub-grupos)
                coGrupo = grupo.CoGrupo;
                itemNome = grupo.NoGrupo ?? "Grupo";
            }
            else
            {
                // Tipo não reconhecido - mostra erro com mais detalhes
                StatusTextBlock.Text = $"Tipo de item não reconhecido: {itemType}";
                MessageBox.Show($"Tipo de item não reconhecido: {itemType}\n\nTipo completo: {dataContext.GetType().FullName}", 
                              "Erro", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            // Se nenhum grupo foi identificado, não carrega
            if (string.IsNullOrEmpty(coGrupo))
            {
                StatusTextBlock.Text = "Grupo não identificado";
                return;
            }

            // Busca apenas os procedimentos da estrutura selecionada
            StatusTextBlock.Text = $"Carregando procedimentos de: {itemNome}...";
            
            var procedimentos = await _procedimentoService.BuscarPorEstruturaAsync(
                coGrupo, 
                coSubGrupo, 
                coFormaOrganizacao, 
                _competenciaAtiva);
            
            _procedimentos.Clear();
            
            foreach (var procedimento in procedimentos)
            {
                _procedimentos.Add(procedimento);
            }
            
            ProcedimentosDataGrid.ItemsSource = _procedimentos;
            
            // Atualizar status
            StatusTextBlock.Text = $"Carregados {_procedimentos.Count} procedimento(s) de: {itemNome}";
        }
        catch (InvalidOperationException ex)
        {
            // Erro específico do banco de dados
            MessageBox.Show(ex.Message, 
                          "Erro de Banco de Dados", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            StatusTextBlock.Text = "Erro ao carregar procedimentos";
        }
        catch (Exception ex)
        {
            var errorMessage = $"Erro ao carregar procedimentos:\n\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nErro interno: {ex.InnerException.Message}";
            }
            
            MessageBox.Show(errorMessage, 
                          "Erro", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            StatusTextBlock.Text = "Erro ao carregar procedimentos";
        }
    }

    private void ProcedimentosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProcedimentosDataGrid.SelectedItem is Procedimento procedimento)
        {
            CarregarDetalhesProcedimento(procedimento);
            AtualizarNomeSubmenu(procedimento);
        }
        else
        {
            SubmenuHeaderTextBlock.Text = "Nenhum procedimento selecionado";
        }
    }

    private void AtualizarNomeSubmenu(Procedimento procedimento)
    {
        try
        {
            // O código do procedimento tem a estrutura: AABBCCDDDD
            // AA = Grupo (posições 1-2)
            // BB = SubGrupo (posições 3-4) - este é o "submenu"
            // CC = FormaOrganizacao (posições 5-6)
            // DDDD = Código específico
            
            if (string.IsNullOrEmpty(procedimento.CoProcedimento) || procedimento.CoProcedimento.Length < 4)
            {
                SubmenuHeaderTextBlock.Text = "Código de procedimento inválido";
                return;
            }

            var coGrupo = procedimento.CoProcedimento.Substring(0, 2);
            var coSubGrupo = procedimento.CoProcedimento.Substring(2, 2);

            // Busca o nome do subgrupo na estrutura já carregada
            var grupo = _grupos.FirstOrDefault(g => g.CoGrupo == coGrupo);
            if (grupo != null)
            {
                var subGrupo = grupo.SubGrupos.FirstOrDefault(sg => sg.CoSubGrupo == coSubGrupo);
                if (subGrupo != null && !string.IsNullOrEmpty(subGrupo.NoSubGrupo))
                {
                    SubmenuHeaderTextBlock.Text = $"{coGrupo}{coSubGrupo} - {subGrupo.NoSubGrupo}";
                    return;
                }
            }

            // Se não encontrou, mostra apenas o código
            SubmenuHeaderTextBlock.Text = $"SubGrupo: {coGrupo}{coSubGrupo}";
        }
        catch (Exception ex)
        {
            SubmenuHeaderTextBlock.Text = "Erro ao carregar submenu";
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar nome do submenu: {ex.Message}");
        }
    }

    private void ProcedimentosDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        // Configurar altura automática das linhas
        e.Row.Height = double.NaN; // Auto height
        
        // Configurar tooltip na linha com observação se existir e mostrar estrela se for comum
        if (e.Row.Item is Procedimento procedimento)
        {
            var procComum = BuscarProcedimentoComumPorCodigo(procedimento.CoProcedimento);
            
            // Configurar tooltip com observação se existir
            if (procComum != null && !string.IsNullOrWhiteSpace(procComum.PrcObservacoes))
            {
                e.Row.ToolTip = $"Observação: {procComum.PrcObservacoes}";
            }
            else
            {
                e.Row.ToolTip = null;
            }
            
        }
    }
    
    private void EstrelaTextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            // Encontrar o DataContext (Procedimento) através da árvore visual
            var parent = textBlock.Parent;
            Procedimento? procedimento = null;
            
            // Navegar pela árvore visual para encontrar o DataContext do procedimento
            while (parent != null)
            {
                if (parent is DataGridCell cell && cell.DataContext is Procedimento proc)
                {
                    procedimento = proc;
                    break;
                }
                if (parent is DataGridRow row && row.DataContext is Procedimento proc2)
                {
                    procedimento = proc2;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            if (procedimento != null)
            {
                var procComum = BuscarProcedimentoComumPorCodigo(procedimento.CoProcedimento);
                
                if (procComum != null)
                {
                    // Procedimento comum - estrela dourada e visível
                    textBlock.Visibility = Visibility.Visible;
                    textBlock.Foreground = new SolidColorBrush(Colors.Gold);
                    textBlock.ToolTip = "Procedimento comum - Clique para remover ou botão direito para gerenciar";
                }
                else
                {
                    // Procedimento não comum - estrela cinza clara e visível
                    textBlock.Visibility = Visibility.Visible;
                    textBlock.Foreground = new SolidColorBrush(Colors.LightGray);
                    textBlock.ToolTip = "Clique para adicionar aos procedimentos comuns";
                }
            }
        }
    }
    
    private async void EstrelaTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // Prevenir seleção da linha
        
        if (sender is TextBlock textBlock)
        {
            // Encontrar o DataContext (Procedimento) através da árvore visual
            var parent = textBlock.Parent;
            Procedimento? procedimento = null;
            
            // Navegar pela árvore visual para encontrar o DataContext do procedimento
            while (parent != null)
            {
                if (parent is DataGridCell cell && cell.DataContext is Procedimento proc)
                {
                    procedimento = proc;
                    break;
                }
                if (parent is DataGridRow row && row.DataContext is Procedimento proc2)
                {
                    procedimento = proc2;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            if (procedimento != null)
            {
                var procComum = BuscarProcedimentoComumPorCodigo(procedimento.CoProcedimento);
                
                if (procComum != null)
                {
                    // Se já é comum, remover
                    await RemoverProcedimentoComumDoContextMenuAsync(procComum);
                }
                else
                {
                    // Se não é comum, adicionar
                    await AdicionarProcedimentoComumDoContextMenuAsync(procedimento);
                }
            }
        }
    }

    private void CarregarDetalhesProcedimento(Procedimento procedimento)
    {
        TxtProcedimento.Text = $"{procedimento.CoProcedimento} {procedimento.NoProcedimento ?? ""}";
        TxtValorSA.Text = procedimento.VlSa?.ToString("C", new System.Globalization.CultureInfo("pt-BR")) ?? "R$ 0,00";
        TxtValorSH.Text = procedimento.VlSh?.ToString("C", new System.Globalization.CultureInfo("pt-BR")) ?? "R$ 0,00";
        TxtValorSP.Text = procedimento.VlSp?.ToString("C", new System.Globalization.CultureInfo("pt-BR")) ?? "R$ 0,00";
        
        // Total Ambulatorial (T.A.) = VL_SA + VL_SP (se VL_TA não existir no banco)
        // Total Hospitalar (T.H.) = VL_SH + VL_SP (se VL_TH não existir no banco)
        var totalAmbulatorial = procedimento.VlTa ?? (procedimento.VlSa ?? 0) + (procedimento.VlSp ?? 0);
        var totalHospitalar = procedimento.VlTh ?? (procedimento.VlSh ?? 0) + (procedimento.VlSp ?? 0);
        
        TxtValorTA.Text = totalAmbulatorial.ToString("C", new System.Globalization.CultureInfo("pt-BR"));
        TxtValorTH.Text = totalHospitalar.ToString("C", new System.Globalization.CultureInfo("pt-BR"));
        TxtPontos.Text = procedimento.QtPontos?.ToString() ?? "0";
        TxtPermanencia.Text = procedimento.QtDiasPermanencia?.ToString() ?? "";
        TxtIdMin.Text = procedimento.VlIdadeMinima?.ToString() ?? "";
        TxtIdMax.Text = procedimento.VlIdadeMaxima?.ToString() ?? "";
        
        // Sexo - sempre exibir por extenso
        var sexo = procedimento.TpSexo?.Trim()?.ToUpper();
        TxtSexo.Text = sexo switch
        {
            "M" => "Masculino",
            "F" => "Feminino",
            "I" => "Ambos",
            "N" => "Não se aplica",
            "" or null => "Não se aplica",
            _ => sexo ?? "Não se aplica"
        };
        
        TxtTempoPermanencia.Text = procedimento.QtTempoPermanencia?.ToString() ?? "";
        
        // Tipo de Financiamento - sempre exibir por extenso
        var financiamento = procedimento.Financiamento?.NoFinanciamento?.Trim() ?? procedimento.CoFinanciamento?.Trim()?.ToUpper() ?? "";
        TxtFinanciamento.Text = financiamento switch
        {
            // Códigos numéricos comuns do SUS
            "01" => "ATENÇÃO BÁSICA (PAB)",
            "02" => "MÉDIA COMPLEXIDADE",
            "03" => "ALTA COMPLEXIDADE",
            "04" => "VIGILÂNCIA EM SAÚDE",
            "05" => "ASSISTÊNCIA FARMACÊUTICA",
            "06" => "MÉDIA E ALTA COMPLEXIDADE (MAC)",
            "07" => "VIGILÂNCIA EM SAÚDE",
            "08" => "FUNDO DE AÇÕES ESTRATÉGICAS E COMPENSAÇÃO (FAEC)",
            "09" => "VIGILÂNCIA SANITÁRIA",
            "10" => "ATENÇÃO BÁSICA FIXA",
            "11" => "ATENÇÃO BÁSICA VARIÁVEL",
            "12" => "ATENÇÃO BÁSICA ESPECIAL",
            "13" => "MÉDIA COMPLEXIDADE AMBULATORIAL",
            "14" => "MÉDIA COMPLEXIDADE HOSPITALAR",
            "15" => "ALTA COMPLEXIDADE AMBULATORIAL",
            "16" => "ALTA COMPLEXIDADE HOSPITALAR",
            "17" => "URGÊNCIA E EMERGÊNCIA",
            "18" => "ATENÇÃO DOMICILIAR",
            "19" => "ATENÇÃO PSICOSSOCIAL",
            "20" => "ATENÇÃO ONCOLÓGICA",
            // Códigos alfanuméricos
            "OI" => "ORÇAMENTO IMPLÍCITO",
            "OE" => "ORÇAMENTO EXPLÍCITO",
            "PAB" => "ATENÇÃO BÁSICA (PAB)",
            "MAC" => "MÉDIA COMPLEXIDADE",
            "AC" => "ALTA COMPLEXIDADE",
            "" or null => "Não se aplica",
            _ => !string.IsNullOrEmpty(procedimento.Financiamento?.NoFinanciamento) 
                ? procedimento.Financiamento.NoFinanciamento 
                : financiamento
        };
        
        // Complexidade - sempre exibir por extenso
        // Baseado na classificação oficial do SUS (Sistema Único de Saúde)
        // IMPORTANTE: TP_COMPLEXIDADE trabalha APENAS COM NÚMEROS (0, 1, 2, 3)
        // Mapeamento correto identificado:
        // [1] = Atenção Básica
        // [2] = Média Complexidade
        var complexidade = procedimento.TpComplexidade?.Trim();
        
        // Se complexidade for null ou vazio, retorna imediatamente
        if (string.IsNullOrEmpty(complexidade))
        {
            TxtComplexidade.Text = "Não se aplica";
            return;
        }
        
        // Mapeamento baseado nos valores reais do banco
        TxtComplexidade.Text = complexidade switch
        {
            // ============================================
            // VALORES NUMÉRICOS (baseado no banco real)
            // ============================================
            // 0 = Complexidade 0 = (verificar se existe)
            "0" => "ATENÇÃO BÁSICA",
            "00" => "ATENÇÃO BÁSICA",
            // 1 = Atenção Básica (CORRIGIDO - era Média Complexidade)
            "1" => "ATENÇÃO BÁSICA",
            "01" => "ATENÇÃO BÁSICA",
            // 2 = Média Complexidade (CORRETO)
            "2" => "MÉDIA COMPLEXIDADE",
            "02" => "MÉDIA COMPLEXIDADE",
            // 3 = Alta Complexidade
            "3" => "ALTA COMPLEXIDADE",
            "03" => "ALTA COMPLEXIDADE",
            
            // ============================================
            // CÓDIGOS ALFANUMÉRICOS (caso apareçam)
            // ============================================
            "A" => "ATENÇÃO BÁSICA",
            "B" => "ATENÇÃO BÁSICA",
            "M" => "MÉDIA COMPLEXIDADE",
            "H" => "ALTA COMPLEXIDADE",
            "AB" => "ATENÇÃO BÁSICA",
            "AP" => "ATENÇÃO PRIMÁRIA",
            "AM" => "MÉDIA COMPLEXIDADE",
            "AA" => "ALTA COMPLEXIDADE",
            "AC" => "ALTA COMPLEXIDADE",
            "MC" => "MÉDIA COMPLEXIDADE",
            "BC" => "ATENÇÃO BÁSICA",
            "BASICA" => "ATENÇÃO BÁSICA",
            "BAIXA" => "ATENÇÃO BÁSICA",
            "PRIMARIA" => "ATENÇÃO PRIMÁRIA",
            "MEDIA" => "MÉDIA COMPLEXIDADE",
            "ALTA" => "ALTA COMPLEXIDADE",
            
            // ============================================
            // FALLBACK: Se não reconhecer, mostra o valor original para debug
            // ============================================
            _ => $"Complexidade: {complexidade}"
        };
    }

    private void CompetenciaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // A competência só será ativada quando clicar no botão de confirmar
    }

    private async void ConfirmarCompetencia_Click(object sender, RoutedEventArgs e)
    {
        if (CompetenciaComboBox.SelectedItem is CompetenciaItem item)
        {
            try
            {
                var competencia = item.DtCompetencia;
                if (!string.IsNullOrEmpty(competencia))
                {
                    await _competenciaService.AtivarAsync(competencia);
                    _competenciaAtiva = competencia;
                    AtualizarTitulo();
                    
                    MessageBox.Show($"Competência {FormatCompetencia(competencia)} ativada com sucesso!", 
                                  "Sucesso", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    // Recarregar apenas grupos com a nova competência (não carrega procedimentos ainda)
                    await CarregarGruposAsync();
                    _procedimentos.Clear();
                    ProcedimentosDataGrid.ItemsSource = _procedimentos;
                    StatusTextBlock.Text = "Competência ativada. Selecione uma categoria para ver os procedimentos";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao ativar competência:\n{ex.Message}", 
                              "Erro", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
    }

    private async void AtivarCompetencia_Click(object sender, RoutedEventArgs e)
    {
        await Task.Run(() => ConfirmarCompetencia_Click(sender, e));
    }

    private void NavPrevious_Click(object sender, RoutedEventArgs e)
    {
        // Navegar para registro anterior
        if (ProcedimentosDataGrid.SelectedIndex > 0)
        {
            ProcedimentosDataGrid.SelectedIndex--;
        }
    }

    private void NavFirst_Click(object sender, RoutedEventArgs e)
    {
        // Navegar para primeiro registro
        ProcedimentosDataGrid.SelectedIndex = 0;
    }

    private void NavLast_Click(object sender, RoutedEventArgs e)
    {
        // Navegar para último registro
        if (ProcedimentosDataGrid.Items.Count > 0)
        {
            ProcedimentosDataGrid.SelectedIndex = ProcedimentosDataGrid.Items.Count - 1;
        }
    }

    private void NavNext_Click(object sender, RoutedEventArgs e)
    {
        // Navegar para próximo registro
        if (ProcedimentosDataGrid.SelectedIndex < ProcedimentosDataGrid.Items.Count - 1)
        {
            ProcedimentosDataGrid.SelectedIndex++;
        }
    }

    private void NotasVersao_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Claupers UnificaSus - Versão 3.0.0.2\n\nVersão moderna refatorada em C#/.NET 8", 
                       "Notas da Versão", 
                       MessageBoxButton.OK, 
                       MessageBoxImage.Information);
    }

    private void CadastrarServico_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Funcionalidade em desenvolvimento", 
                       "Aviso", 
                       MessageBoxButton.OK, 
                       MessageBoxImage.Information);
    }

    private void DetalhamentoLink_Click(object sender, RoutedEventArgs e)
    {
        // Abrir tela de detalhamento
        MessageBox.Show("Funcionalidade em desenvolvimento", 
                       "Aviso", 
                       MessageBoxButton.OK, 
                       MessageBoxImage.Information);
    }

    private void SiteLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }

    private void Localizar_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Localizar",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var label = new Label { Content = "Digite o termo de busca:" };
        var textBox = new TextBox { Margin = new Thickness(5), Height = 25 };
        var button = new Button { Content = "Buscar", Margin = new Thickness(5), Width = 100 };

        grid.Children.Add(label);
        Grid.SetRow(textBox, 1);
        grid.Children.Add(textBox);
        Grid.SetRow(button, 2);
        grid.Children.Add(button);

        button.Click += async (s, args) =>
        {
            var filtro = textBox.Text;
            if (!string.IsNullOrEmpty(filtro) && !string.IsNullOrEmpty(_competenciaAtiva))
            {
                try
                {
                    var procedimentos = await _procedimentoService.BuscarPorFiltroAsync(filtro, _competenciaAtiva);
                    _procedimentos.Clear();
                    foreach (var proc in procedimentos)
                    {
                        _procedimentos.Add(proc);
                    }
                    ProcedimentosDataGrid.ItemsSource = _procedimentos;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro na busca:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        };

        dialog.Content = grid;
        dialog.ShowDialog();
    }

    private async void ExibirComuns_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_competenciaAtiva))
            {
                MessageBox.Show("Por favor, selecione e ative uma competência antes de exibir procedimentos comuns.", 
                              "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Buscar todos os procedimentos comuns e atualizar cache
            var procedimentosComuns = await _procedimentoComumService.BuscarTodosAsync();
            _procedimentosComunsCache = procedimentosComuns.ToList();
            
            if (!procedimentosComuns.Any())
            {
                MessageBox.Show("Não há procedimentos comuns cadastrados.", 
                              "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Limpar lista de procedimentos
            _procedimentos.Clear();

            // Extrair códigos dos procedimentos comuns
            var codigos = procedimentosComuns
                .Where(p => !string.IsNullOrEmpty(p.PrcCodProc))
                .Select(p => p.PrcCodProc!)
                .ToList();

            if (codigos.Any())
            {
                // Buscar todos os procedimentos de uma vez (mais eficiente)
                var procedimentosEncontrados = await _procedimentoService.BuscarPorCodigosAsync(
                    codigos, 
                    _competenciaAtiva);

                foreach (var procedimento in procedimentosEncontrados)
                {
                    _procedimentos.Add(procedimento);
                }
            }

            // Atualizar DataGrid
            ProcedimentosDataGrid.ItemsSource = _procedimentos;
            
            // Atualizar status
            StatusTextBlock.Text = $"Carregados {_procedimentos.Count} procedimento(s) comum(ns)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar procedimentos comuns:\n{ex.Message}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Importar_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Funcionalidade em desenvolvimento", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Relatorios_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_competenciaAtiva))
            {
                MessageBox.Show("Por favor, selecione e ative uma competência antes de gerar relatórios.", 
                              "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_relatorioService == null)
            {
                MessageBox.Show("Erro: Serviço de relatórios não foi inicializado corretamente.", 
                              "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var relatoriosWindow = new RelatoriosWindow(_relatorioService, _competenciaAtiva)
            {
                Owner = this
            };
            relatoriosWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir janela de relatórios:\n{ex.Message}\n\nDetalhes:\n{ex.StackTrace}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ProcComuns_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Buscar todos os procedimentos comuns
            var procedimentosComuns = await _procedimentoComumService.BuscarTodosAsync();
            
            // Criar diálogo para gerenciar procedimentos comuns
            var dialog = new Window
            {
                Title = "Procedimentos Comuns",
                Width = 800,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // DataGrid para exibir procedimentos comuns
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionMode = DataGridSelectionMode.Single,
                Margin = new Thickness(5)
            };

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Código",
                Binding = new System.Windows.Data.Binding("PrcCodProc"),
                Width = 120
            });

            var nomeColumn = new DataGridTextColumn
            {
                Header = "Nome do Procedimento",
                Binding = new System.Windows.Data.Binding("PrcNoProcedimento"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };
            
            // Estilo para permitir quebra de linha no nome do procedimento
            nomeColumn.ElementStyle = new Style(typeof(TextBlock));
            nomeColumn.ElementStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            nomeColumn.ElementStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.None));
            nomeColumn.ElementStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2)));
            
            dataGrid.Columns.Add(nomeColumn);

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Observações",
                Binding = new System.Windows.Data.Binding("PrcObservacoes"),
                Width = 200
            });

            dataGrid.ItemsSource = procedimentosComuns;
            
            // Label de informações (declarado antes para uso no evento)
            var labelInfo = new Label
            {
                Content = $"Total de procedimentos comuns: {procedimentosComuns.Count()}",
                Margin = new Thickness(5),
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(labelInfo, 0);
            grid.Children.Add(labelInfo);
            
            // Configurar altura automática das linhas para permitir quebra de linha
            dataGrid.LoadingRow += (s, e) =>
            {
                e.Row.Height = double.NaN; // Auto height
            };
            
            // Evento de duplo clique para editar observações
            dataGrid.MouseDoubleClick += async (s, e) =>
            {
                if (dataGrid.SelectedItem is ProcedimentoComum selecionado)
                {
                    // Verificar se o clique foi na coluna de Observações
                    var hit = e.OriginalSource as DependencyObject;
                    DataGridCell? cell = null;
                    
                    // Procura o DataGridCell na árvore visual
                    while (hit != null && cell == null)
                    {
                        if (hit is DataGridCell foundCell)
                        {
                            cell = foundCell;
                            break;
                        }
                        hit = VisualTreeHelper.GetParent(hit);
                    }
                    
                    // Se encontrou a célula e é a coluna de Observações, ou se não encontrou (comportamento padrão)
                    if (cell == null || cell.Column?.Header?.ToString() == "Observações")
                    {
                        await EditarProcedimentoComumAsync(selecionado, dialog, dataGrid, labelInfo);
                    }
                }
            };
            
            Grid.SetRow(dataGrid, 1);
            grid.Children.Add(dataGrid);

            // Botões de ação
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5)
            };

            var btnAdicionar = new Button
            {
                Content = "Adicionar",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            var btnEditar = new Button
            {
                Content = "Editar",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            var btnRemover = new Button
            {
                Content = "Remover",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            var btnFechar = new Button
            {
                Content = "Fechar",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            buttonPanel.Children.Add(btnAdicionar);
            buttonPanel.Children.Add(btnEditar);
            buttonPanel.Children.Add(btnRemover);
            buttonPanel.Children.Add(btnFechar);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // Event handlers
            btnAdicionar.Click += async (s, args) =>
            {
                await AdicionarProcedimentoComumAsync(dialog, dataGrid, labelInfo);
            };

            btnEditar.Click += async (s, args) =>
            {
                if (dataGrid.SelectedItem is ProcedimentoComum selecionado)
                {
                    await EditarProcedimentoComumAsync(selecionado, dialog, dataGrid, labelInfo);
                }
                else
                {
                    MessageBox.Show("Por favor, selecione um procedimento comum para editar.", 
                                  "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            btnRemover.Click += async (s, args) =>
            {
                if (dataGrid.SelectedItem is ProcedimentoComum selecionado)
                {
                    var confirmacao = MessageBox.Show(
                        $"Deseja realmente remover o procedimento comum '{selecionado.PrcNoProcedimento}'?",
                        "Confirmar Remoção",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirmacao == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await _procedimentoComumService.RemoverAsync(selecionado.PrcCod);
                            
                            // Atualizar cache
                            _procedimentosComunsCache.Remove(selecionado);
                            
                            // Atualizar DataGrid principal para remover a estrela
                            ProcedimentosDataGrid.Items.Refresh();
                            
                            // Recarregar lista
                            var atualizados = await _procedimentoComumService.BuscarTodosAsync();
                            dataGrid.ItemsSource = atualizados;
                            labelInfo.Content = $"Total de procedimentos comuns: {atualizados.Count()}";
                            
                            MessageBox.Show("Procedimento comum removido com sucesso!", 
                                          "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erro ao remover procedimento comum:\n{ex.Message}", 
                                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Por favor, selecione um procedimento comum para remover.", 
                                  "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            btnFechar.Click += (s, args) => dialog.Close();

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar procedimentos comuns:\n{ex.Message}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void ProcedimentosDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        // Verificar se há um procedimento selecionado
        if (dataGrid.SelectedItem is not Procedimento procedimento)
        {
            e.Handled = true; // Não mostrar menu se não há seleção
            return;
        }

        // Criar menu de contexto
        var contextMenu = new ContextMenu();

        // Verificar se o procedimento está nos procedimentos comuns
        var procComum = BuscarProcedimentoComumPorCodigo(procedimento.CoProcedimento);

        if (procComum == null)
        {
            // Procedimento NÃO está nos comuns - mostrar opção para adicionar
            var menuItemAdicionar = new MenuItem
            {
                Header = "Adicionar aos Procedimentos Comuns"
            };
            menuItemAdicionar.Click += async (s, args) =>
            {
                await AdicionarProcedimentoComumDoContextMenuAsync(procedimento);
            };
            contextMenu.Items.Add(menuItemAdicionar);
        }
        else
        {
            // Procedimento JÁ está nos comuns - mostrar opções para remover e editar observação
            var menuItemRemover = new MenuItem
            {
                Header = "Remover dos Procedimentos Comuns"
            };
            menuItemRemover.Click += async (s, args) =>
            {
                await RemoverProcedimentoComumDoContextMenuAsync(procComum);
            };
            contextMenu.Items.Add(menuItemRemover);

            var menuItemSeparador1 = new Separator();
            contextMenu.Items.Add(menuItemSeparador1);

            var menuItemEditarObs = new MenuItem
            {
                Header = "Adicionar/Editar Observação"
            };
            menuItemEditarObs.Click += async (s, args) =>
            {
                await EditarObservacaoProcedimentoComumAsync(procComum);
            };
            contextMenu.Items.Add(menuItemEditarObs);

            // Se tiver observação, mostrar no final do menu
            if (!string.IsNullOrWhiteSpace(procComum.PrcObservacoes))
            {
                var menuItemSeparador2 = new Separator();
                contextMenu.Items.Add(menuItemSeparador2);

                var menuItemObs = new MenuItem
                {
                    Header = $"Observação: {procComum.PrcObservacoes}",
                    IsEnabled = false,
                    FontStyle = FontStyles.Italic
                };
                contextMenu.Items.Add(menuItemObs);
            }
        }

        dataGrid.ContextMenu = contextMenu;
    }
    
    private async Task AdicionarProcedimentoComumDoContextMenuAsync(Procedimento procedimento)
    {
        try
        {
            // Verificar se já existe
            var existente = await _procedimentoComumService.BuscarPorCodigoProcedimentoAsync(procedimento.CoProcedimento);
            if (existente != null)
            {
                MessageBox.Show("Este procedimento já está na lista de procedimentos comuns.", 
                              "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Criar diálogo para adicionar observações
            var dialog = new Window
            {
                Title = "Adicionar Procedimento Comum",
                Width = 500,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Informações do procedimento
            var labelProc = new Label
            {
                Content = $"Procedimento: {procedimento.CoProcedimento} - {procedimento.NoProcedimento}",
                Margin = new Thickness(5),
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(labelProc, 0);
            grid.Children.Add(labelProc);

            var labelObs = new Label
            {
                Content = "Observações:",
                Margin = new Thickness(5, 10, 5, 5)
            };
            Grid.SetRow(labelObs, 1);
            grid.Children.Add(labelObs);

            var textBoxObs = new TextBox
            {
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxLength = 255
            };
            Grid.SetRow(textBoxObs, 2);
            grid.Children.Add(textBoxObs);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5)
            };

            var btnSalvar = new Button
            {
                Content = "Salvar",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            var btnCancelar = new Button
            {
                Content = "Cancelar",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            btnSalvar.Click += async (s, args) =>
            {
                try
                {
                    var proximoCodigo = await _procedimentoComumService.ObterProximoCodigoAsync();
                    
                    var novoComum = new ProcedimentoComum
                    {
                        PrcCod = proximoCodigo,
                        PrcCodProc = procedimento.CoProcedimento,
                        PrcNoProcedimento = procedimento.NoProcedimento,
                        PrcObservacoes = textBoxObs.Text.Trim()
                    };

                    await _procedimentoComumService.AdicionarAsync(novoComum);
                    
                    // Atualizar cache
                    _procedimentosComunsCache.Add(novoComum);
                    
                    // Atualizar DataGrid para mostrar a estrela
                    ProcedimentosDataGrid.Items.Refresh();
                    
                    dialog.Close();
                    
                    MessageBox.Show("Procedimento comum adicionado com sucesso!", 
                                  "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao adicionar procedimento comum:\n{ex.Message}", 
                                  "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnCancelar.Click += (s, args) => dialog.Close();

            buttonPanel.Children.Add(btnSalvar);
            buttonPanel.Children.Add(btnCancelar);
            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao adicionar procedimento comum:\n{ex.Message}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task RemoverProcedimentoComumDoContextMenuAsync(ProcedimentoComum procComum)
    {
        try
        {
            var confirmacao = MessageBox.Show(
                $"Deseja realmente remover o procedimento comum '{procComum.PrcNoProcedimento}'?",
                "Confirmar Remoção",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacao == MessageBoxResult.Yes)
            {
                await _procedimentoComumService.RemoverAsync(procComum.PrcCod);
                
                // Atualizar cache
                _procedimentosComunsCache.Remove(procComum);
                
                // Atualizar DataGrid para remover a estrela
                ProcedimentosDataGrid.Items.Refresh();
                
                // Se estiver mostrando procedimentos comuns, recarregar
                if (_procedimentos.Any())
                {
                    await CarregarProcedimentosComunsAsync();
                }
                
                MessageBox.Show("Procedimento comum removido com sucesso!", 
                              "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao remover procedimento comum:\n{ex.Message}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task EditarObservacaoProcedimentoComumAsync(ProcedimentoComum procComum)
    {
        try
        {
            var dialog = new Window
            {
                Title = "Editar Observação do Procedimento Comum",
                Width = 500,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Informações do procedimento
            var labelProc = new Label
            {
                Content = $"Procedimento: {procComum.PrcCodProc} - {procComum.PrcNoProcedimento}",
                Margin = new Thickness(5),
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(labelProc, 0);
            grid.Children.Add(labelProc);

            var labelObs = new Label
            {
                Content = "Observações:",
                Margin = new Thickness(5, 10, 5, 5)
            };
            Grid.SetRow(labelObs, 1);
            grid.Children.Add(labelObs);

            var textBoxObs = new TextBox
            {
                Margin = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxLength = 255,
                Text = procComum.PrcObservacoes ?? string.Empty
            };
            Grid.SetRow(textBoxObs, 2);
            grid.Children.Add(textBoxObs);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5)
            };

            var btnSalvar = new Button
            {
                Content = "Salvar",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            var btnCancelar = new Button
            {
                Content = "Cancelar",
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Width = 100
            };

            btnSalvar.Click += async (s, args) =>
            {
                try
                {
                    procComum.PrcObservacoes = textBoxObs.Text.Trim();
                    await _procedimentoComumService.AtualizarAsync(procComum);
                    
                    // Atualizar cache
                    var index = _procedimentosComunsCache.FindIndex(p => p.PrcCod == procComum.PrcCod);
                    if (index >= 0)
                    {
                        _procedimentosComunsCache[index] = procComum;
                    }
                    
                    dialog.Close();
                    
                    MessageBox.Show("Observação atualizada com sucesso!", 
                                  "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao atualizar observação:\n{ex.Message}", 
                                  "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            btnCancelar.Click += (s, args) => dialog.Close();

            buttonPanel.Children.Add(btnSalvar);
            buttonPanel.Children.Add(btnCancelar);
            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao editar observação:\n{ex.Message}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AdicionarProcedimentoComumAsync(Window parentDialog, DataGrid dataGrid, Label labelInfo)
    {
        // Verificar se há um procedimento selecionado na lista principal
        if (ProcedimentosDataGrid.SelectedItem is not Procedimento procedimentoSelecionado)
        {
            MessageBox.Show("Por favor, selecione um procedimento na lista principal primeiro.", 
                          "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Verificar se já existe como comum
        var existente = await _procedimentoComumService.BuscarPorCodigoProcedimentoAsync(procedimentoSelecionado.CoProcedimento);
        if (existente != null)
        {
            MessageBox.Show("Este procedimento já está na lista de procedimentos comuns.", 
                          "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Criar diálogo para adicionar observações
        var dialog = new Window
        {
            Title = "Adicionar Procedimento Comum",
            Width = 500,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = parentDialog
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Informações do procedimento
        var labelProc = new Label
        {
            Content = $"Procedimento: {procedimentoSelecionado.CoProcedimento} - {procedimentoSelecionado.NoProcedimento}",
            Margin = new Thickness(5),
            FontWeight = FontWeights.Bold
        };
        Grid.SetRow(labelProc, 0);
        grid.Children.Add(labelProc);

        var labelObs = new Label
        {
            Content = "Observações:",
            Margin = new Thickness(5, 10, 5, 5)
        };
        Grid.SetRow(labelObs, 1);
        grid.Children.Add(labelObs);

        var textBoxObs = new TextBox
        {
            Margin = new Thickness(5),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxLength = 255
        };
        Grid.SetRow(textBoxObs, 2);
        grid.Children.Add(textBoxObs);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(5)
        };

        var btnSalvar = new Button
        {
            Content = "Salvar",
            Margin = new Thickness(5, 0, 5, 0),
            Padding = new Thickness(10, 5, 10, 5),
            Width = 100
        };

        var btnCancelar = new Button
        {
            Content = "Cancelar",
            Margin = new Thickness(5, 0, 5, 0),
            Padding = new Thickness(10, 5, 10, 5),
            Width = 100
        };

        buttonPanel.Children.Add(btnSalvar);
        buttonPanel.Children.Add(btnCancelar);

        Grid.SetRow(buttonPanel, 3);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;

        btnSalvar.Click += async (s, args) =>
        {
            try
            {
                var proximoCodigo = await _procedimentoComumService.ObterProximoCodigoAsync();
                
                var novoComum = new ProcedimentoComum
                {
                    PrcCod = proximoCodigo,
                    PrcCodProc = procedimentoSelecionado.CoProcedimento,
                    PrcNoProcedimento = procedimentoSelecionado.NoProcedimento,
                    PrcObservacoes = textBoxObs.Text.Trim()
                };

                await _procedimentoComumService.AdicionarAsync(novoComum);
                
                // Atualizar cache
                _procedimentosComunsCache.Add(novoComum);
                
                // Atualizar DataGrid principal para mostrar a estrela
                ProcedimentosDataGrid.Items.Refresh();
                
                // Recarregar lista
                var atualizados = await _procedimentoComumService.BuscarTodosAsync();
                dataGrid.ItemsSource = atualizados;
                labelInfo.Content = $"Total de procedimentos comuns: {atualizados.Count()}";
                
                dialog.Close();
                MessageBox.Show("Procedimento comum adicionado com sucesso!", 
                              "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao adicionar procedimento comum:\n{ex.Message}", 
                              "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        btnCancelar.Click += (s, args) => dialog.Close();

        dialog.ShowDialog();
    }

    private Task EditarProcedimentoComumAsync(ProcedimentoComum procedimentoComum, Window parentDialog, DataGrid dataGrid, Label labelInfo)
    {
        var dialog = new Window
        {
            Title = "Editar Procedimento Comum",
            Width = 500,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = parentDialog
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var labelProc = new Label
        {
            Content = $"Procedimento: {procedimentoComum.PrcCodProc} - {procedimentoComum.PrcNoProcedimento}",
            Margin = new Thickness(5),
            FontWeight = FontWeights.Bold
        };
        Grid.SetRow(labelProc, 0);
        grid.Children.Add(labelProc);

        var labelObs = new Label
        {
            Content = "Observações:",
            Margin = new Thickness(5, 10, 5, 5)
        };
        Grid.SetRow(labelObs, 1);
        grid.Children.Add(labelObs);

        var textBoxObs = new TextBox
        {
            Margin = new Thickness(5),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxLength = 255,
            Text = procedimentoComum.PrcObservacoes ?? ""
        };
        Grid.SetRow(textBoxObs, 2);
        grid.Children.Add(textBoxObs);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(5)
        };

        var btnSalvar = new Button
        {
            Content = "Salvar",
            Margin = new Thickness(5, 0, 5, 0),
            Padding = new Thickness(10, 5, 10, 5),
            Width = 100
        };

        var btnCancelar = new Button
        {
            Content = "Cancelar",
            Margin = new Thickness(5, 0, 5, 0),
            Padding = new Thickness(10, 5, 10, 5),
            Width = 100
        };

        buttonPanel.Children.Add(btnSalvar);
        buttonPanel.Children.Add(btnCancelar);

        Grid.SetRow(buttonPanel, 3);
        grid.Children.Add(buttonPanel);

        dialog.Content = grid;

        btnSalvar.Click += async (s, args) =>
        {
            try
            {
                procedimentoComum.PrcObservacoes = textBoxObs.Text.Trim();
                await _procedimentoComumService.AtualizarAsync(procedimentoComum);
                
                // Atualizar cache
                var index = _procedimentosComunsCache.FindIndex(p => p.PrcCod == procedimentoComum.PrcCod);
                if (index >= 0)
                {
                    _procedimentosComunsCache[index] = procedimentoComum;
                }
                
                // Atualizar DataGrid principal para atualizar tooltip
                ProcedimentosDataGrid.Items.Refresh();
                
                // Recarregar lista
                var atualizados = await _procedimentoComumService.BuscarTodosAsync();
                dataGrid.ItemsSource = atualizados;
                
                dialog.Close();
                MessageBox.Show("Procedimento comum atualizado com sucesso!", 
                              "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar procedimento comum:\n{ex.Message}", 
                              "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        btnCancelar.Click += (s, args) => dialog.Close();

        dialog.ShowDialog();
        return Task.CompletedTask;
    }

    private void ProcedimentosDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        // Ordenação já é feita automaticamente pelo DataGrid
        // Este evento é apenas para garantir que funciona
        e.Handled = false;
    }

    private async void BuscarRelacionados_Click(object sender, RoutedEventArgs e)
    {
        // Verifica se há um procedimento selecionado
        if (ProcedimentosDataGrid.SelectedItem is not Procedimento procedimentoSelecionado)
        {
            MessageBox.Show("Por favor, selecione um procedimento primeiro.", 
                          "Aviso", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
            return;
        }

        // Verifica se há uma competência ativa
        if (string.IsNullOrEmpty(_competenciaAtiva))
        {
            MessageBox.Show("Nenhuma competência ativa. Por favor, ative uma competência primeiro.", 
                          "Aviso", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
            return;
        }

        // Obtém o filtro selecionado
        if (FiltrosComboBox.SelectedItem is not ComboBoxItem itemSelecionado)
        {
            MessageBox.Show("Por favor, selecione um tipo de filtro.", 
                          "Aviso", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Information);
            return;
        }

        var filtroSelecionado = itemSelecionado.Content?.ToString() ?? string.Empty;

        try
        {
            StatusTextBlock.Text = $"Buscando {filtroSelecionado} relacionados...";
            
            IEnumerable<RelacionadoItem> relacionados;

            // Busca os relacionados baseado no filtro selecionado
            switch (filtroSelecionado)
            {
                case "Cid10":
                    relacionados = await _procedimentoService.BuscarCID10RelacionadosAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "Compatíveis":
                    relacionados = await _procedimentoService.BuscarCompativeisRelacionadosAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "Habilitação":
                    relacionados = await _procedimentoService.BuscarHabilitacoesRelacionadasAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "CBO":
                    relacionados = await _procedimentoService.BuscarCBOsRelacionadosAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "Serviços":
                    relacionados = await _procedimentoService.BuscarServicosRelacionadosAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "Tipo de Leito":
                    relacionados = await _procedimentoService.BuscarTiposLeitoRelacionadosAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "Modalidade":
                    relacionados = await _procedimentoService.BuscarModalidadesRelacionadasAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "Descrição":
                    relacionados = await _procedimentoService.BuscarDescricaoRelacionadaAsync(
                        procedimentoSelecionado.CoProcedimento, _competenciaAtiva);
                    break;
                case "Instrumento de Registro":
                case "Detalhes":
                case "Incremento":
                    MessageBox.Show($"Funcionalidade '{filtroSelecionado}' ainda não implementada.", 
                                  "Aviso", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    StatusTextBlock.Text = "Pronto";
                    return;
                default:
                    MessageBox.Show($"Filtro '{filtroSelecionado}' não reconhecido.", 
                                  "Erro", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                    StatusTextBlock.Text = "Pronto";
                    return;
            }

            // Exibe os resultados em uma janela
            ExibirRelacionados(relacionados, filtroSelecionado, procedimentoSelecionado.CoProcedimento);
            
            StatusTextBlock.Text = $"Encontrados {relacionados.Count()} registro(s) de {filtroSelecionado}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao buscar relacionados:\n\n{ex.Message}", 
                          "Erro", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            StatusTextBlock.Text = "Erro ao buscar relacionados";
        }
    }

    private void ExibirRelacionados(IEnumerable<RelacionadoItem> relacionados, string tipoFiltro, string coProcedimento)
    {
        // Para "Descrição", usa uma janela maior com TextBox expansível
        bool isDescricao = tipoFiltro == "Descrição";
        
        var dialog = new Window
        {
            Title = $"{tipoFiltro} relacionados ao procedimento {coProcedimento}",
            Width = isDescricao ? 900 : 700,
            Height = isDescricao ? 600 : 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Título
        var titulo = new TextBlock
        {
            Text = $"{tipoFiltro} relacionados ao procedimento {coProcedimento}",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(10)
        };
        Grid.SetRow(titulo, 0);
        grid.Children.Add(titulo);

        // Botão de fechar (criado antes para poder ser referenciado)
        var btnFechar = new Button
        {
            Content = "Fechar",
            Width = 100,
            Height = 30,
            Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnFechar.Click += (s, e) => dialog.Close();

        // Se for Descrição e houver registros, usa TextBox expansível
        if (isDescricao && relacionados.Any())
        {
            var primeiroItem = relacionados.First();
            
            // Ajusta as linhas do grid para acomodar o TextBox
            grid.RowDefinitions[1] = new RowDefinition { Height = GridLength.Auto };
            grid.RowDefinitions.Insert(2, new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            
            // Label do código do procedimento
            var labelCodigo = new Label
            {
                Content = $"Código do Procedimento: {primeiroItem.Codigo}",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 5, 10, 5)
            };
            Grid.SetRow(labelCodigo, 1);
            grid.Children.Add(labelCodigo);
            
            // TextBox expansível com scroll para a descrição
            var textBoxDescricao = new TextBox
            {
                Text = primeiroItem.Descricao ?? "Nenhuma descrição disponível.",
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(10, 5, 10, 5),
                FontSize = 12,
                AcceptsReturn = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetRow(textBoxDescricao, 2);
            grid.Children.Add(textBoxDescricao);
            
            // Botão na linha 3
            Grid.SetRow(btnFechar, 3);
            grid.Children.Add(btnFechar);
        }
        else
        {
            // DataGrid para exibir os resultados (modo normal)
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                Margin = new Thickness(10, 5, 10, 5)
            };

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Código",
                Binding = new System.Windows.Data.Binding("Codigo"),
                Width = 150
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Descrição",
                Binding = new System.Windows.Data.Binding("Descricao"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Informação Adicional",
                Binding = new System.Windows.Data.Binding("InformacaoAdicional"),
                Width = 200
            });

            dataGrid.ItemsSource = relacionados.ToList();
            Grid.SetRow(dataGrid, 1);
            grid.Children.Add(dataGrid);
            
            // Botão na linha 2
            Grid.SetRow(btnFechar, 2);
            grid.Children.Add(btnFechar);
        }

        // Mensagem se não houver registros
        if (!relacionados.Any())
        {
            var mensagem = new TextBlock
            {
                Text = "NENHUM REGISTRO ENCONTRADO.",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red)
            };
            Grid.SetRow(mensagem, 1);
            grid.Children.Add(mensagem);
            
            // Garante que o botão está visível mesmo sem registros
            if (!grid.Children.Contains(btnFechar))
            {
                Grid.SetRow(btnFechar, 2);
                grid.Children.Add(btnFechar);
            }
        }

        dialog.Content = grid;
        dialog.ShowDialog();
    }
}

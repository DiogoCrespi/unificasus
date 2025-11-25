using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Helpers;
using ApplicationService = UnificaSUS.Application.Services;

namespace UnificaSUS.WPF;

public partial class MainWindow : Window
{
    private readonly ApplicationService.ProcedimentoService _procedimentoService;
    private readonly ApplicationService.CompetenciaService _competenciaService;
    private readonly ApplicationService.GrupoService _grupoService;
    private readonly ApplicationService.ProcedimentoComumService _procedimentoComumService;
    private readonly ApplicationService.RelatorioService _relatorioService;
    private readonly ApplicationService.ServicoClassificacaoService _servicoClassificacaoService;
    private readonly IConfigurationReader _configurationReader = null!; // Inicializado via DI no construtor
    
    private string _competenciaAtiva = string.Empty;
    private ObservableCollection<Grupo> _grupos = new();
    private ObservableCollection<Procedimento> _procedimentos = new();
    private List<ProcedimentoComum> _procedimentosComunsCache = new();
    private string _databasePath = "local";
    
    // Controle de carregamento e fila de requisições
    private bool _isLoading = false;
    private CancellationTokenSource? _currentCancellationTokenSource;
    private readonly Dictionary<string, IEnumerable<object>> _cacheRelacionados = new();
    private System.Windows.Threading.DispatcherTimer? _debounceTimer;

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
        ApplicationService.ServicoClassificacaoService servicoClassificacaoService,
        IConfigurationReader configurationReader)
    {
        InitializeComponent();
        
        // Verificar se os serviços foram injetados corretamente
        if (procedimentoService == null)
            throw new ArgumentNullException(nameof(procedimentoService));
        if (competenciaService == null)
            throw new ArgumentNullException(nameof(competenciaService));
        if (grupoService == null)
            throw new ArgumentNullException(nameof(grupoService));
        if (procedimentoComumService == null)
            throw new ArgumentNullException(nameof(procedimentoComumService));
        if (relatorioService == null)
            throw new ArgumentNullException(nameof(relatorioService));
        if (servicoClassificacaoService == null)
            throw new ArgumentNullException(nameof(servicoClassificacaoService));
        if (configurationReader == null)
            throw new ArgumentNullException(nameof(configurationReader));
        
        _procedimentoService = procedimentoService;
        _competenciaService = competenciaService;
        _grupoService = grupoService;
        _procedimentoComumService = procedimentoComumService;
        _relatorioService = relatorioService;
        _servicoClassificacaoService = servicoClassificacaoService;
        _configurationReader = configurationReader;
        
        // Obter caminho do banco para título
        try
        {
            if (_configurationReader != null)
            {
                var dbPath = _configurationReader.GetDatabasePath();
                if (!string.IsNullOrEmpty(dbPath) && dbPath.Contains(':'))
                {
                    var parts = dbPath.Split(':');
                    _databasePath = parts.Length > 1 ? parts[0] : "local";
                }
                else
                {
                    _databasePath = "local";
                }
            }
            else
            {
                _databasePath = "local";
            }
        }
        catch (Exception ex)
        {
            // Log do erro mas não interrompe a inicialização
            System.Diagnostics.Debug.WriteLine($"Erro ao ler configuração: {ex.Message}");
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
            // Verificar se os serviços estão inicializados
            if (_competenciaService == null)
            {
                throw new InvalidOperationException("Serviço de competência não foi inicializado corretamente.");
            }
            
            // Verificar se o arquivo de configuração existe
            if (_configurationReader != null && !_configurationReader.ConfigFileExists())
            {
                MessageBox.Show(
                    $"Arquivo de configuração não encontrado:\n\nC:\\Program Files\\claupers\\unificasus\\unificasus.ini\n\nPor favor, verifique se o arquivo existe.",
                    "Erro de Configuração",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            // Carregar competência ativa
            await CarregarCompetenciaAtivaAsync();
            
            // Carregar competências disponíveis
            await CarregarCompetenciasDisponiveisAsync();
            
            // Carregar apenas grupos/categorias (não carrega procedimentos ainda)
            if (!string.IsNullOrEmpty(_competenciaAtiva))
            {
                if (_grupoService == null)
                {
                    throw new InvalidOperationException("Serviço de grupos não foi inicializado corretamente.");
                }
                
                await CarregarGruposAsync();
                
                // Carregar procedimentos comuns automaticamente
                if (_procedimentoComumService != null)
                {
                    await CarregarProcedimentosComunsAsync();
                }
                
                // Não carrega procedimentos automaticamente - só quando o usuário selecionar
                if (StatusTextBlock != null)
                {
                    StatusTextBlock.Text = "Selecione uma categoria para ver os procedimentos";
                }
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
            
            // Pré-selecionar "Descrição" no FiltrosComboBox por padrão
            if (FiltrosComboBox != null)
            {
                // Encontrar o item "Descrição" e selecioná-lo
                foreach (ComboBoxItem item in FiltrosComboBox.Items)
                {
                    if (item.Content?.ToString() == "Descrição")
                    {
                        FiltrosComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Erro ao carregar dados:\n\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nErro interno: {ex.InnerException.Message}";
            }
            errorMessage += $"\n\nStack Trace:\n{ex.StackTrace}";
            
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
    
    /// <summary>
    /// Define o status de carregamento e atualiza a UI
    /// </summary>
    private void SetLoadingStatus(string message, bool isLoading)
    {
        _isLoading = isLoading;
        
        Dispatcher.Invoke(() =>
        {
            // Barra de progresso sempre visível, mas mostra conteúdo apenas quando carregando
            if (LoadingBarGrid != null)
            {
                // Sempre visível, não colapsa
                LoadingBarGrid.Visibility = Visibility.Visible;
            }
            
            if (LoadingProgressBar != null)
            {
                LoadingProgressBar.IsIndeterminate = isLoading;
                LoadingProgressBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (LoadingStatusTextBlock != null)
            {
                LoadingStatusTextBlock.Text = isLoading ? message : string.Empty;
            }
            
            // Desabilitar/habilitar botão "Buscar Relacionados"
            if (BtnBuscarRelacionados != null)
            {
                BtnBuscarRelacionados.IsEnabled = !isLoading;
            }
        });
    }
    
    /// <summary>
    /// Gera chave de cache para resultados relacionados
    /// </summary>
    private string GerarChaveCache(string coProcedimento, string competencia, string tipoFiltro)
    {
        return $"{coProcedimento}|{competencia}|{tipoFiltro}";
    }
    
    /// <summary>
    /// Invalida o cache de resultados relacionados
    /// </summary>
    private void InvalidarCacheRelacionados()
    {
        _cacheRelacionados.Clear();
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

            // Buscar todos os procedimentos comuns e atualizar cache usando a fila para evitar chamadas paralelas
            var procedimentosComuns = await DatabaseRequestQueue.Instance.EnqueueAsync(
                async () => await _procedimentoComumService.BuscarTodosAsync());
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
                // Buscar todos os procedimentos de uma vez usando a fila para evitar chamadas paralelas
                var procedimentosEncontrados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _procedimentoService.BuscarPorCodigosAsync(
                        codigos, 
                        _competenciaAtiva));

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
        var selectedItem = CategoriasTreeView.SelectedItem;
        
        if (selectedItem == null)
        {
            return;
        }

        // Expandir apenas o caminho até o item selecionado (sem recolher outros)
        await Dispatcher.InvokeAsync(() =>
        {
            ExpandirCaminhoAteItem(selectedItem);
        });

        // Aguardar um pouco para o TreeView processar a expansão
        await Task.Delay(100);

        // Ao selecionar um item no TreeView, carregar apenas os procedimentos relacionados
        await CarregarProcedimentosSelecionadosAsync();
    }

    private void ExpandirCaminhoAteItem(object item)
    {
        if (item == null)
            return;

        // Determinar o tipo de item e expandir o caminho apropriado
        if (item is FormaOrganizacao formaOrganizacao)
        {
            // Expandir grupo, depois subgrupo, depois forma de organização
            var grupo = _grupos.FirstOrDefault(g => g.CoGrupo == formaOrganizacao.CoGrupo);
            if (grupo != null)
            {
                var grupoTreeViewItem = EncontrarTreeViewItemPorItem(CategoriasTreeView, grupo);
                if (grupoTreeViewItem != null)
                {
                    grupoTreeViewItem.IsExpanded = true;
                    grupoTreeViewItem.UpdateLayout();

                    var subGrupo = grupo.SubGrupos.FirstOrDefault(sg => sg.CoSubGrupo == formaOrganizacao.CoSubGrupo);
                    if (subGrupo != null)
                    {
                        var subGrupoTreeViewItem = EncontrarTreeViewItemPorItem(CategoriasTreeView, subGrupo);
                        if (subGrupoTreeViewItem != null)
                        {
                            subGrupoTreeViewItem.IsExpanded = true;
                            subGrupoTreeViewItem.UpdateLayout();
                        }
                    }
                }
            }
        }
        else if (item is SubGrupo subGrupo)
        {
            // Expandir apenas o grupo e depois o subgrupo
            var grupo = _grupos.FirstOrDefault(g => g.CoGrupo == subGrupo.CoGrupo);
            if (grupo != null)
            {
                var grupoTreeViewItem = EncontrarTreeViewItemPorItem(CategoriasTreeView, grupo);
                if (grupoTreeViewItem != null)
                {
                    grupoTreeViewItem.IsExpanded = true;
                    grupoTreeViewItem.UpdateLayout();
                }
            }
        }
        // Se for um Grupo, não precisa expandir nada, pois já está no nível raiz

        // Encontrar e trazer o item selecionado para a visualização
        var treeViewItem = EncontrarTreeViewItemPorItem(CategoriasTreeView, item);
        if (treeViewItem != null)
        {
            treeViewItem.IsExpanded = true;
            treeViewItem.UpdateLayout();
            treeViewItem.BringIntoView();
        }
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
            // Tipo não reconhecido
            StatusTextBlock.Text = $"Tipo de item não reconhecido: {dataContext.GetType().Name}";
            return;
        }

        // Se nenhum grupo foi identificado, não carrega
        if (string.IsNullOrEmpty(coGrupo))
        {
            StatusTextBlock.Text = "Grupo não identificado";
            return;
        }

        try
        {
            // Busca apenas os procedimentos da estrutura selecionada usando a fila para evitar chamadas paralelas
            StatusTextBlock.Text = $"Carregando procedimentos de: {itemNome}...";
            
            // Usar DatabaseRequestQueue para garantir que não há chamadas paralelas ao banco
            var procedimentos = await DatabaseRequestQueue.Instance.EnqueueAsync(
                async () => await _procedimentoService.BuscarPorEstruturaAsync(
                    coGrupo, 
                    coSubGrupo, 
                    coFormaOrganizacao, 
                    _competenciaAtiva));
            
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
            var errorMsg = ex.Message;
            if (ex.InnerException != null && ex.InnerException.Message.Contains("invalid transaction handle", StringComparison.OrdinalIgnoreCase))
            {
                errorMsg = "Erro de transação no banco de dados. Por favor, tente novamente.";
            }
            
            MessageBox.Show(errorMsg, 
                          "Erro de Banco de Dados", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            StatusTextBlock.Text = "Erro ao carregar procedimentos";
        }
        catch (Exception ex) when (ex.Message.Contains("invalid transaction handle", StringComparison.OrdinalIgnoreCase) ||
                                   (ex.InnerException?.Message?.Contains("invalid transaction handle", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            // Erro específico de transação do Firebird
            MessageBox.Show("Erro de transação no banco de dados. Por favor, aguarde um momento e tente novamente.", 
                          "Erro de Banco de Dados", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            StatusTextBlock.Text = "Erro ao carregar procedimentos - tente novamente";
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
            
            // Não expandir automaticamente - apenas quando usuário clicar no TreeView
            // Isso evita múltiplas chamadas ao banco de dados que não aceita buscas paralelas
            
            // Debounce: cancelar requisição anterior e aguardar 400ms
            _currentCancellationTokenSource?.Cancel();
            _currentCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _currentCancellationTokenSource.Token;
            
            // Cancelar timer anterior se existir
            _debounceTimer?.Stop();
            _debounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            
            _debounceTimer.Tick += async (s, args) =>
            {
                _debounceTimer.Stop();
                if (!cancellationToken.IsCancellationRequested)
                {
                    await AtualizarAreaRelacionadosAsync(cancellationToken);
                }
            };
            
            _debounceTimer.Start();
        }
        else
        {
            SubmenuHeaderTextBlock.Text = "Nenhum procedimento selecionado";
        }
    }

    private void CompetenciaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CompetenciaComboBox.SelectedItem is string competencia)
        {
            // Se a competência selecionada for diferente da ativa, avisa o usuário
            // Mas não impede de ver, apenas de editar (se houvesse edição)
            // O botão de ativar competência é que muda a _competenciaAtiva globalmente para operações de escrita
            // Para leitura, podemos usar a selecionada no combo se quisermos visualizar dados de outra competência?
            // O requisito diz "ATIVAR COMPETÊNCIA" botão e "Competência:" combo.
            // O comportamento atual do sistema parece ser que o combo apenas seleciona para ativação.
            // Mas se quisermos que o grid atualize conforme a competência selecionada no combo (preview),
            // deveríamos atualizar _competenciaAtiva ou passar a competência do combo para as buscas.
            
            // Por segurança e seguindo o padrão atual, vamos manter _competenciaAtiva como a fonte da verdade
            // até que o usuário clique em "Ativar". 
            // PORÉM, o usuário pediu "embaixo de Competência tem um espaço...".
            // Se ele mudar o combo, ele espera ver os dados daquela competência?
            // Geralmente sim. Mas o botão "Ativar" sugere que a mudança não é imediata para todo o sistema.
            // Vamos assumir que a visualização segue a competência ATIVA (que é exibida onde?).
            // O combo serve para selecionar uma NOVA competência para ativar.
            
            // Se o usuário quer ver os dados da competência selecionada no combo ANTES de ativar,
            // precisaríamos passar essa competência para o AtualizarAreaRelacionados.
            // Mas o AtualizarAreaRelacionados usa _competenciaAtiva.
            
            // Vamos manter simples: Atualiza a área se a competência mudar (se o usuário ativar).
            // Se o combo for apenas para seleção pré-ativação, não faz nada aqui.
            // Mas se o combo JÁ reflete a competência ativa, então ok.
            
            // Verificando o código de AtivarCompetencia_Click (não visível aqui, mas inferido):
            // Ele deve pegar o item do combo e setar _competenciaAtiva.
            
            // Se o usuário mudar o combo, ele ainda não ativou.
            // Então não devemos atualizar a visualização com dados misturados (procedimentos de uma competência, relacionados de outra).
            // O ideal é só atualizar quando confirmar.
            
            // Mas espere, o DataGrid de procedimentos é carregado com base em qual competência?
            // Provavelmente _competenciaAtiva.
            // Então os relacionados também devem ser.
            
            // Conclusão: Não alterar CompetenciaComboBox_SelectionChanged para atualizar a view, 
            // pois a view depende da competência ATIVA, que só muda no botão Confirmar/Ativar.
            
            // Não atualizar área relacionada aqui, pois a competência ainda não foi ativada
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


    private TreeViewItem? EncontrarTreeViewItemPorItem(ItemsControl itemsControl, object item)
    {
        if (itemsControl == null || item == null)
            return null;

        // Verificar se o item está diretamente nos itens do controle
        foreach (var container in itemsControl.Items)
        {
            if (container == item)
            {
                var foundTreeViewItem = itemsControl.ItemContainerGenerator.ContainerFromItem(container) as TreeViewItem;
                return foundTreeViewItem;
            }

            var treeViewItem = itemsControl.ItemContainerGenerator.ContainerFromItem(container) as TreeViewItem;
            if (treeViewItem != null)
            {
                var dataContext = treeViewItem.DataContext ?? treeViewItem.Header;
                if (dataContext == item || container == item)
                {
                    return treeViewItem;
                }

                // Buscar recursivamente nos filhos
                var found = EncontrarTreeViewItemRecursivo(treeViewItem, item);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private TreeViewItem? EncontrarTreeViewItemRecursivo(TreeViewItem parentItem, object item)
    {
        if (parentItem == null || item == null)
            return null;

        foreach (var child in parentItem.Items)
        {
            if (child == item)
            {
                var foundTreeViewItem = parentItem.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                return foundTreeViewItem;
            }

            var treeViewItem = parentItem.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
            if (treeViewItem != null)
            {
                var dataContext = treeViewItem.DataContext ?? treeViewItem.Header;
                if (dataContext == item || child == item)
                {
                    return treeViewItem;
                }

                // Buscar recursivamente nos filhos
                var found = EncontrarTreeViewItemRecursivo(treeViewItem, item);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
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
                    
                    // Invalidar cache quando competência muda
                    InvalidarCacheRelacionados();
                    
                    MessageBox.Show($"Competência {FormatCompetencia(competencia)} ativada com sucesso!", 
                                  "Sucesso", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    
                    // Recarregar apenas grupos com a nova competência (não carrega procedimentos ainda)
                    await CarregarGruposAsync();
                    
                    // Mostrar barra de carregamento enquanto carrega procedimentos comuns
                    SetLoadingStatus("Carregando procedimentos comuns...", true);
                    
                    try
                    {
                        // Ir automaticamente para a tela inicial (procedimentos comuns)
                        await CarregarProcedimentosComunsAsync();
                    }
                    finally
                    {
                        // Esconder barra de carregamento após concluir (mesmo em caso de erro)
                        SetLoadingStatus("Pronto", false);
                    }
                }
            }
            catch (Exception ex)
            {
                SetLoadingStatus("Erro", false);
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

    private async void CadastrarServico_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(_competenciaAtiva))
            {
                MessageBox.Show("Por favor, selecione e ative uma competência antes de cadastrar serviço/classificação.", 
                              "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await AbrirJanelaCadastroServicoClassificacaoAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir janela de cadastro:\n{ex.Message}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AbrirJanelaCadastroServicoClassificacaoAsync()
    {
        var dialog = new Window
        {
            Title = "Cadastro de Serviço/Classificação do estabelecimento.",
            Width = 1000,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // DataGrid com os registros existentes
        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = false, // Permite edição para novas linhas
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            SelectionMode = DataGridSelectionMode.Single,
            Margin = new Thickness(5),
            CanUserAddRows = false // Controlamos manualmente a adição de linhas
        };

        // Coluna Id (sequencial para exibição)
        var idColumn = new DataGridTextColumn
        {
            Header = "Id",
            Binding = new Binding("Id"),
            Width = 60,
            CanUserSort = true,
            IsReadOnly = true
        };
        dataGrid.Columns.Add(idColumn);

        // Coluna Serv. (Código do Serviço) - editável apenas em novas linhas
        var servicoColumn = new DataGridTextColumn
        {
            Header = "Serv.",
            Binding = new Binding("CoServico"),
            Width = 80,
            CanUserSort = true
        };
        dataGrid.Columns.Add(servicoColumn);

        // Coluna Descrição serviço
        var descServicoColumn = new DataGridTextColumn
        {
            Header = "Descrição serviço",
            Binding = new Binding("DescricaoServico"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 300,
            IsReadOnly = true
        };
        descServicoColumn.ElementStyle = new Style(typeof(TextBlock));
        descServicoColumn.ElementStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
        descServicoColumn.ElementStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2)));
        dataGrid.Columns.Add(descServicoColumn);

        // Coluna Class. (Código da Classificação) - editável apenas em novas linhas
        var classificacaoColumn = new DataGridTextColumn
        {
            Header = "Class.",
            Binding = new Binding("CoClassificacao"),
            Width = 80,
            CanUserSort = true
        };
        dataGrid.Columns.Add(classificacaoColumn);

        // Coluna Descrição Classificação
        var descClassColumn = new DataGridTextColumn
        {
            Header = "Descrição Classificação",
            Binding = new Binding("DescricaoClassificacao"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 250,
            IsReadOnly = true
        };
        descClassColumn.ElementStyle = new Style(typeof(TextBlock));
        descClassColumn.ElementStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
        descClassColumn.ElementStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2)));
        dataGrid.Columns.Add(descClassColumn);

        // Configurar altura automática das linhas
        dataGrid.LoadingRow += (s, e) =>
        {
            e.Row.Height = double.NaN; // Auto height
        };

        Grid.SetRow(dataGrid, 0);
        mainGrid.Children.Add(dataGrid);

        // Campos de entrada: Serviço e Classificação
        var inputGrid = new Grid();
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        inputGrid.Margin = new Thickness(5);

        var txtServico = new TextBox
        {
            MaxLength = 3,
            Margin = new Thickness(5, 0, 5, 0)
        };
        var txtClassificacao = new TextBox
        {
            MaxLength = 3,
            Margin = new Thickness(5, 0, 5, 0)
        };

        var lblServico = new Label { Content = "Serviço:", Margin = new Thickness(5, 5, 2, 5) };
        var lblClassificacao = new Label { Content = "Classificação:", Margin = new Thickness(15, 5, 2, 5) };

        Grid.SetColumn(lblServico, 0);
        Grid.SetColumn(txtServico, 1);
        Grid.SetColumn(lblClassificacao, 2);
        Grid.SetColumn(txtClassificacao, 3);

        inputGrid.Children.Add(lblServico);
        inputGrid.Children.Add(txtServico);
        inputGrid.Children.Add(lblClassificacao);
        inputGrid.Children.Add(txtClassificacao);

        Grid.SetRow(inputGrid, 1);
        mainGrid.Children.Add(inputGrid);

        // Botões: Novo, Salvar, Cancelar, Excluir
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(5)
        };

        var btnNovo = new Button
        {
            Content = "Novo",
            Margin = new Thickness(5, 0, 5, 0),
            Padding = new Thickness(10, 5, 10, 5),
            Width = 100
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

        var btnExcluir = new Button
        {
            Content = "Excluir",
            Margin = new Thickness(5, 0, 5, 0),
            Padding = new Thickness(10, 5, 10, 5),
            Width = 100
        };

        buttonPanel.Children.Add(btnNovo);
        buttonPanel.Children.Add(btnSalvar);
        buttonPanel.Children.Add(btnCancelar);
        buttonPanel.Children.Add(btnExcluir);

        Grid.SetRow(buttonPanel, 2);
        mainGrid.Children.Add(buttonPanel);

        // Mensagem de status no rodapé
        var statusText = new TextBlock
        {
            Text = "Informe o serviço e a classificação com 3 digitos",
            Margin = new Thickness(5),
            FontStyle = FontStyles.Italic,
            Foreground = Brushes.Gray
        };
        Grid.SetRow(statusText, 3);
        mainGrid.Children.Add(statusText);

        dialog.Content = mainGrid;

        // Classe auxiliar para exibição no DataGrid
        var itensExibicao = new ObservableCollection<ServicoClassificacaoItem>();
        ServicoClassificacaoItem? itemSelecionado = null;
        ServicoClassificacaoItem? linhaNova = null;

        // Função para validar e buscar nome do serviço
        Func<string, Task<string?>> validarServico = async (codigo) =>
        {
            try
            {
                codigo = codigo.Trim().PadLeft(3, '0');
                if (codigo.Length != 3) return null;

                // Buscar classificações do serviço para verificar se existe
                var classificacoes = await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _servicoClassificacaoService.BuscarPorServicoAsync(codigo, _competenciaAtiva));

                var primeira = classificacoes.FirstOrDefault();
                return primeira?.Servico?.NoServico;
            }
            catch
            {
                return null;
            }
        };

        // Função para validar e buscar nome da classificação
        Func<string, string, Task<string?>> validarClassificacao = async (coServico, coClassificacao) =>
        {
            try
            {
                coServico = coServico.Trim().PadLeft(3, '0');
                coClassificacao = coClassificacao.Trim().PadLeft(3, '0');
                
                if (coServico.Length != 3 || coClassificacao.Length != 3) return null;

                var item = await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _servicoClassificacaoService.BuscarPorCodigosAsync(
                        coServico, coClassificacao, _competenciaAtiva));

                return item?.NoClassificacao;
            }
            catch
            {
                return null;
            }
        };

        // Função para carregar dados
        Func<Task> carregarDados = async () =>
        {
            try
            {
                var classificacoes = await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _servicoClassificacaoService.BuscarTodosAsync(_competenciaAtiva));

                itensExibicao.Clear();
                int id = 1;
                foreach (var item in classificacoes.OrderByDescending(x => x.CoServico).ThenByDescending(x => x.CoClassificacao))
                {
                    itensExibicao.Add(new ServicoClassificacaoItem
                    {
                        Id = id++,
                        CoServico = item.CoServico,
                        DescricaoServico = item.Servico?.NoServico ?? string.Empty,
                        CoClassificacao = item.CoClassificacao,
                        DescricaoClassificacao = item.NoClassificacao ?? string.Empty,
                        ServicoClassificacao = item,
                        IsNovo = false
                    });
                }

                dataGrid.ItemsSource = itensExibicao;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        // Handler para quando seleciona um item no DataGrid
        dataGrid.SelectionChanged += async (s, e) =>
        {
            // Se havia uma linha nova antes da mudança de seleção, validar antes de trocar
            if (linhaNova != null && linhaNova != dataGrid.SelectedItem && itensExibicao.Contains(linhaNova))
            {
                // Validar serviço se preenchido mas não validado
                var codigoServico = linhaNova.CoServico?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(codigoServico) && string.IsNullOrEmpty(linhaNova.DescricaoServico))
                {
                    var nomeServico = await validarServico(codigoServico);
                    if (nomeServico != null)
                    {
                        linhaNova.DescricaoServico = nomeServico;
                        linhaNova.CoServico = codigoServico.PadLeft(3, '0');
                        txtServico.Text = linhaNova.CoServico;
                        // Atualizar DataGrid para mostrar a descrição
                        dataGrid.Items.Refresh();
                    }
                    else
                    {
                        MessageBox.Show($"Serviço '{codigoServico}' inválido ou não encontrado!", 
                                      "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Validar classificação se preenchida mas não validada
                var codigoClass = linhaNova.CoClassificacao?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(codigoServico) && !string.IsNullOrEmpty(codigoClass) && 
                    string.IsNullOrEmpty(linhaNova.DescricaoClassificacao))
                {
                    var nomeClass = await validarClassificacao(codigoServico, codigoClass);
                    if (nomeClass != null)
                    {
                        linhaNova.DescricaoClassificacao = nomeClass;
                        linhaNova.CoClassificacao = codigoClass.PadLeft(3, '0');
                        txtClassificacao.Text = linhaNova.CoClassificacao;
                        // Atualizar DataGrid para mostrar a descrição
                        dataGrid.Items.Refresh();
                    }
                    else
                    {
                        MessageBox.Show($"Classificação '{codigoClass}' inválida ou não encontrada para o serviço '{codigoServico}'!", 
                                      "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            if (dataGrid.SelectedItem is ServicoClassificacaoItem item)
            {
                if (item.IsNovo)
                {
                    // Linha nova - sincronizar com campos do rodapé
                    itemSelecionado = null;
                    linhaNova = item;
                    txtServico.Text = item.CoServico ?? string.Empty;
                    txtClassificacao.Text = item.CoClassificacao ?? string.Empty;
                    txtServico.IsReadOnly = false;
                    txtClassificacao.IsReadOnly = false;
                    btnSalvar.IsEnabled = true;
                    btnExcluir.IsEnabled = false;
                }
                else
                {
                    // Item existente selecionado
                    itemSelecionado = item;
                    linhaNova = null;
                    txtServico.Text = item.CoServico;
                    txtClassificacao.Text = item.CoClassificacao;
                    txtServico.IsReadOnly = true;
                    txtClassificacao.IsReadOnly = true;
                    btnSalvar.IsEnabled = false;
                    btnExcluir.IsEnabled = true;
                }
            }
            else
            {
                itemSelecionado = null;
                // Não limpar linhaNova aqui, ela pode estar sendo editada
                txtServico.IsReadOnly = false;
                txtClassificacao.IsReadOnly = false;
                btnSalvar.IsEnabled = true;
                btnExcluir.IsEnabled = true;
            }
        };

        // Carregar dados iniciais
        await carregarDados();

        // Event handlers para sincronizar campos do rodapé com linha nova
        txtServico.TextChanged += (s, e) =>
        {
            if (linhaNova != null && dataGrid.SelectedItem == linhaNova)
            {
                linhaNova.CoServico = txtServico.Text ?? string.Empty;
            }
        };

        txtClassificacao.TextChanged += (s, e) =>
        {
            if (linhaNova != null && dataGrid.SelectedItem == linhaNova)
            {
                linhaNova.CoClassificacao = txtClassificacao.Text ?? string.Empty;
            }
        };

        // Event handler para validar serviço quando o usuário sair do campo
        txtServico.LostFocus += async (s, e) =>
        {
            if (linhaNova == null) return;

            var codigo = txtServico.Text?.Trim() ?? string.Empty;
            linhaNova.CoServico = codigo;

            if (!string.IsNullOrEmpty(codigo) && codigo.Length <= 3)
            {
                var nomeServico = await validarServico(codigo);
                if (nomeServico != null)
                {
                    linhaNova.DescricaoServico = nomeServico;
                    linhaNova.CoServico = codigo.PadLeft(3, '0');
                    txtServico.Text = linhaNova.CoServico;
                    statusText.Text = $"Serviço válido: {nomeServico}";
                    statusText.Foreground = Brushes.Green;
                    // Atualizar DataGrid para mostrar a descrição
                    dataGrid.Items.Refresh();
                }
                else
                {
                    linhaNova.DescricaoServico = string.Empty;
                    MessageBox.Show($"Serviço '{codigo}' inválido ou não encontrado!", 
                                  "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    statusText.Text = "Serviço inválido";
                    statusText.Foreground = Brushes.Red;
                    // Atualizar DataGrid para limpar a descrição
                    dataGrid.Items.Refresh();
                }
            }
        };

        // Event handler para validar classificação quando o usuário sair do campo
        txtClassificacao.LostFocus += async (s, e) =>
        {
            if (linhaNova == null) return;

            var codigoClass = txtClassificacao.Text?.Trim() ?? string.Empty;
            linhaNova.CoClassificacao = codigoClass;
            var codigoServico = linhaNova.CoServico?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(codigoServico) && !string.IsNullOrEmpty(codigoClass) && 
                codigoServico.Length <= 3 && codigoClass.Length <= 3)
            {
                var nomeClass = await validarClassificacao(codigoServico, codigoClass);
                if (nomeClass != null)
                {
                    linhaNova.DescricaoClassificacao = nomeClass;
                    linhaNova.CoClassificacao = codigoClass.PadLeft(3, '0');
                    txtClassificacao.Text = linhaNova.CoClassificacao;
                    statusText.Text = $"Classificação válida: {nomeClass}";
                    statusText.Foreground = Brushes.Green;
                    // Atualizar DataGrid para mostrar a descrição
                    dataGrid.Items.Refresh();
                }
                else
                {
                    linhaNova.DescricaoClassificacao = string.Empty;
                    MessageBox.Show($"Classificação '{codigoClass}' inválida ou não encontrada para o serviço '{codigoServico}'!", 
                                  "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    statusText.Text = "Classificação inválida";
                    statusText.Foreground = Brushes.Red;
                    // Atualizar DataGrid para limpar a descrição
                    dataGrid.Items.Refresh();
                }
            }
        };

        // Função auxiliar para tentar salvar linha nova (retorna true se salvou com sucesso ou não havia nada para salvar)
        Func<Task<bool>> tentarSalvarLinhaNova = async () =>
        {
            if (linhaNova == null || !itensExibicao.Contains(linhaNova)) return true;

            var servico = linhaNova.CoServico?.Trim() ?? string.Empty;
            var classificacao = linhaNova.CoClassificacao?.Trim() ?? string.Empty;

            // Se não há dados para salvar, retorna true
            if (string.IsNullOrEmpty(servico) && string.IsNullOrEmpty(classificacao)) return true;

            // Validar campos
            if (string.IsNullOrEmpty(servico) || string.IsNullOrEmpty(classificacao) ||
                string.IsNullOrEmpty(linhaNova.DescricaoServico) || string.IsNullOrEmpty(linhaNova.DescricaoClassificacao))
            {
                return false; // Não pode salvar, campos incompletos
            }

            try
            {
                servico = servico.PadLeft(3, '0');
                classificacao = classificacao.PadLeft(3, '0');

                var existe = await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _servicoClassificacaoService.ExisteAsync(servico, classificacao, _competenciaAtiva));
                
                if (existe) return false; // Já existe

                var novoItem = new ServicoClassificacao
                {
                    CoServico = servico,
                    CoClassificacao = classificacao,
                    DtCompetencia = _competenciaAtiva,
                    NoClassificacao = linhaNova.DescricaoClassificacao
                };

                await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _servicoClassificacaoService.AdicionarAsync(novoItem));

                itensExibicao.Remove(linhaNova);
                linhaNova = null;
                await carregarDados();
                return true;
            }
            catch
            {
                return false;
            }
        };

        // Event handler para prevenir fechamento da janela sem salvar linha nova
        dialog.Closing += async (s, e) =>
        {
            if (linhaNova != null && itensExibicao.Contains(linhaNova))
            {
                var servico = linhaNova.CoServico?.Trim() ?? string.Empty;
                var classificacao = linhaNova.CoClassificacao?.Trim() ?? string.Empty;

                // Verificar se há dados para salvar
                if (!string.IsNullOrEmpty(servico) || !string.IsNullOrEmpty(classificacao))
                {
                    var resultado = MessageBox.Show(
                        "Há uma linha nova não salva. Deseja salvar antes de fechar?",
                        "Confirmar",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        e.Cancel = true; // Cancela o fechamento temporariamente
                        
                        // Validar e focar em campos não preenchidos
                        if (string.IsNullOrEmpty(servico))
                        {
                            dataGrid.SelectedItem = linhaNova;
                            dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, servicoColumn);
                            dataGrid.BeginEdit();
                            txtServico.Focus();
                            MessageBox.Show("Por favor, informe o serviço antes de salvar.", 
                                          "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // Mantém a janela aberta
                        }

                        if (string.IsNullOrEmpty(classificacao))
                        {
                            dataGrid.SelectedItem = linhaNova;
                            dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, classificacaoColumn);
                            dataGrid.BeginEdit();
                            txtClassificacao.Focus();
                            MessageBox.Show("Por favor, informe a classificação antes de salvar.", 
                                          "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // Mantém a janela aberta
                        }

                        if (string.IsNullOrEmpty(linhaNova.DescricaoServico))
                        {
                            dataGrid.SelectedItem = linhaNova;
                            dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, servicoColumn);
                            dataGrid.BeginEdit();
                            txtServico.Focus();
                            MessageBox.Show("Por favor, valide o serviço (deve ter descrição preenchida) antes de salvar.", 
                                          "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // Mantém a janela aberta
                        }

                        if (string.IsNullOrEmpty(linhaNova.DescricaoClassificacao))
                        {
                            dataGrid.SelectedItem = linhaNova;
                            dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, classificacaoColumn);
                            dataGrid.BeginEdit();
                            txtClassificacao.Focus();
                            MessageBox.Show("Por favor, valide a classificação (deve ter descrição preenchida) antes de salvar.", 
                                          "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return; // Mantém a janela aberta
                        }

                        // Tentar salvar
                        var salvou = await tentarSalvarLinhaNova();
                        if (salvou)
                        {
                            // Permite fechar
                            e.Cancel = false;
                            dialog.DialogResult = true;
                        }
                        else
                        {
                            MessageBox.Show("Não foi possível salvar. Verifique se todos os campos estão preenchidos e válidos.", 
                                          "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                            // Mantém a janela aberta
                            e.Cancel = true;
                        }
                    }
                    else if (resultado == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true; // Cancela o fechamento
                    }
                    // Se No, permite fechar normalmente (e.Cancel já é false)
                }
            }
        };

        // Event handler para prevenir edição de linhas existentes
        dataGrid.BeginningEdit += (s, e) =>
        {
            if (e.Row.Item is ServicoClassificacaoItem item && !item.IsNovo)
            {
                // Cancelar edição de linhas existentes nas colunas de código
                if (e.Column == servicoColumn || e.Column == classificacaoColumn)
                {
                    e.Cancel = true;
                    MessageBox.Show("Para modificar um registro existente, exclua-o e crie um novo.", 
                                  "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        };

        // Event handler para quando o usuário edita uma célula no DataGrid
        dataGrid.CellEditEnding += async (s, e) =>
        {
            if (e.EditAction == DataGridEditAction.Cancel) return;

            var item = e.Row.Item as ServicoClassificacaoItem;
            if (item == null || !item.IsNovo) return;

            if (e.Column == servicoColumn)
            {
                // Validar serviço
                var codigo = item.CoServico?.Trim() ?? string.Empty;
                // Sincronizar com campo do rodapé
                if (linhaNova == item)
                {
                    txtServico.Text = codigo;
                }
                
                if (codigo.Length > 0 && codigo.Length <= 3)
                {
                    var nomeServico = await validarServico(codigo);
                    if (nomeServico != null)
                    {
                        item.DescricaoServico = nomeServico;
                        item.CoServico = codigo.PadLeft(3, '0');
                        if (linhaNova == item)
                        {
                            txtServico.Text = item.CoServico;
                        }
                        statusText.Text = $"Serviço válido: {nomeServico}";
                        statusText.Foreground = Brushes.Green;
                        // Atualizar DataGrid para mostrar a descrição
                        dataGrid.Items.Refresh();
                    }
                    else
                    {
                        item.DescricaoServico = string.Empty;
                        MessageBox.Show($"Serviço '{codigo}' inválido ou não encontrado!", 
                                      "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        statusText.Text = "Serviço inválido";
                        statusText.Foreground = Brushes.Red;
                        // Atualizar DataGrid para limpar a descrição
                        dataGrid.Items.Refresh();
                    }
                }
            }
            else if (e.Column == classificacaoColumn)
            {
                // Validar classificação (precisa do serviço também)
                var codigoServico = item.CoServico?.Trim() ?? string.Empty;
                var codigoClass = item.CoClassificacao?.Trim() ?? string.Empty;
                // Sincronizar com campo do rodapé
                if (linhaNova == item)
                {
                    txtClassificacao.Text = codigoClass;
                }
                
                if (codigoServico.Length > 0 && codigoClass.Length > 0 && 
                    codigoServico.Length <= 3 && codigoClass.Length <= 3)
                {
                    var nomeClass = await validarClassificacao(codigoServico, codigoClass);
                    if (nomeClass != null)
                    {
                        item.DescricaoClassificacao = nomeClass;
                        item.CoClassificacao = codigoClass.PadLeft(3, '0');
                        if (linhaNova == item)
                        {
                            txtClassificacao.Text = item.CoClassificacao;
                        }
                        statusText.Text = $"Classificação válida: {nomeClass}";
                        statusText.Foreground = Brushes.Green;
                        // Atualizar DataGrid para mostrar a descrição
                        dataGrid.Items.Refresh();
                    }
                    else
                    {
                        item.DescricaoClassificacao = string.Empty;
                        MessageBox.Show($"Classificação '{codigoClass}' inválida ou não encontrada para o serviço '{codigoServico}'!", 
                                      "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        statusText.Text = "Classificação inválida";
                        statusText.Foreground = Brushes.Red;
                        // Atualizar DataGrid para limpar a descrição
                        dataGrid.Items.Refresh();
                    }
                }
            }
        };

        // Validar quando o usuário clicar fora da célula ou em outra coisa
        dataGrid.LostFocus += async (s, e) =>
        {
            if (linhaNova != null && itensExibicao.Contains(linhaNova))
            {
                // Validar serviço se preenchido
                var codigoServico = linhaNova.CoServico?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(codigoServico) && string.IsNullOrEmpty(linhaNova.DescricaoServico))
                {
                    var nomeServico = await validarServico(codigoServico);
                    if (nomeServico != null)
                    {
                        linhaNova.DescricaoServico = nomeServico;
                        linhaNova.CoServico = codigoServico.PadLeft(3, '0');
                        txtServico.Text = linhaNova.CoServico;
                        // Atualizar DataGrid para mostrar a descrição
                        dataGrid.Items.Refresh();
                    }
                }

                // Validar classificação se preenchida
                var codigoClass = linhaNova.CoClassificacao?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(codigoServico) && !string.IsNullOrEmpty(codigoClass) && 
                    string.IsNullOrEmpty(linhaNova.DescricaoClassificacao))
                {
                    var nomeClass = await validarClassificacao(codigoServico, codigoClass);
                    if (nomeClass != null)
                    {
                        linhaNova.DescricaoClassificacao = nomeClass;
                        linhaNova.CoClassificacao = codigoClass.PadLeft(3, '0');
                        txtClassificacao.Text = linhaNova.CoClassificacao;
                        // Atualizar DataGrid para mostrar a descrição
                        dataGrid.Items.Refresh();
                    }
                }
            }
        };

        // Event handlers
        btnNovo.Click += (s, e) =>
        {
            // Remover linha nova anterior se existir
            if (linhaNova != null && itensExibicao.Contains(linhaNova))
            {
                itensExibicao.Remove(linhaNova);
            }

            // Criar nova linha vazia
            var maxId = itensExibicao.Any() ? itensExibicao.Max(x => x.Id) : 0;
            linhaNova = new ServicoClassificacaoItem
            {
                Id = maxId + 1,
                CoServico = string.Empty,
                DescricaoServico = string.Empty,
                CoClassificacao = string.Empty,
                DescricaoClassificacao = string.Empty,
                IsNovo = true
            };

            itensExibicao.Insert(0, linhaNova);
            
            // Selecionar a nova linha e permitir edição
            dataGrid.SelectedIndex = 0;
            dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, servicoColumn);
            dataGrid.BeginEdit();

            txtServico.Text = string.Empty;
            txtClassificacao.Text = string.Empty;
            itemSelecionado = null;
            btnSalvar.IsEnabled = true;
            btnExcluir.IsEnabled = false;
            statusText.Text = "Digite o código do serviço (3 dígitos)";
            statusText.Foreground = Brushes.Gray;
        };

        btnSalvar.Click += async (s, e) =>
        {
            // Verificar se há uma linha nova no DataGrid
            if (linhaNova != null && itensExibicao.Contains(linhaNova))
            {
                var servico = linhaNova.CoServico?.Trim() ?? string.Empty;
                var classificacao = linhaNova.CoClassificacao?.Trim() ?? string.Empty;

                // Validar que ambos têm conteúdo e focar na célula não preenchida
                if (string.IsNullOrEmpty(servico))
                {
                    MessageBox.Show("Por favor, informe o serviço.", 
                                  "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Focar na célula de serviço
                    dataGrid.SelectedItem = linhaNova;
                    dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, servicoColumn);
                    dataGrid.BeginEdit();
                    txtServico.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(classificacao))
                {
                    MessageBox.Show("Por favor, informe a classificação.", 
                                  "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Focar na célula de classificação
                    dataGrid.SelectedItem = linhaNova;
                    dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, classificacaoColumn);
                    dataGrid.BeginEdit();
                    txtClassificacao.Focus();
                    return;
                }

                // Validar que ambos foram validados (têm descrições)
                if (string.IsNullOrEmpty(linhaNova.DescricaoServico))
                {
                    MessageBox.Show("Por favor, valide o serviço (deve ter descrição preenchida).", 
                                  "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Focar na célula de serviço
                    dataGrid.SelectedItem = linhaNova;
                    dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, servicoColumn);
                    dataGrid.BeginEdit();
                    txtServico.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(linhaNova.DescricaoClassificacao))
                {
                    MessageBox.Show("Por favor, valide a classificação (deve ter descrição preenchida).", 
                                  "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Focar na célula de classificação
                    dataGrid.SelectedItem = linhaNova;
                    dataGrid.CurrentCell = new DataGridCellInfo(linhaNova, classificacaoColumn);
                    dataGrid.BeginEdit();
                    txtClassificacao.Focus();
                    return;
                }

                // Usar função auxiliar para salvar
                var salvou = await tentarSalvarLinhaNova();
                if (salvou)
                {
                    dataGrid.SelectedIndex = -1;
                    itemSelecionado = null;
                    txtServico.Text = string.Empty;
                    txtClassificacao.Text = string.Empty;
                    statusText.Text = "Informe o serviço e a classificação com 3 digitos";
                    statusText.Foreground = Brushes.Gray;
                    
                    MessageBox.Show("Registro adicionado com sucesso!", 
                                  "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    MessageBox.Show("Erro ao salvar. Verifique se o registro já existe ou se há algum problema com os dados.", 
                                  "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Fallback para o método antigo (campos de texto)
            var servicoTxt = txtServico.Text.Trim();
            var classificacaoTxt = txtClassificacao.Text.Trim();

            // Validar que ambos têm conteúdo
            if (string.IsNullOrEmpty(servicoTxt) || string.IsNullOrEmpty(classificacaoTxt))
            {
                MessageBox.Show("Por favor, informe o serviço e a classificação.", 
                              "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PadLeft para garantir 3 dígitos (preenche com zeros à esquerda)
            servicoTxt = servicoTxt.PadLeft(3, '0');
            classificacaoTxt = classificacaoTxt.PadLeft(3, '0');

            // Validar comprimento máximo
            if (servicoTxt.Length > 3 || classificacaoTxt.Length > 3)
            {
                MessageBox.Show("O serviço e a classificação devem ter no máximo 3 dígitos.", 
                              "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Se há item selecionado, não permitir salvar (apenas excluir)
                if (itemSelecionado != null)
                {
                    MessageBox.Show("Um registro já está selecionado. Para criar um novo registro, clique em 'Novo' primeiro. Para modificar, exclua o registro atual e crie um novo.", 
                                  "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Verificar se já existe antes de adicionar
                var existe = await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _servicoClassificacaoService.ExisteAsync(servicoTxt, classificacaoTxt, _competenciaAtiva));
                
                if (existe)
                {
                    MessageBox.Show($"Já existe uma classificação '{classificacaoTxt}' para o serviço '{servicoTxt}' nesta competência.", 
                                  "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var novoItem = new ServicoClassificacao
                {
                    CoServico = servicoTxt,
                    CoClassificacao = classificacaoTxt,
                    DtCompetencia = _competenciaAtiva,
                    NoClassificacao = string.Empty
                };

                // Adicionar novo
                await DatabaseRequestQueue.Instance.EnqueueAsync(
                    async () => await _servicoClassificacaoService.AdicionarAsync(novoItem));

                await carregarDados();
                dataGrid.SelectedIndex = -1;
                itemSelecionado = null;
                txtServico.Text = string.Empty;
                txtClassificacao.Text = string.Empty;
                
                MessageBox.Show("Registro adicionado com sucesso!", 
                              "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        btnCancelar.Click += (s, e) =>
        {
            // Remover linha nova se existir
            if (linhaNova != null && itensExibicao.Contains(linhaNova))
            {
                itensExibicao.Remove(linhaNova);
                linhaNova = null;
            }
            
            dataGrid.SelectedIndex = -1;
            itemSelecionado = null;
            txtServico.Text = string.Empty;
            txtClassificacao.Text = string.Empty;
            txtServico.IsReadOnly = false;
            txtClassificacao.IsReadOnly = false;
            btnSalvar.IsEnabled = true;
            btnExcluir.IsEnabled = false;
            statusText.Text = "Informe o serviço e a classificação com 3 digitos";
            statusText.Foreground = Brushes.Gray;
        };

        btnExcluir.Click += async (s, e) =>
        {
            if (itemSelecionado == null)
            {
                MessageBox.Show("Por favor, selecione um registro para excluir.", 
                              "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmacao = MessageBox.Show(
                $"Deseja realmente excluir o registro?\nServiço: {itemSelecionado.CoServico}\nClassificação: {itemSelecionado.CoClassificacao}",
                "Confirmar Exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacao == MessageBoxResult.Yes)
            {
                try
                {
                    await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _servicoClassificacaoService.RemoverAsync(
                            itemSelecionado.CoServico, 
                            itemSelecionado.CoClassificacao, 
                            _competenciaAtiva));

                    await carregarDados();
                    
                    // Limpar campos e habilitar botões
                    dataGrid.SelectedIndex = -1;
                    itemSelecionado = null;
                    txtServico.Text = string.Empty;
                    txtClassificacao.Text = string.Empty;
                    txtServico.IsReadOnly = false;
                    txtClassificacao.IsReadOnly = false;
                    btnSalvar.IsEnabled = true;
                    btnExcluir.IsEnabled = false;
                    
                    MessageBox.Show("Registro excluído com sucesso!", 
                                  "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao excluir:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        };

        dialog.ShowDialog();
    }


    // Classe auxiliar para exibição no DataGrid
    private class ServicoClassificacaoItem
    {
        public int Id { get; set; }
        public string CoServico { get; set; } = string.Empty;
        public string DescricaoServico { get; set; } = string.Empty;
        public string CoClassificacao { get; set; } = string.Empty;
        public string DescricaoClassificacao { get; set; } = string.Empty;
        public ServicoClassificacao? ServicoClassificacao { get; set; }
        public bool IsNovo { get; set; } = false;
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

    private async void Importar_Click(object sender, RoutedEventArgs e)
    {
        // Cria FirebirdContext para a importação
        var importContext = new Infrastructure.Data.FirebirdContext(_configurationReader);
        
        var importWindow = new ImportWindow(importContext, _configurationReader)
        {
            Owner = this
        };
        
        importWindow.ShowDialog();
        
        // Se a importação foi concluída com sucesso, atualiza a listagem de competências
        if (importWindow.ImportacaoConcluidaComSucesso)
        {
            try
            {
                await CarregarCompetenciasDisponiveisAsync();
                MessageBox.Show(
                    "Importação concluída com sucesso!\n\nA listagem de competências foi atualizada.",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Importação concluída, mas houve erro ao atualizar a listagem:\n\n{ex.Message}",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        
        // Dispose do contexto após fechar a janela
        importContext.Dispose();
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

    private async void TesteAcentuacao_Click(object sender, RoutedEventArgs e)
    {
        await TestarAcentuacaoAsync();
    }

    private async void ProcComuns_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Buscar todos os procedimentos comuns
            // Os dados já vêm com encoding correto do repositório (FirebirdReaderHelper.GetStringSafe)
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
                            
                            // Recarregar lista (dados já vêm com encoding correto do repositório)
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
    
    private void ProcedimentosDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
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
    
    private Task EditarObservacaoProcedimentoComumAsync(ProcedimentoComum procComum)
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
        
        return Task.CompletedTask;
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
                
                // Recarregar lista (dados já vêm com encoding correto do repositório)
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
                
                // Recarregar lista (dados já vêm com encoding correto do repositório)
                var atualizados = await _procedimentoComumService.BuscarTodosAsync();
                dataGrid.ItemsSource = atualizados;
                labelInfo.Content = $"Total de procedimentos comuns: {atualizados.Count()}";
                
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
        // Verifica se já está carregando
        if (_isLoading)
        {
            return;
        }

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
        var cancellationToken = new CancellationTokenSource().Token;

        try
        {
            SetLoadingStatus($"Buscando {filtroSelecionado} relacionados...", true);
            StatusTextBlock.Text = $"Buscando {filtroSelecionado} relacionados...";
            
            IEnumerable<RelacionadoItem> relacionados;

            // Busca os relacionados baseado no filtro selecionado usando a fila de requisições
            switch (filtroSelecionado)
            {
                case "Cid10":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarCID10RelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Compatíveis":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarCompativeisRelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Habilitação":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarHabilitacoesRelacionadasAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "CBO":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarCBOsRelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Serviços":
                    var servicos = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarServicosRelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    ExibirServicosRelacionados(servicos, filtroSelecionado, procedimentoSelecionado.CoProcedimento);
                    SetLoadingStatus("Pronto", false);
                    return;
                case "Tipo de Leito":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarTiposLeitoRelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Modalidade":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarModalidadesRelacionadasAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Descrição":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarDescricaoRelacionadaAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Detalhes":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarDetalhesRelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Incremento":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarIncrementosRelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                case "Instrumento de Registro":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarInstrumentosRegistroRelacionadosAsync(
                            procedimentoSelecionado.CoProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    break;
                default:
                    MessageBox.Show($"Filtro '{filtroSelecionado}' não reconhecido.", 
                                  "Erro", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                    SetLoadingStatus("Pronto", false);
                    StatusTextBlock.Text = "Pronto";
                    return;
            }

            // Exibe os resultados em uma janela
            ExibirRelacionados(relacionados, filtroSelecionado, procedimentoSelecionado.CoProcedimento);
            
            SetLoadingStatus("Pronto", false);
            StatusTextBlock.Text = $"Encontrados {relacionados.Count()} registro(s) de {filtroSelecionado}";
        }
        catch (OperationCanceledException)
        {
            SetLoadingStatus("Pronto", false);
            StatusTextBlock.Text = "Operação cancelada";
        }
        catch (Exception ex)
        {
            SetLoadingStatus("Erro", false);
            
            // Não mostrar erro ao usuário se for erro de concorrência
            if (!ex.Message.Contains("lock", StringComparison.OrdinalIgnoreCase) && 
                !ex.Message.Contains("concurrent", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Erro ao buscar relacionados:\n\n{ex.Message}", 
                              "Erro", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            else
            {
                StatusTextBlock.Text = "Aguarde a operação anterior concluir";
            }
        }
    }

    private void ExibirRelacionados(IEnumerable<RelacionadoItem> relacionados, string tipoFiltro, string coProcedimento)
    {
        // Para "Descrição" e "Detalhes", usa uma janela maior com TextBox expansível
        bool isDescricaoOuDetalhes = tipoFiltro == "Descrição" || tipoFiltro == "Detalhes";
        
        var dialog = new Window
        {
            Title = $"{tipoFiltro} relacionados ao procedimento {coProcedimento}",
            Width = isDescricaoOuDetalhes ? 900 : 700,
            Height = isDescricaoOuDetalhes ? 600 : 500,
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

        // Se for Descrição ou Detalhes e houver registros, usa layout especial
        if (isDescricaoOuDetalhes && relacionados.Any())
        {
            var primeiroItem = relacionados.First();
            
            // Ajusta as linhas do grid para acomodar DataGrid + TextBox
            grid.RowDefinitions.Clear();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Título
            
            if (tipoFiltro == "Detalhes")
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(7, GridUnitType.Star) }); // DataGrid (70%)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) }); // TextBox (30%)
            }
            else
            {
                // Para Descrição, o Label ocupa pouco espaço e o TextBox ocupa o resto
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Label
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // TextBox
            }

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botão
            
            // TextBox para descrição (comum para Detalhes e Descrição)
            // Para "Descrição": quebra linha (Wrap), sem rolagem horizontal
            // Para "Detalhes": sem quebra (NoWrap), com rolagem horizontal
            var textBoxDescricao = new TextBox
            {
                Text = primeiroItem.DescricaoCompleta ?? primeiroItem.Descricao ?? "Nenhuma descrição disponível.",
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(10, 5, 10, 5),
                FontSize = 12,
                AcceptsReturn = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // DataGrid na parte superior (para Detalhes)
            if (tipoFiltro == "Detalhes")
            {
                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    HeadersVisibility = DataGridHeadersVisibility.Column,
                    Margin = new Thickness(10, 5, 10, 5),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    CanUserResizeColumns = true,
                    CanUserReorderColumns = false,
                    ColumnWidth = DataGridLength.Auto
                };

                // Colunas: Comp. | Cód. | Descrição
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Comp.",
                    Binding = new System.Windows.Data.Binding("InformacaoAdicional"),
                    Width = new DataGridLength(80),
                    MinWidth = 60,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Cód.",
                    Binding = new System.Windows.Data.Binding("Codigo"),
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Descrição",
                    Binding = new System.Windows.Data.Binding("Descricao"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 300,
                    CanUserResize = true
                });

                dataGrid.ItemsSource = relacionados;
                dataGrid.SelectedIndex = 0;
                
                // Evento para atualizar TextBox quando seleciona outra linha
                dataGrid.SelectionChanged += (s, e) =>
                {
                    if (dataGrid.SelectedItem is RelacionadoItem itemSelecionado)
                    {
                        textBoxDescricao.Text = itemSelecionado.DescricaoCompleta ?? itemSelecionado.Descricao ?? "Nenhuma descrição disponível.";
                    }
                };
                
                Grid.SetRow(dataGrid, 1);
                grid.Children.Add(dataGrid);
            }
            else
            {
                // Para "Descrição", mostra apenas o label do código
                var labelCodigo = new Label
                {
                    Content = $"Código do Procedimento: {primeiroItem.Codigo}",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10, 5, 10, 5)
                };
                Grid.SetRow(labelCodigo, 1);
                grid.Children.Add(labelCodigo);
            }
            
            // Adiciona TextBox na linha 2 (comum para Detalhes e Descrição)
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
                Margin = new Thickness(10, 5, 10, 5),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                CanUserResizeColumns = true,
                CanUserReorderColumns = false,
                ColumnWidth = DataGridLength.Auto
            };

            // Customizar colunas baseado no tipo de filtro
            if (tipoFiltro == "Habilitação")
            {
                // Para Habilitações: Habil. | Comp. (Grupo) | Descrição
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Habil.",
                    Binding = new System.Windows.Data.Binding("Codigo"),
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                });

                // Coluna Comp. com formatação condicional (vermelho quando vazio)
                var compColumn = new DataGridTemplateColumn
                {
                    Header = "Comp.",
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                };
                
                var compCellTemplate = new DataTemplate();
                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(5, 2, 5, 2));
                
                // Binding para texto: mostra "Sem grupo" quando vazio, senão mostra o grupo
                var textBinding = new System.Windows.Data.Binding("InformacaoAdicional")
                {
                    Converter = new HabilitaçãoGrupoConverter()
                };
                textBlockFactory.SetBinding(TextBlock.TextProperty, textBinding);
                
                // Binding para cor: vermelho quando vazio, preto quando tem grupo
                var colorBinding = new System.Windows.Data.Binding("InformacaoAdicional")
                {
                    Converter = new HabilitaçãoGrupoColorConverter()
                };
                textBlockFactory.SetBinding(TextBlock.ForegroundProperty, colorBinding);
                
                compCellTemplate.VisualTree = textBlockFactory;
                compColumn.CellTemplate = compCellTemplate;
                dataGrid.Columns.Add(compColumn);

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Descrição",
                    Binding = new System.Windows.Data.Binding("Descricao"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 300,
                    CanUserResize = true
                });
            }
            else if (tipoFiltro == "CBO")
            {
                // Para CBO: CBO | Comp. (Competência) | Descrição
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "CBO",
                    Binding = new System.Windows.Data.Binding("Codigo"),
                    Width = new DataGridLength(120),
                    MinWidth = 100,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Comp.",
                    Binding = new System.Windows.Data.Binding("InformacaoAdicional"),
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Descrição",
                    Binding = new System.Windows.Data.Binding("Descricao"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 300,
                    CanUserResize = true
                });
            }
            else if (tipoFiltro == "Modalidade")
            {
                // Para Modalidade: Comp. | Código | Descrição
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Comp.",
                    Binding = new System.Windows.Data.Binding("InformacaoAdicional"),
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Código",
                    Binding = new System.Windows.Data.Binding("Codigo"),
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Descrição",
                    Binding = new System.Windows.Data.Binding("Descricao"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 300,
                    CanUserResize = true
                });
            }
            else if (tipoFiltro == "Instrumento de Registro")
            {
                // Para Instrumento de Registro: Comp. | Código | Descrição
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Comp.",
                    Binding = new System.Windows.Data.Binding("InformacaoAdicional"),
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Código",
                    Binding = new System.Windows.Data.Binding("Codigo"),
                    Width = new DataGridLength(100),
                    MinWidth = 80,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Descrição",
                    Binding = new System.Windows.Data.Binding("Descricao"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 300,
                    CanUserResize = true
                });
            }
            else
            {
                // Para outros tipos: Código | Descrição | Informação Adicional
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Código",
                    Binding = new System.Windows.Data.Binding("Codigo"),
                    Width = new DataGridLength(150),
                    MinWidth = 100,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Descrição",
                    Binding = new System.Windows.Data.Binding("Descricao"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 300,
                    CanUserResize = true
                });

                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Informação Adicional",
                    Binding = new System.Windows.Data.Binding("InformacaoAdicional"),
                    Width = new DataGridLength(200),
                    MinWidth = 150,
                    CanUserResize = true
                });
            }

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

    private void ExibirServicosRelacionados(IEnumerable<ServicoRelacionadoItem> servicos, string tipoFiltro, string coProcedimento)
    {
        var dialog = new Window
        {
            Title = $"{tipoFiltro} relacionados ao procedimento {coProcedimento}",
            Width = 1000,
            Height = 500,
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

        // Botão de fechar
        var btnFechar = new Button
        {
            Content = "Fechar",
            Width = 100,
            Height = 30,
            Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnFechar.Click += (s, e) => dialog.Close();

        // DataGrid para exibir os serviços com todas as colunas
        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(10, 5, 10, 5),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            CanUserResizeColumns = true,
            CanUserReorderColumns = false,
            ColumnWidth = DataGridLength.Auto
        };

        // Colunas para Serviços: Comp. | Serv. | Descrição Serviço | Class. | Descrição Classificação
        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Comp.",
            Binding = new System.Windows.Data.Binding("Competencia"),
            Width = new DataGridLength(100),
            MinWidth = 80,
            CanUserResize = true
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Serv.",
            Binding = new System.Windows.Data.Binding("Codigo"),
            Width = new DataGridLength(100),
            MinWidth = 80,
            CanUserResize = true
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Descrição Serviço",
            Binding = new System.Windows.Data.Binding("Descricao"),
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            MinWidth = 200,
            CanUserResize = true
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Class.",
            Binding = new System.Windows.Data.Binding("CodigoClassificacao"),
            Width = new DataGridLength(100),
            MinWidth = 80,
            CanUserResize = true
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Descrição Classificação",
            Binding = new System.Windows.Data.Binding("DescricaoClassificacao"),
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            MinWidth = 200,
            CanUserResize = true
        });

        dataGrid.ItemsSource = servicos.ToList();
        Grid.SetRow(dataGrid, 1);
        grid.Children.Add(dataGrid);
        
        // Botão na linha 2
        Grid.SetRow(btnFechar, 2);
        grid.Children.Add(btnFechar);

        // Mensagem se não houver registros
        if (!servicos.Any())
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

    private async void FiltrosComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _currentCancellationTokenSource?.Cancel();
        _currentCancellationTokenSource = new CancellationTokenSource();
        await AtualizarAreaRelacionadosAsync(_currentCancellationTokenSource.Token);
    }

    private async Task AtualizarAreaRelacionadosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar se AreaRelacionados existe
            if (AreaRelacionados == null)
            {
                return;
            }
            
            // Verificar se foi cancelado
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            // Limpar área
            AreaRelacionados.Children.Clear();

            var procedimento = ProcedimentosDataGrid.SelectedItem as Procedimento;
            if (procedimento == null)
            {
                if (AreaRelacionados != null)
                {
                    var msg = new TextBlock
                    {
                        Text = "Selecione um procedimento para ver os itens relacionados.",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic
                    };
                    AreaRelacionados.Children.Add(msg);
                }
                return;
            }

            if (FiltrosComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                return;
            }

            var tipoFiltro = selectedItem.Content?.ToString() ?? string.Empty;
            var coProcedimento = procedimento.CoProcedimento;
            
            if (string.IsNullOrEmpty(tipoFiltro))
            {
                return;
            }

            // Verificar cache
            var cacheKey = GerarChaveCache(coProcedimento, _competenciaAtiva, tipoFiltro);
            if (_cacheRelacionados.TryGetValue(cacheKey, out var cachedResult))
            {
                RenderizarConteudoRelacionado(cachedResult, tipoFiltro);
                return;
            }

            // Mostrar loading
            SetLoadingStatus($"Carregando {tipoFiltro}...", true);
            
            if (AreaRelacionados != null)
            {
                var loading = new TextBlock
                {
                    Text = "Carregando...",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                AreaRelacionados.Children.Add(loading);
            }

            IEnumerable<object>? relacionados = null;

            // Usar fila de requisições para garantir execução sequencial
            switch (tipoFiltro)
            {
                case "Cid10":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarCID10RelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Compatíveis":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarCompativeisRelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Habilitação":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarHabilitacoesRelacionadasAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "CBO":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarCBOsRelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Serviços":
                    // Serviços retorna um tipo diferente (ServicoRelacionadoItem)
                    var servicos = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => await _procedimentoService.BuscarServicosRelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken),
                        cancellationToken);
                    RenderizarConteudoRelacionado(servicos.Cast<object>(), tipoFiltro);
                    SetLoadingStatus("Pronto", false);
                    return;
                case "Tipo de Leito":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarTiposLeitoRelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Modalidade":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarModalidadesRelacionadasAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Instrumento de Registro":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarInstrumentosRegistroRelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Detalhes":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarDetalhesRelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Incremento":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarIncrementosRelacionadosAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
                case "Descrição":
                    relacionados = await DatabaseRequestQueue.Instance.EnqueueAsync(
                        async () => (await _procedimentoService.BuscarDescricaoRelacionadaAsync(coProcedimento, _competenciaAtiva, cancellationToken)).Cast<object>(),
                        cancellationToken);
                    break;
            }

            if (relacionados != null)
            {
                // Armazenar no cache
                _cacheRelacionados[cacheKey] = relacionados;
                RenderizarConteudoRelacionado(relacionados, tipoFiltro);
            }
            
            SetLoadingStatus("Pronto", false);
        }
        catch (OperationCanceledException)
        {
            // Requisição foi cancelada - não fazer nada
            SetLoadingStatus("Pronto", false);
        }
        catch (Exception ex)
        {
            SetLoadingStatus("Erro", false);
            
            // Verificar se AreaRelacionados existe antes de usar
            if (AreaRelacionados != null)
            {
                try
                {
                    AreaRelacionados.Children.Clear();
                    var err = new TextBlock
                    {
                        Text = $"Erro ao carregar: {ex.Message}",
                        Foreground = Brushes.Red,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(5)
                    };
                    AreaRelacionados.Children.Add(err);
                }
                catch
                {
                    // Se ainda assim falhar, apenas logar o erro
                    System.Diagnostics.Debug.WriteLine($"Erro ao exibir mensagem de erro na área relacionada: {ex.Message}");
                }
            }
            else
            {
                // Se AreaRelacionados não existe, mostrar erro em MessageBox apenas se não for erro de concorrência
                if (!ex.Message.Contains("lock", StringComparison.OrdinalIgnoreCase) && 
                    !ex.Message.Contains("concurrent", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Erro ao carregar dados relacionados:\n\n{ex.Message}", 
                                  "Erro", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
            }
        }
    }

    private void RenderizarConteudoRelacionado(IEnumerable<object> itens, string tipoFiltro)
    {
        // Verificar se AreaRelacionados existe
        if (AreaRelacionados == null)
        {
            return;
        }
        
        AreaRelacionados.Children.Clear();

        if (itens == null || !itens.Cast<object>().Any())
        {
            var msg = new TextBlock
            {
                Text = "Nenhum registro encontrado.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold
            };
            AreaRelacionados.Children.Add(msg);
            return;
        }

        // Grid container
        var grid = new Grid();
        
        // Se for Serviços, é um caso à parte com colunas específicas
        if (tipoFiltro == "Serviços")
        {
            var dataGridServ = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                Margin = new Thickness(0),
                BorderThickness = new Thickness(0),
                ItemsSource = itens
            };

            dataGridServ.Columns.Add(new DataGridTextColumn { Header = "Comp.", Binding = new Binding("Competencia"), Width = new DataGridLength(80), MinWidth = 60, CanUserResize = true });
            dataGridServ.Columns.Add(new DataGridTextColumn { Header = "Serv.", Binding = new Binding("Codigo"), Width = new DataGridLength(80), MinWidth = 60, CanUserResize = true });
            dataGridServ.Columns.Add(new DataGridTextColumn { Header = "Descrição Serviço", Binding = new Binding("Descricao"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 200, CanUserResize = true });
            dataGridServ.Columns.Add(new DataGridTextColumn { Header = "Class.", Binding = new Binding("CodigoClassificacao"), Width = new DataGridLength(80), MinWidth = 60, CanUserResize = true });
            dataGridServ.Columns.Add(new DataGridTextColumn { Header = "Descrição Classificação", Binding = new Binding("DescricaoClassificacao"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 200, CanUserResize = true });

            if (AreaRelacionados != null)
            {
                AreaRelacionados.Children.Add(dataGridServ);
            }
            return;
        }

        // Para os demais tipos (RelacionadoItem)
        var listaItens = itens.Cast<RelacionadoItem>().ToList();
        // Para "Descrição", mostra apenas um TextBox grande (sem DataGrid)
        // Para "Detalhes", mostra DataGrid + TextBox
        bool isDescricao = tipoFiltro == "Descrição";
        bool isDetalhes = tipoFiltro == "Detalhes";

        if (isDescricao)
        {
            // Para Descrição: apenas um TextBox grande ocupando toda a área
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }
        else if (isDetalhes)
        {
            // Para Detalhes: DataGrid + Splitter + TextBox
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataGrid
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) }); // Splitter
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // TextBox
        }
        else
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            Margin = new Thickness(0),
            BorderThickness = new Thickness(0),
            ItemsSource = listaItens,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            CanUserResizeColumns = true,
            CanUserReorderColumns = false,
            ColumnWidth = DataGridLength.Auto
        };

        // Configuração de colunas baseada no tipo
        if (tipoFiltro == "Modalidade" || tipoFiltro == "Instrumento de Registro")
        {
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Comp.",
                Binding = new Binding("InformacaoAdicional"),
                Width = new DataGridLength(80),
                MinWidth = 60,
                CanUserResize = true
            });
        }
        else if (tipoFiltro == "Habilitação")
        {
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Comp.",
                Binding = new Binding("InformacaoAdicional") { Converter = new HabilitaçãoGrupoConverter() },
                Width = new DataGridLength(80),
                MinWidth = 60,
                CanUserResize = true
            });
        }

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Código",
            Binding = new Binding("Codigo"),
            Width = new DataGridLength(100),
            MinWidth = 80,
            CanUserResize = true
        });

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Descrição",
            Binding = new Binding("Descricao"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 300,
            CanUserResize = true
        });

        if (tipoFiltro != "Modalidade" && tipoFiltro != "Instrumento de Registro" && tipoFiltro != "Habilitação")
        {
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Informação Adicional",
                Binding = new Binding("InformacaoAdicional"),
                Width = new DataGridLength(150),
                MinWidth = 100,
                CanUserResize = true
            });
        }

        // Para "Descrição", mostra apenas um TextBox grande com a descrição
        if (isDescricao)
        {
            var primeiroItem = listaItens.FirstOrDefault();
            var textBoxDescricao = new TextBox
            {
                Text = primeiroItem != null 
                    ? (primeiroItem.DescricaoCompleta ?? primeiroItem.Descricao ?? "Nenhuma descrição disponível.")
                    : "Nenhuma descrição disponível.",
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                FontSize = 12,
                AcceptsReturn = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetRow(textBoxDescricao, 0);
            grid.Children.Add(textBoxDescricao);
        }
        else
        {
            // Para outros tipos (incluindo Detalhes), mostra DataGrid
            Grid.SetRow(dataGrid, 0);
            grid.Children.Add(dataGrid);

            // Para "Detalhes", adiciona TextBox abaixo do DataGrid
            if (isDetalhes)
            {
                var splitter = new GridSplitter
                {
                    Height = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.LightGray
                };
                Grid.SetRow(splitter, 1);
                grid.Children.Add(splitter);

                var textBoxDesc = new TextBox
                {
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    IsReadOnly = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Margin = new Thickness(0, 5, 0, 0),
                    Padding = new Thickness(5)
                };
                Grid.SetRow(textBoxDesc, 2);
                grid.Children.Add(textBoxDesc);

                // Evento de seleção
                dataGrid.SelectionChanged += (s, e) =>
                {
                    if (dataGrid.SelectedItem is RelacionadoItem item)
                    {
                        textBoxDesc.Text = !string.IsNullOrEmpty(item.DescricaoCompleta) 
                            ? item.DescricaoCompleta 
                            : item.Descricao;
                    }
                };

                // Selecionar primeiro item se houver
                if (listaItens.Any())
                {
                    dataGrid.SelectedIndex = 0;
                }
            }
        }

        if (AreaRelacionados != null)
        {
            AreaRelacionados.Children.Add(grid);
        }
    }

    /// <summary>
    /// Obtém o encoding Windows-1252 de forma segura, com fallback para ISO-8859-1
    /// </summary>
    private static Encoding GetWindows1252Encoding()
    {
        try
        {
            return Encoding.GetEncoding(1252);
        }
        catch
        {
            try
            {
                return Encoding.GetEncoding("Windows-1252");
            }
            catch
            {
                // Fallback para ISO-8859-1 (compatível para caracteres brasileiros)
                return Encoding.GetEncoding("ISO-8859-1");
            }
        }
    }

    /// <summary>
    /// Testa inserção, leitura e validação de acentuação - Versão simplificada
    /// </summary>
    private async Task TestarAcentuacaoAsync()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            $"TesteAcentuacao_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        
        var log = new StringBuilder();
        log.AppendLine("=== TESTE DE ACENTUAÇÃO ===");
        log.AppendLine($"Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        log.AppendLine();

        ProcedimentoComum? procedimentoTeste = null;

        try
        {
            // 1. Preparar dados de teste
            var textoTeste = "AÇÕES RELACIONADAS A DOAÇÃO DE ÓRGÃOS E TECIDOS PARA TRANSPLANTE";
            log.AppendLine($"Texto de teste: {textoTeste}");
            log.AppendLine();

            // 2. Obter próximo código
            var proximoCodigo = await _procedimentoComumService.ObterProximoCodigoAsync();
            log.AppendLine($"Código gerado: {proximoCodigo}");
            log.AppendLine();

            // 3. Criar e inserir
            procedimentoTeste = new ProcedimentoComum
            {
                PrcCod = proximoCodigo,
                PrcCodProc = $"TESTE{proximoCodigo}",
                PrcNoProcedimento = textoTeste,
                PrcObservacoes = "TESTE: ção, ão, ões, ç, á, é, í, ó, ú"
            };

            log.AppendLine("=== INSERÇÃO ===");
            log.AppendLine($"Inserindo: {procedimentoTeste.PrcNoProcedimento}");
            await _procedimentoComumService.AdicionarAsync(procedimentoTeste);
            log.AppendLine("✓ Inserido com sucesso");
            log.AppendLine();

            // 4. Aguardar e ler de volta
            await Task.Delay(300);
            log.AppendLine("=== LEITURA ===");
            var lido = await _procedimentoComumService.BuscarPorCodigoAsync(procedimentoTeste.PrcCod);
            
            if (lido == null)
            {
                log.AppendLine("✗ ERRO: Registro não encontrado após inserção!");
                File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);
                MessageBox.Show("Erro: Registro não encontrado após inserção", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            log.AppendLine($"Lido: {lido.PrcNoProcedimento}");
            log.AppendLine();
            
            // Debug: Mostrar bytes do texto original e lido
            log.AppendLine("=== DEBUG BYTES ===");
            // Obtém encoding Windows-1252 com fallback seguro (mesmo padrão usado nos repositórios)
            Encoding encoding1252;
            try
            {
                encoding1252 = Encoding.GetEncoding(1252);
            }
            catch
            {
                try
                {
                    encoding1252 = Encoding.GetEncoding("Windows-1252");
                }
                catch
                {
                    encoding1252 = Encoding.GetEncoding("ISO-8859-1"); // Fallback compatível
                }
            }
            
            var bytesOriginal = encoding1252.GetBytes(textoTeste);
            var bytesLido = lido.PrcNoProcedimento != null ? encoding1252.GetBytes(lido.PrcNoProcedimento) : Array.Empty<byte>();
            
            log.AppendLine($"Bytes original (Windows-1252): {string.Join(", ", bytesOriginal.Select(b => $"0x{b:X2}"))}");
            log.AppendLine($"Bytes lido (Windows-1252):     {string.Join(", ", bytesLido.Select(b => $"0x{b:X2}"))}");
            log.AppendLine($"Bytes são iguais: {bytesOriginal.SequenceEqual(bytesLido)}");
            log.AppendLine();
            
            // Debug: Verificar bytes do "Ç" especificamente
            var indiceCedilha = textoTeste.IndexOf('Ç');
            if (indiceCedilha >= 0 && indiceCedilha < bytesOriginal.Length)
            {
                log.AppendLine($"Byte do 'Ç' no original: 0x{bytesOriginal[indiceCedilha]:X2} (deveria ser 0xC7)");
            }
            if (lido.PrcNoProcedimento != null)
            {
                var indiceCedilhaLido = lido.PrcNoProcedimento.IndexOf('Ç');
                if (indiceCedilhaLido < 0)
                {
                    // Verifica se tem "Ã" seguido de algo
                    var indiceA = lido.PrcNoProcedimento.IndexOf('Ã');
                    if (indiceA >= 0 && indiceA + 1 < bytesLido.Length)
                    {
                        log.AppendLine($"Encontrado 'Ã' na posição {indiceA}, bytes: 0x{bytesLido[indiceA]:X2} 0x{bytesLido[indiceA + 1]:X2}");
                        log.AppendLine($"  Isso sugere que foi salvo como UTF-8 (0xC3 0x87) ao invés de Windows-1252 (0xC7)");
                    }
                }
                else
                {
                    log.AppendLine($"Byte do 'Ç' no lido: 0x{bytesLido[indiceCedilhaLido]:X2}");
                }
            }
            
            // Debug: Comparar primeiros bytes
            log.AppendLine();
            log.AppendLine("Primeiros 10 bytes comparados:");
            for (int i = 0; i < Math.Min(10, Math.Min(bytesOriginal.Length, bytesLido.Length)); i++)
            {
                bool igual = bytesOriginal[i] == bytesLido[i];
                log.AppendLine($"  Pos {i}: Original=0x{bytesOriginal[i]:X2}, Lido=0x{bytesLido[i]:X2}, {(igual ? "✓" : "✗")}");
            }
            log.AppendLine();

            // 5. Validação simples e direta
            log.AppendLine("=== VALIDAÇÃO ===");
            bool textoIgual = lido.PrcNoProcedimento == textoTeste;
            // Verifica se os acentos presentes no texto de teste foram preservados
            bool temAcentos = lido.PrcNoProcedimento?.Contains("Ç") == true && 
                             lido.PrcNoProcedimento.Contains("Õ") == true &&
                             lido.PrcNoProcedimento.Contains("Ã") == true &&
                             lido.PrcNoProcedimento.Contains("Ó") == true;

            log.AppendLine($"Texto original: {textoTeste}");
            log.AppendLine($"Texto lido:     {lido.PrcNoProcedimento ?? "(null)"}");
            log.AppendLine($"São iguais: {textoIgual}");
            log.AppendLine($"Tem acentos: {temAcentos}");
            log.AppendLine();

            // 6. Verificar caracteres específicos
            var caracteres = new[] { 'Ç', 'Õ', 'Ã', 'Ó', 'Á' };
            log.AppendLine("Verificação de caracteres:");
            bool todosCorretos = true;
            foreach (var c in caracteres)
            {
                bool originalTem = textoTeste.Contains(c);
                bool lidoTem = lido.PrcNoProcedimento?.Contains(c) == true;
                bool correto = originalTem == lidoTem;
                todosCorretos = todosCorretos && correto;
                log.AppendLine($"  {c}: Original={originalTem}, Lido={lidoTem}, {(correto ? "✓" : "✗")}");
            }
            log.AppendLine();

            // 7. Resultado final
            bool sucesso = textoIgual && temAcentos && todosCorretos;
            log.AppendLine("=== RESULTADO ===");
            if (sucesso)
            {
                log.AppendLine("✓✓✓ TESTE PASSOU - ACENTUAÇÃO CORRETA ✓✓✓");
            }
            else
            {
                log.AppendLine("✗✗✗ TESTE FALHOU - ACENTUAÇÃO INCORRETA ✗✗✗");
                log.AppendLine();
                log.AppendLine("Diferenças encontradas:");
                if (!textoIgual)
                {
                    log.AppendLine($"  - Textos não são idênticos");
                    log.AppendLine($"    Original: [{textoTeste}]");
                    log.AppendLine($"    Lido:     [{lido.PrcNoProcedimento}]");
                }
                if (!temAcentos)
                {
                    log.AppendLine($"  - Caracteres acentuados não foram preservados");
                }
            }

            // 8. Limpar
            log.AppendLine();
            log.AppendLine("=== LIMPEZA ===");
            try
            {
                await _procedimentoComumService.RemoverAsync(procedimentoTeste.PrcCod);
                log.AppendLine("✓ Registro de teste excluído");
            }
            catch (Exception ex)
            {
                log.AppendLine($"✗ Erro ao excluir: {ex.Message}");
            }

            // 9. Salvar e mostrar resultado
            File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);

            var mensagem = sucesso
                ? $"✓ TESTE PASSOU!\n\nAcentuação preservada corretamente.\n\nLog: {logPath}"
                : $"✗ TESTE FALHOU!\n\nAcentuação não foi preservada.\n\nLog: {logPath}";

            MessageBox.Show(mensagem, sucesso ? "Sucesso" : "Erro",
                MessageBoxButton.OK, sucesso ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            log.AppendLine();
            log.AppendLine("=== ERRO ===");
            log.AppendLine($"Mensagem: {ex.Message}");
            log.AppendLine($"Tipo: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                log.AppendLine($"Erro interno: {ex.InnerException.Message}");
            }
            log.AppendLine();
            log.AppendLine("Stack Trace:");
            log.AppendLine(ex.StackTrace);

            // Tentar limpar mesmo em caso de erro
            if (procedimentoTeste != null)
            {
                try
                {
                    await _procedimentoComumService.RemoverAsync(procedimentoTeste.PrcCod);
                    log.AppendLine("✓ Registro de teste excluído após erro");
                }
                catch { }
            }

            File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);

            MessageBox.Show($"Erro no teste:\n\n{ex.Message}\n\nLog: {logPath}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Normaliza string para Windows-1252 (garante acentuação correta no DataGrid de Procedimentos Comuns)
    /// </summary>
    private static string NormalizeStringToWindows1252(string text, Encoding encoding1252)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        try
        {
            // Converte a string para bytes usando Windows-1252 e depois de volta para string
            // Isso garante que a string está no encoding correto
            byte[] bytes = encoding1252.GetBytes(text);
            return encoding1252.GetString(bytes);
        }
        catch
        {
            // Se falhar, retorna o texto original
            return text;
        }
    }

    /// <summary>
    /// Handler para garantir que o scroll do mouse funcione corretamente na TreeView
    /// </summary>
    private void TreeViewScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            // Converte o delta do mouse (geralmente 120 ou -120) para pixels de scroll
            // Divide por 3 para obter um scroll suave (ajuste conforme necessário)
            double offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
            e.Handled = true;
        }
    }
}

// Converter para exibir texto na coluna Comp. de Habilitações
public class HabilitaçãoGrupoConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string grupo && !string.IsNullOrWhiteSpace(grupo))
        {
            return grupo;
        }
        return "Sem grupo";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Converter para exibir cor na coluna Comp. de Habilitações
public class HabilitaçãoGrupoColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string grupo && !string.IsNullOrWhiteSpace(grupo))
        {
            return new SolidColorBrush(Colors.Black); // Preto quando tem grupo
        }
        return new SolidColorBrush(Colors.Red); // Vermelho quando não tem grupo
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

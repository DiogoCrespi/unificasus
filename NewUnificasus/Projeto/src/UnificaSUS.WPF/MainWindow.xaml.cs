using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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
    private readonly IConfigurationReader _configurationReader;
    
    private string _competenciaAtiva = string.Empty;
    private ObservableCollection<Grupo> _grupos = new();
    private ObservableCollection<Procedimento> _procedimentos = new();
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
        IConfigurationReader configurationReader)
    {
        InitializeComponent();
        
        _procedimentoService = procedimentoService;
        _competenciaService = competenciaService;
        _grupoService = grupoService;
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
        }
    }

    private void CarregarDetalhesProcedimento(Procedimento procedimento)
    {
        TxtProcedimento.Text = $"{procedimento.CoProcedimento} {procedimento.NoProcedimento ?? ""}";
        TxtValorSA.Text = procedimento.VlSa?.ToString("C", new System.Globalization.CultureInfo("pt-BR")) ?? "R$ 0,00";
        TxtValorSH.Text = procedimento.VlSh?.ToString("C", new System.Globalization.CultureInfo("pt-BR")) ?? "R$ 0,00";
        TxtValorSP.Text = procedimento.VlSp?.ToString("C", new System.Globalization.CultureInfo("pt-BR")) ?? "R$ 0,00";
        TxtPontos.Text = procedimento.QtPontos?.ToString() ?? "0";
        TxtPermanencia.Text = procedimento.QtDiasPermanencia?.ToString() ?? "";
        TxtIdMin.Text = procedimento.VlIdadeMinima?.ToString() ?? "";
        TxtIdMax.Text = procedimento.VlIdadeMaxima?.ToString() ?? "";
        
        // Sexo
        var sexo = procedimento.TpSexo?.Trim()?.ToUpper();
        TxtSexo.Text = sexo switch
        {
            "M" => "Masculino",
            "F" => "Feminino",
            "I" => "Indiferente",
            "" or null => "Não se aplica",
            _ => sexo ?? "Não se aplica"
        };
        
        TxtTempoPermanencia.Text = procedimento.QtTempoPermanencia?.ToString() ?? "9999";
        TxtFinanciamento.Text = procedimento.Financiamento?.NoFinanciamento ?? procedimento.CoFinanciamento ?? "";
        
        // Complexidade
        var complexidade = procedimento.TpComplexidade?.Trim()?.ToUpper();
        TxtComplexidade.Text = complexidade switch
        {
            "AB" => "Atenção Básica",
            "AP" => "Atenção Primária",
            "AM" => "Atenção Média",
            "AA" => "Atenção Alta",
            "" or null => "Não se aplica",
            _ => complexidade ?? "Não se aplica"
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

    private void ExibirComuns_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Funcionalidade em desenvolvimento", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Importar_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Funcionalidade em desenvolvimento", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Relatorios_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Funcionalidade em desenvolvimento", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ProcComuns_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Funcionalidade em desenvolvimento", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ProcedimentosDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        // Ordenação já é feita automaticamente pelo DataGrid
        // Este evento é apenas para garantir que funciona
        e.Handled = false;
    }
}

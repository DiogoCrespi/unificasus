using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UnificaSUS.Application.Services;
using UnificaSUS.Core.Entities;

namespace UnificaSUS.WPF;

/// <summary>
/// Janela de geração de relatórios
/// </summary>
public partial class RelatoriosWindow : Window
{
    private readonly RelatorioService _relatorioService;
    private readonly string _competencia;
    private readonly ObservableCollection<ItemRelatorio> _itensSelecionados;
    private string _tipoFiltroAtual = "Grupo";

    public RelatoriosWindow(RelatorioService relatorioService, string competencia)
    {
        try
        {
            if (relatorioService == null)
                throw new ArgumentNullException(nameof(relatorioService));
            
            if (string.IsNullOrEmpty(competencia))
                throw new ArgumentException("Competência não pode ser vazia", nameof(competencia));

            // Inicializar campos antes do InitializeComponent para evitar null reference
            _relatorioService = relatorioService;
            _competencia = competencia;
            _itensSelecionados = new ObservableCollection<ItemRelatorio>();
            _tipoFiltroAtual = "Grupo"; // Inicializar antes do InitializeComponent

            InitializeComponent();
            
            // Inicializar controles após InitializeComponent
            ListBoxItensSelecionados.ItemsSource = _itensSelecionados;
            StatusTextBlock.Text = $"Competência: {FormatCompetencia(competencia)}";
            
            // Carregar itens iniciais quando a janela for carregada
            Loaded += RelatoriosWindow_Loaded;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao inicializar janela de relatórios:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                          "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private string FormatCompetencia(string competencia)
    {
        if (competencia.Length == 6)
        {
            return $"{competencia.Substring(4, 2)}/{competencia.Substring(0, 4)}";
        }
        return competencia;
    }

    private async void RelatoriosWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await CarregarItensDisponiveisAsync();
    }

    private async void TipoFiltro_Changed(object sender, RoutedEventArgs e)
    {
        // Verificar se os controles já foram inicializados
        if (ComboBoxItens == null || StatusTextBlock == null)
            return;

        if (sender is RadioButton rb && rb.IsChecked == true)
        {
            _tipoFiltroAtual = rb.Content?.ToString() ?? "Grupo";
            ComboBoxItens.Text = string.Empty;
            ComboBoxItens.ItemsSource = null;
            StatusTextBlock.Text = $"Carregando itens...";
            
            // Carregar todos os itens disponíveis para o tipo selecionado
            await CarregarItensDisponiveisAsync();
        }
    }

    private async Task CarregarItensDisponiveisAsync()
    {
        try
        {
            if (ComboBoxItens == null || StatusTextBlock == null)
                return;

            StatusTextBlock.Text = "Carregando itens...";
            
            IEnumerable<ItemRelatorio> itens = _tipoFiltroAtual switch
            {
                "Grupo" => await _relatorioService.BuscarGruposDisponiveisAsync(_competencia, null),
                "Sub-grupo" => await _relatorioService.BuscarSubGruposDisponiveisAsync(_competencia, null),
                "Forma de organização" => await _relatorioService.BuscarFormasOrganizacaoDisponiveisAsync(_competencia, null),
                "Procedimento" => await _relatorioService.BuscarProcedimentosDisponiveisAsync(_competencia, null),
                _ => Enumerable.Empty<ItemRelatorio>()
            };

            var listaItens = itens.ToList();
            
            // Atualizar na thread da UI
            await Dispatcher.InvokeAsync(() =>
            {
                // Debug: verificar se há itens
                if (listaItens.Count == 0)
                {
                    StatusTextBlock.Text = $"Nenhum item encontrado para {_tipoFiltroAtual} na competência {_competencia}";
                    ComboBoxItens.ItemsSource = null;
                    ComboBoxItens.SelectedItem = null;
                    MessageBox.Show($"Nenhum item encontrado para '{_tipoFiltroAtual}' na competência {FormatCompetencia(_competencia)}.\n\nVerifique se há dados para esta competência no banco de dados.", 
                                  "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ComboBoxItens.ItemsSource = listaItens;
                    StatusTextBlock.Text = $"Filtro selecionado: {_tipoFiltroAtual} - {listaItens.Count} itens disponíveis";
                    
                    // Pré-seleciona o primeiro item da lista
                    if (listaItens.Count > 0)
                    {
                        ComboBoxItens.SelectedItem = listaItens.First();
                        ComboBoxItens.Text = listaItens.First().ToString();
                        
                        // Debug: verificar se o ItemsSource foi definido e se os itens têm dados
                        System.Diagnostics.Debug.WriteLine($"ItemsSource definido com {listaItens.Count} itens para {_tipoFiltroAtual}");
                        var primeiroItem = listaItens.First();
                        System.Diagnostics.Debug.WriteLine($"Primeiro item pré-selecionado: Código='{primeiroItem.Codigo}', Nome='{primeiroItem.Nome}', ToString='{primeiroItem.ToString()}'");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar itens:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = $"Erro ao carregar itens: {ex.Message}";
        }
    }

    private void ComboBoxItens_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Filtro automático já funciona com IsEditable="True" no ComboBox
        // O usuário pode digitar e o ComboBox filtra automaticamente
    }

    private void ComboBoxItens_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Quando seleciona um item da lista, já está pronto para adicionar
    }

    private async void ComboBoxItens_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            // Se pressionar Enter, tenta adicionar o item
            await AdicionarItemSelecionadoAsync();
        }
    }

    private async void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        await AdicionarItemSelecionadoAsync();
    }

    private async Task AdicionarItemSelecionadoAsync()
    {
        try
        {
            ItemRelatorio? itemEncontrado = null;

            // Se há um item selecionado no ComboBox, usa ele
            if (ComboBoxItens.SelectedItem is ItemRelatorio itemSelecionado)
            {
                itemEncontrado = itemSelecionado;
            }
            else
            {
                // Se não há seleção, tenta encontrar pelo texto digitado
                var textoDigitado = ComboBoxItens.Text.Trim();
                if (string.IsNullOrWhiteSpace(textoDigitado))
                {
                    MessageBox.Show("Digite ou selecione um item da lista.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Busca o item pelo texto digitado
                if (ComboBoxItens.ItemsSource is IEnumerable<ItemRelatorio> itens)
                {
                    itemEncontrado = itens.FirstOrDefault(i => 
                        i.Codigo.Equals(textoDigitado, StringComparison.OrdinalIgnoreCase) ||
                        i.Nome.Contains(textoDigitado, StringComparison.OrdinalIgnoreCase) ||
                        i.ToString().Contains(textoDigitado, StringComparison.OrdinalIgnoreCase));
                }

                // Se não encontrou na lista atual, busca no banco
                if (itemEncontrado == null)
                {
                    StatusTextBlock.Text = "Buscando item...";
                    IEnumerable<ItemRelatorio> itensBusca = _tipoFiltroAtual switch
                    {
                        "Grupo" => await _relatorioService.BuscarGruposDisponiveisAsync(_competencia, textoDigitado),
                        "Sub-grupo" => await _relatorioService.BuscarSubGruposDisponiveisAsync(_competencia, textoDigitado),
                        "Forma de organização" => await _relatorioService.BuscarFormasOrganizacaoDisponiveisAsync(_competencia, textoDigitado),
                        "Procedimento" => await _relatorioService.BuscarProcedimentosDisponiveisAsync(_competencia, textoDigitado),
                        _ => Enumerable.Empty<ItemRelatorio>()
                    };

                    itemEncontrado = itensBusca.FirstOrDefault();
                }
            }

            if (itemEncontrado != null)
            {
                // Verifica se já não está na lista
                if (!_itensSelecionados.Any(i => i.Tipo == itemEncontrado.Tipo && i.Codigo == itemEncontrado.Codigo))
                {
                    itemEncontrado.Competencia = _competencia;
                    _itensSelecionados.Add(itemEncontrado);
                    StatusTextBlock.Text = $"Item adicionado: {itemEncontrado}";
                    ComboBoxItens.Text = string.Empty;
                    ComboBoxItens.SelectedItem = null;
                }
                else
                {
                    MessageBox.Show("Este item já está na lista de impressão.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Item não encontrado. Verifique o código ou nome digitado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusTextBlock.Text = "Item não encontrado";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao adicionar item:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Erro ao adicionar";
        }
    }

    private void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        _itensSelecionados.Clear();
        StatusTextBlock.Text = "Lista limpa";
    }

    private async void BtnImprimir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_itensSelecionados.Any())
            {
                MessageBox.Show("Adicione pelo menos um item à lista de impressão.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusTextBlock.Text = "Gerando relatório...";

            // Cria configuração do relatório
            var configuracao = new ConfiguracaoRelatorio
            {
                Titulo = TxtTituloRelatorio.Text.Trim(),
                NaoImprimirSPZerado = ChkNaoImprimirSPZerado.IsChecked == true,
                Modelo = RbModeloCodigoNomeValorSP.IsChecked == true ? "CodigoNomeValorSP" : "CodigoNomeValorSP",
                OrdenarPor = RbOrdenarPorCodigo.IsChecked == true ? "Codigo" :
                            RbOrdenarPorNome.IsChecked == true ? "Nome" : "ValorSP"
            };

            // Busca procedimentos
            var procedimentos = await _relatorioService.BuscarProcedimentosParaRelatorioAsync(
                _itensSelecionados, _competencia, configuracao);

            if (!procedimentos.Any())
            {
                MessageBox.Show("Nenhum procedimento encontrado com os critérios selecionados.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusTextBlock.Text = "Nenhum procedimento encontrado";
                return;
            }

            // Mostra diálogo de progresso
            var progressDialog = new Window
            {
                Title = "Gerando Relatório",
                Width = 350,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            var progressStack = new StackPanel
            {
                Margin = new Thickness(20),
                Orientation = Orientation.Vertical
            };

            var progressText = new TextBlock
            {
                Text = "Gerando documento e preparando impressão...",
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var progressBar = new ProgressBar
            {
                Height = 20,
                IsIndeterminate = true
            };

            progressStack.Children.Add(progressText);
            progressStack.Children.Add(progressBar);
            progressDialog.Content = progressStack;

            // Mostra o diálogo de progresso
            progressDialog.Show();
            
            // Força atualização da UI
            await Task.Delay(50); // Pequeno delay para garantir que o diálogo apareça

            try
            {
                // Gera o documento primeiro (pode ser demorado)
                progressText.Text = "Gerando documento...";
                await Task.Delay(10); // Permite atualização da UI
                
                var document = GerarDocumentoRelatorio(procedimentos, configuracao);
                
                // Fecha o diálogo antes de abrir o PrintDialog
                progressDialog.Close();
                
                // Atualiza status
                StatusTextBlock.Text = "Abrindo diálogo de impressão...";
                
                // Abre diálogo de impressão (pode travar um pouco, mas é necessário)
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    StatusTextBlock.Text = "Enviando para impressora...";
                    
                    document.PageHeight = printDialog.PrintableAreaHeight;
                    document.PageWidth = printDialog.PrintableAreaWidth;
                    
                    printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, configuracao.Titulo);
                    
                    StatusTextBlock.Text = $"Relatório gerado: {procedimentos.Count()} procedimentos";
                }
                else
                {
                    StatusTextBlock.Text = "Impressão cancelada";
                }
            }
            catch
            {
                // Fecha o diálogo se ainda estiver aberto
                if (progressDialog.IsVisible)
                {
                    progressDialog.Close();
                }
                throw; // Relança a exceção para ser tratada no catch externo
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar relatório:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Erro ao gerar relatório";
        }
    }

    private FlowDocument GerarDocumentoRelatorio(IEnumerable<ItemRelatorioProcedimento> procedimentos, ConfiguracaoRelatorio configuracao)
    {
        try
        {
            // Cria documento ultra-compacto
            var document = new FlowDocument
            {
                PageWidth = 816,
                PageHeight = 1056,
                PagePadding = new Thickness(20, 15, 20, 15), // Margens mínimas
                FontFamily = new FontFamily("Arial"),
                FontSize = 7.5, // Fonte pequena para máximo aproveitamento
                LineHeight = 10, // Altura mínima
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight
            };

            // Cabeçalho compacto
            var titulo = new Paragraph(new Run(configuracao.Titulo))
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };
            document.Blocks.Add(titulo);

            var competencia = new Paragraph(new Run($"Competência: {FormatCompetencia(_competencia)}"))
            {
                FontSize = 7.5,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(competencia);

            // Lista de procedimentos (em vez de tabela)
            int contador = 1;
            foreach (var proc in procedimentos)
            {
                var valorFormatado = proc.VlSp?.ToString("C", new System.Globalization.CultureInfo("pt-BR")) ?? "R$ 0,00";
                var textoItem = $"{contador}. {proc.CoProcedimento} - {proc.NoProcedimento ?? string.Empty} - Valor: {valorFormatado}";
                
                var itemLista = new Paragraph(new Run(textoItem))
                {
                    FontSize = 8,
                    LineHeight = 12,
                    Margin = new Thickness(0, 0, 0, 4),
                    TextIndent = 0
                };
                
                document.Blocks.Add(itemLista);
                contador++;
            }

            // Rodapé compacto
            var rodape = new Paragraph(new Run($"Total: {procedimentos.Count()} procedimentos"))
            {
                FontSize = 7.5,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 3, 0, 0)
            };
            document.Blocks.Add(rodape);

            return document;
        }
        catch (Exception ex)
        {
            throw new Exception($"Erro ao gerar documento de impressão: {ex.Message}", ex);
        }
    }

}



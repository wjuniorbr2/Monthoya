using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ModuleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyModuleFilter();
        SaveActiveTabState();
    }

    private void ApplyModuleFilter()
    {
        var query = ModuleSearchBox.Text;
        var filteredItems = _moduleItems
            .Where(item => item switch
            {
                LocacaoSummary locacao => ContainsSearch(
                    query,
                    locacao.Codigo,
                    locacao.Status,
                    locacao.TipoLocacao,
                    locacao.ImovelResumo,
                    locacao.LocatarioPrincipalNome,
                    locacao.ProprietarioPrincipalNome,
                    locacao.AlertasTexto),
                _ => ContainsSearch(query, item.ToString())
            })
            .ToList();

        ModuleGrid.ItemsSource = filteredItems;
        ModuleOpenButton.IsEnabled = _activeModulePage == ShellPage.Locacoes && ModuleGrid.SelectedItem is LocacaoSummary;

        if (_activeModulePage == ShellPage.Locacoes && _moduleItems.Count > 0 && filteredItems.Count == 0)
        {
            ModuleNoticeText.Text = "Nenhuma locação encontrada para a pesquisa atual.";
        }
        else if (_activeModulePage == ShellPage.Locacoes && _moduleItems.Count > 0)
        {
            ModuleNoticeText.Text = GetModuleDefinition(ShellPage.Locacoes).Notice;
        }
    }

    private async Task LoadGenericModuleAsync(ShellPage page)
    {
        _activeModulePage = page;
        var definition = GetModuleDefinition(page);
        ModuleTitleText.Text = definition.Title;
        ModuleSubtitleText.Text = definition.Subtitle;
        ModuleNoticeText.Text = definition.Notice;
        ModulePrimaryActionButton.Content = definition.ActionText;
        ModuleOpenButton.Visibility = page == ShellPage.Locacoes ? Visibility.Visible : Visibility.Collapsed;
        ModuleOpenButton.IsEnabled = false;
        ConfigureModuleGrid(page);

        if (page == ShellPage.Configuracoes)
        {
            ShowSettingsMenuButtons();
            _moduleItems = [];
            ModuleGrid.ItemsSource = Array.Empty<object>();
            return;
        }

        HideSettingsMenuButtons();
        try
        {
            IEnumerable<object> items = page switch
            {
                ShellPage.Locacoes => (await _rentalManagementService.GetLocacoesAsync()).Cast<object>(),
                ShellPage.Financeiro => (await _rentalManagementService.GetLancamentosFinanceirosAsync()).Cast<object>(),
                ShellPage.Boletos => (await _rentalManagementService.GetBoletosAsync()).Cast<object>(),
                ShellPage.NotasFiscais => (await _rentalManagementService.GetNotasFiscaisAsync()).Cast<object>(),
                ShellPage.Documentos => (await _rentalManagementService.GetPessoaDocumentosAsync()).Cast<object>(),
                ShellPage.Relatorios => (await _rentalManagementService.GetImoveisAsync()).Cast<object>(),
                ShellPage.Dimob => (await _rentalManagementService.GetDimobDeclaracoesAsync()).Cast<object>(),
                ShellPage.Manutencoes => (await _rentalManagementService.GetManutencoesAsync()).Cast<object>(),
                ShellPage.Vistorias => (await _rentalManagementService.GetVistoriasAsync()).Cast<object>(),
                _ => []
            };

            _moduleItems = items.ToList();
            ModuleNoticeText.Text = page == ShellPage.Locacoes && _moduleItems.Count == 0
                ? "Nenhuma locação cadastrada."
                : definition.Notice;
            ApplyModuleFilter();
        }
        catch (Exception ex)
        {
            _moduleItems = [];
            ModuleGrid.ItemsSource = Array.Empty<object>();
            ModuleNoticeText.Text = $"Não foi possível carregar este módulo. {ex.Message}";
        }
    }

    private async void ModulePrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeModulePage == ShellPage.Configuracoes)
        {
            MessageBox.Show(this, "Escolha uma das opções de configuração abaixo.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_activeModulePage == ShellPage.Locacoes)
        {
            await ShowCreateLocacaoDialogAsync();
            return;
        }

        var message = _activeModulePage switch
        {
            ShellPage.Boletos => "Integração bancária ainda não configurada.",
            ShellPage.NotasFiscais => "Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual.",
            ShellPage.Dimob => "Exportação oficial DIMOB pendente de confirmação do layout vigente da Receita Federal.",
            ShellPage.Documentos => "Modelos iniciais criados como pendentes de revisão. A redação final deve ser confirmada com o cliente.",
            _ => "CRUD completo deste módulo será implementado em uma próxima etapa."
        };

        MessageBox.Show(this, message, "Monthoya", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task ShowCreateLocacaoDialogAsync()
    {
        IReadOnlyList<ImovelSummary> imoveis;
        IReadOnlyList<PessoaSummary> pessoas;

        try
        {
            imoveis = await _rentalManagementService.GetImoveisAsync();
            pessoas = await _rentalManagementService.GetPessoasAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível carregar os dados para nova locação. {ex.Message}", "Nova locação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (imoveis.Count == 0)
        {
            MessageBox.Show(this, "Cadastre ao menos um imóvel antes de criar uma locação.", "Nova locação", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var pessoasSelecionaveis = pessoas.OrderBy(x => x.Nome).ToList();
        if (pessoasSelecionaveis.Count == 0)
        {
            MessageBox.Show(this, "Cadastre ao menos uma pessoa antes de criar uma locação.", "Nova locação", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Window
        {
            Owner = this,
            Title = "Nova locação",
            Width = 720,
            Height = 680,
            MinWidth = 640,
            MinHeight = 580,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Background
        };

        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(new TextBlock
        {
            Text = "Nova locação",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 14)
        });

        var form = new StackPanel();
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = form
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var imovelBox = CreateComboBox(imoveis, nameof(ImovelSummary.Endereco));
        var proprietarioBox = CreateComboBox(pessoasSelecionaveis, nameof(PessoaSummary.Nome));
        var locatarioBox = CreateComboBox(pessoasSelecionaveis, nameof(PessoaSummary.Nome));
        var tipoBox = CreateComboBox(Enum.GetValues<TipoLocacao>().Cast<object>().ToList(), null);
        tipoBox.SelectedItem = TipoLocacao.Residencial;
        var valorBox = new TextBox { Margin = new Thickness(0, 6, 0, 12) };
        var dataInicioBox = new DatePicker { SelectedDate = DateTime.Today, Margin = new Thickness(0, 6, 0, 12) };
        var dataCobrancaBox = new DatePicker { SelectedDate = DateTime.Today, Margin = new Thickness(0, 6, 0, 12) };
        var diaBaseBox = new TextBox { Text = DateTime.Today.Day.ToString(), Margin = new Thickness(0, 6, 0, 12) };
        var diaVencimentoBox = new TextBox { Text = DateTime.Today.Day.ToString(), Margin = new Thickness(0, 6, 0, 12) };
        var diaRepasseBox = new TextBox { Text = DateTime.Today.Day.ToString(), Margin = new Thickness(0, 6, 0, 12) };
        var antecipadoBox = new CheckBox { Content = "Aluguel antecipado", Margin = new Thickness(0, 6, 0, 12) };
        var observacoesBox = new TextBox
        {
            AcceptsReturn = true,
            Height = 82,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var errorText = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.Firebrick,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        AddLabeledControl(form, "Imóvel", imovelBox);
        AddLabeledControl(form, "Proprietário principal", proprietarioBox);
        AddLabeledControl(form, "Locatário principal", locatarioBox);
        AddLabeledControl(form, "Tipo de locação", tipoBox);
        AddLabeledControl(form, "Valor aluguel inicial", valorBox);
        AddLabeledControl(form, "Data início locação", dataInicioBox);
        AddLabeledControl(form, "Data início cobrança", dataCobrancaBox);
        AddLabeledControl(form, "Dia base", diaBaseBox);
        AddLabeledControl(form, "Dia vencimento locatário", diaVencimentoBox);
        AddLabeledControl(form, "Dia repasse proprietário", diaRepasseBox);
        form.Children.Add(antecipadoBox);
        AddLabeledControl(form, "Observações internas", observacoesBox);
        form.Children.Add(errorText);

        dataInicioBox.SelectedDateChanged += (_, _) =>
        {
            if (!dataCobrancaBox.SelectedDate.HasValue)
            {
                dataCobrancaBox.SelectedDate = dataInicioBox.SelectedDate;
            }
        };
        dataCobrancaBox.SelectedDateChanged += (_, _) =>
        {
            if (dataCobrancaBox.SelectedDate is not DateTime selected)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(diaBaseBox.Text)) diaBaseBox.Text = selected.Day.ToString();
            if (string.IsNullOrWhiteSpace(diaVencimentoBox.Text)) diaVencimentoBox.Text = diaBaseBox.Text;
            if (string.IsNullOrWhiteSpace(diaRepasseBox.Text)) diaRepasseBox.Text = diaVencimentoBox.Text;
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        Grid.SetRow(buttons, 2);
        var saveButton = new Button
        {
            Content = "Salvar locação",
            Style = (Style)FindResource("PrimaryButtonSmall"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("SecondaryButton")
        };
        cancelButton.Click += (_, _) => dialog.Close();
        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        root.Children.Add(buttons);
        dialog.Content = root;

        saveButton.Click += async (_, _) =>
        {
            errorText.Text = string.Empty;
            saveButton.IsEnabled = false;
            try
            {
                if (imovelBox.SelectedItem is not ImovelSummary imovel)
                {
                    throw new InvalidOperationException("Selecione o imóvel.");
                }

                if (proprietarioBox.SelectedItem is not PessoaSummary proprietario)
                {
                    throw new InvalidOperationException("Selecione o proprietário.");
                }

                if (locatarioBox.SelectedItem is not PessoaSummary locatario)
                {
                    throw new InvalidOperationException("Selecione o locatário.");
                }

                var valor = ParseNullableDecimal(valorBox.Text)
                    ?? throw new InvalidOperationException("Informe o valor inicial do aluguel.");

                var request = new CreateLocacaoRequest(
                    ImovelId: imovel.Id,
                    Partes:
                    [
                        new LocacaoParteRequest(proprietario.Id, TipoParteLocacao.Proprietario, IsPrincipal: true, PercentualParticipacao: 100m, RecebeRepasse: true),
                        new LocacaoParteRequest(locatario.Id, TipoParteLocacao.Locatario, IsPrincipal: true, RecebeCobranca: true)
                    ],
                    TipoLocacao: tipoBox.SelectedItem is TipoLocacao tipo ? tipo : TipoLocacao.Residencial,
                    Status: LocacaoStatus.Rascunho,
                    DataInicioLocacao: ToLocacaoDateOnly(dataInicioBox.SelectedDate),
                    DataInicioCobranca: ToLocacaoDateOnly(dataCobrancaBox.SelectedDate),
                    DiaBase: ParseRequiredDay(diaBaseBox.Text, "Dia base"),
                    DiaVencimentoLocatario: ParseRequiredDay(diaVencimentoBox.Text, "Dia vencimento locatário"),
                    DiaRepasseProprietario: ParseRequiredDay(diaRepasseBox.Text, "Dia repasse proprietário"),
                    ValorAluguelInicial: valor,
                    AluguelAntecipado: antecipadoBox.IsChecked == true,
                    ObservacoesInternas: string.IsNullOrWhiteSpace(observacoesBox.Text) ? null : observacoesBox.Text.Trim());

                await _rentalManagementService.CreateLocacaoAsync(request);
                dialog.Close();
                await LoadGenericModuleAsync(ShellPage.Locacoes);
                MessageBox.Show(this, "Locação criada como rascunho.", "Nova locação", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                errorText.Text = ex.Message;
                saveButton.IsEnabled = true;
            }
        };

        dialog.ShowDialog();
    }

    private async void ModuleReloadButton_Click(object sender, RoutedEventArgs e) =>
        await LoadGenericModuleAsync(_activeModulePage);

    private void ModuleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ModuleOpenButton.IsEnabled = _activeModulePage == ShellPage.Locacoes && ModuleGrid.SelectedItem is LocacaoSummary;

    private async void ModuleOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeModulePage != ShellPage.Locacoes || ModuleGrid.SelectedItem is not LocacaoSummary locacao)
        {
            return;
        }

        try
        {
            var details = await _rentalManagementService.GetLocacaoAsync(locacao.Id);
            var summary = details.Summary;
            MessageBox.Show(
                this,
                $"Locação: {summary.Codigo ?? "-"}\nStatus: {summary.Status}\nImóvel: {summary.ImovelResumo}\nLocatário: {summary.LocatarioPrincipalNome}\nProprietário: {summary.ProprietarioPrincipalNome}\n\nO formulário de edição completo será implementado em uma próxima etapa.",
                "Locação",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível abrir a locação. {ex.Message}", "Locação", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ConfigureModuleGrid(ShellPage page)
    {
        ModuleGrid.Columns.Clear();
        ModuleGrid.AutoGenerateColumns = page != ShellPage.Locacoes;

        if (page != ShellPage.Locacoes)
        {
            return;
        }

        AddModuleTextColumn("Código", "Codigo", 0.7);
        AddModuleTextColumn("Status", "Status", 1);
        AddModuleTextColumn("Imóvel", "ImovelResumo", 1.8);
        AddModuleTextColumn("Locatário", "LocatarioPrincipalNome", 1.3);
        AddModuleTextColumn("Proprietário", "ProprietarioPrincipalNome", 1.3);
        AddModuleTextColumn("Aluguel", "ValorAluguelAtual", 0.9, "R$ {0:N2}");
        AddModuleTextColumn("Venc.", "DiaVencimentoLocatario", 0.55);
        AddModuleTextColumn("Início", "DataInicioLocacao", 0.8, "dd/MM/yyyy");
        AddModuleTextColumn("Fim previsto", "DataFimPrevista", 0.9, "dd/MM/yyyy");
        AddModuleTextColumn("Alertas", "AlertasTexto", 1.4);
    }

    private void AddModuleTextColumn(string header, string bindingPath, double width, string? stringFormat = null)
    {
        ModuleGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(bindingPath) { StringFormat = stringFormat },
            Width = new DataGridLength(width, DataGridLengthUnitType.Star)
        });
    }

    private ComboBox CreateComboBox(IEnumerable<object> items, string? displayMemberPath)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = items,
            Margin = new Thickness(0, 6, 0, 12)
        };

        if (!string.IsNullOrWhiteSpace(displayMemberPath))
        {
            comboBox.DisplayMemberPath = displayMemberPath;
        }

        comboBox.SelectedIndex = comboBox.Items.Count > 0 ? 0 : -1;
        return comboBox;
    }

    private static void AddLabeledControl(Panel panel, string label, Control control)
    {
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(control);
    }

    private static DateOnly? ToLocacaoDateOnly(DateTime? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static int ParseRequiredDay(string? value, string label)
    {
        if (!int.TryParse(value, out var day) || day is < 1 or > 31)
        {
            throw new InvalidOperationException($"{label} deve ficar entre 1 e 31.");
        }

        return day;
    }

    private ModulePageState CaptureModulePageState() =>
        new(ModuleSearchBox.Text, TryGetItemId(ModuleGrid.SelectedItem));

    private Task RestoreModulePageStateAsync(ModulePageState state)
    {
        ModuleSearchBox.Text = state.SearchText;
        ApplyModuleFilter();
        RestoreDataGridSelection(ModuleGrid, state.SelectedItemId);
        return Task.CompletedTask;
    }

    private sealed record ModulePageState(string SearchText, Guid? SelectedItemId) : IShellPageState
    {
        public static ModulePageState Default { get; } = new("", null);
    }
}

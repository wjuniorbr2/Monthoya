using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task ShowCreateLocacaoInlineAsync()
    {
        IReadOnlyList<ImovelSummary> imoveis;
        IReadOnlyList<PessoaSummary> pessoas;
        IReadOnlyList<LocacaoSummary> locacoes;

        try
        {
            imoveis = await _rentalManagementService.GetImoveisAsync();
            pessoas = await _rentalManagementService.GetPessoasAsync();
            locacoes = await _rentalManagementService.GetLocacoesAsync();
        }
        catch (Exception ex)
        {
            SetModuleNotice($"Não foi possível carregar os dados para nova locação. {ex.Message}");
            return;
        }

        var imoveisIndisponiveis = locacoes
            .Where(IsLocacaoBloqueiaNovaLocacao)
            .Select(x => x.ImovelId)
            .ToHashSet();
        imoveis = imoveis
            .Where(x => IsImovelDisponivelParaNovaLocacao(x, imoveisIndisponiveis))
            .ToList();

        if (imoveis.Count == 0)
        {
            SetModuleNotice("Não há imóveis disponíveis para nova locação. Use imóveis com finalidade Locação/Ambos, status Disponível e sem locação em aberto.");
            return;
        }

        var pessoasSelecionaveis = pessoas.OrderBy(x => x.Nome).ToList();
        if (pessoasSelecionaveis.Count == 0)
        {
            SetModuleNotice("Cadastre ao menos uma pessoa antes de criar uma locação.");
            return;
        }

        ModuleGrid.SelectedItem = null;
        ModuleDetailsBorder.Visibility = Visibility.Visible;

        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = "Nova locação",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        root.Children.Add(new TextBlock
        {
            Text = "Cadastro básico da locação ativa. O proprietário vem do imóvel selecionado. Informe aqui locatários, fiadores e dados básicos da locação.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });

        var form = new WrapPanel();
        root.Children.Add(form);

        var errorText = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.Firebrick,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var codigoBox = new TextBox
        {
            Text = await GetNextAvailableLocacaoCodeAsync(),
            Margin = new Thickness(0, 6, 0, 12)
        };
        var imovelBox = CreateComboBox(imoveis, nameof(ImovelSummary.Endereco));
        var imovelProprietarioBox = new TextBox
        {
            Text = "Selecione um imóvel.",
            IsReadOnly = true,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var tipoBox = CreateComboBox(Enum.GetValues<TipoLocacao>().Cast<object>().ToList(), null);
        tipoBox.SelectedItem = TipoLocacao.Residencial;
        var valorBox = new TextBox { Margin = new Thickness(0, 6, 0, 12) };
        var dataInicioBox = new DatePicker { SelectedDate = DateTime.Today, Margin = new Thickness(0, 6, 0, 12) };
        var dataCobrancaBox = new DatePicker { SelectedDate = DateTime.Today, Margin = new Thickness(0, 6, 0, 12) };
        var diaBaseBox = new TextBox { Text = DateTime.Today.Day.ToString(), Margin = new Thickness(0, 6, 0, 12) };
        var diaVencimentoBox = new TextBox { Text = DateTime.Today.Day.ToString(), Margin = new Thickness(0, 6, 0, 12) };
        var diaRepasseBox = new TextBox { Text = DateTime.Today.Day.ToString(), Margin = new Thickness(0, 6, 0, 12) };
        var antecipadoBox = new CheckBox { Content = "Aluguel antecipado", Margin = new Thickness(0, 28, 0, 12) };
        var observacoesBox = new TextBox
        {
            AcceptsReturn = true,
            Height = 70,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 12)
        };

        ConfigureLocacaoCodeTextBox(codigoBox, errorText);
        ConfigureLocacaoDecimalTextBox(valorBox, errorText);
        ConfigureLocacaoDatePicker(dataInicioBox, errorText);
        ConfigureLocacaoDatePicker(dataCobrancaBox, errorText);
        ConfigureLocacaoDayTextBox(diaBaseBox, errorText);
        ConfigureLocacaoDayTextBox(diaVencimentoBox, errorText);
        ConfigureLocacaoDayTextBox(diaRepasseBox, errorText);

        imovelBox.SelectionChanged += (_, _) =>
        {
            imovelProprietarioBox.Text = imovelBox.SelectedItem is ImovelSummary selectedImovel
                ? selectedImovel.Proprietario
                : "Selecione um imóvel.";
        };

        AddLabeledControl(form, "Código", codigoBox, 120);
        AddLabeledControl(form, "Imóvel", imovelBox, 320);
        AddLabeledControl(form, "Proprietário(s) do imóvel", imovelProprietarioBox, 260);
        AddLabeledControl(form, "Tipo de locação", tipoBox, 180);
        AddLabeledControl(form, "Valor aluguel inicial", valorBox, 180);
        AddLabeledControl(form, "Data início locação", dataInicioBox, 180);
        AddLabeledControl(form, "Data início cobrança", dataCobrancaBox, 180);
        AddLabeledControl(form, "Dia base", diaBaseBox, 120);
        AddLabeledControl(form, "Dia vencimento locatário", diaVencimentoBox, 170);
        AddLabeledControl(form, "Dia repasse proprietário", diaRepasseBox, 170);
        AddLabeledControl(form, string.Empty, antecipadoBox, 190);
        AddLabeledControl(form, "Observações internas", observacoesBox, 520);

        var parteRows = new List<LocacaoParteEditorRow>();
        root.Children.Add(CreateLocacaoPartesEditor(pessoasSelecionaveis, errorText, parteRows));
        root.Children.Add(errorText);

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
            Margin = new Thickness(0, 8, 0, 0)
        };
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
        cancelButton.Click += (_, _) => ShowLocacaoSelectionDetails();
        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        root.Children.Add(buttons);
        ModuleDetailsHost.Content = root;

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

                TryApplyBrazilianDate(dataInicioBox, message => errorText.Text = message);
                TryApplyBrazilianDate(dataCobrancaBox, message => errorText.Text = message);
                if (!string.IsNullOrWhiteSpace(errorText.Text))
                {
                    throw new InvalidOperationException(errorText.Text);
                }

                var valor = ParseNullableDecimal(valorBox.Text)
                    ?? throw new InvalidOperationException("Informe o valor inicial do aluguel.");

                var imovelDetails = await _rentalManagementService.GetImovelAsync(imovel.Id)
                    ?? throw new InvalidOperationException("Imóvel selecionado não encontrado.");

                var partes = BuildLocacaoParteRequests(parteRows).ToList();
                partes.Insert(0, new LocacaoParteRequest(
                    imovelDetails.Dados.ProprietarioId,
                    TipoParteLocacao.Proprietario,
                    IsPrincipal: true,
                    PercentualParticipacao: 100m,
                    RecebeRepasse: true));

                var request = new CreateLocacaoRequest(
                    ImovelId: imovel.Id,
                    Partes: partes,
                    Codigo: string.IsNullOrWhiteSpace(codigoBox.Text) ? null : codigoBox.Text.Trim(),
                    TipoLocacao: tipoBox.SelectedItem is TipoLocacao tipo ? tipo : TipoLocacao.Residencial,
                    Status: LocacaoStatus.Ativa,
                    DataInicioLocacao: ToLocacaoDateOnly(dataInicioBox.SelectedDate),
                    DataInicioCobranca: ToLocacaoDateOnly(dataCobrancaBox.SelectedDate),
                    DiaBase: ParseRequiredDay(diaBaseBox.Text, "Dia base"),
                    DiaVencimentoLocatario: ParseRequiredDay(diaVencimentoBox.Text, "Dia vencimento locatário"),
                    DiaRepasseProprietario: ParseRequiredDay(diaRepasseBox.Text, "Dia repasse proprietário"),
                    ValorAluguelInicial: valor,
                    AluguelAntecipado: antecipadoBox.IsChecked == true,
                    ObservacoesInternas: string.IsNullOrWhiteSpace(observacoesBox.Text) ? null : observacoesBox.Text.Trim());

                var created = await _rentalManagementService.CreateLocacaoAsync(request);
                await LoadGenericModuleAsync(ShellPage.Locacoes);
                RestoreDataGridSelection(ModuleGrid, created.Summary.Id);
                ShowLocacaoDetails(created.Summary, "Locação criada como ativa.");
                MessageBox.Show(this, "Locação criada como ativa.", "Nova locação", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                errorText.Text = ex.Message;
                saveButton.IsEnabled = true;
            }
        };
    }

    private static bool IsLocacaoBloqueiaNovaLocacao(LocacaoSummary locacao) =>
        !locacao.Status.Contains("Cancelada", StringComparison.OrdinalIgnoreCase)
        && !locacao.Status.Contains("Encerrada", StringComparison.OrdinalIgnoreCase);

    private static bool IsImovelDisponivelParaNovaLocacao(ImovelSummary imovel, ISet<Guid> imoveisIndisponiveis)
    {
        if (imoveisIndisponiveis.Contains(imovel.Id))
        {
            return false;
        }

        var finalidade = imovel.Finalidade ?? string.Empty;
        var status = imovel.Status ?? string.Empty;
        var aceitaLocacao = finalidade.Contains("Locação", StringComparison.OrdinalIgnoreCase)
            || finalidade.Contains("Locacao", StringComparison.OrdinalIgnoreCase)
            || finalidade.Contains("Ambos", StringComparison.OrdinalIgnoreCase);
        var disponivel = status.Contains("Dispon", StringComparison.OrdinalIgnoreCase);

        return aceitaLocacao && disponivel;
    }
}

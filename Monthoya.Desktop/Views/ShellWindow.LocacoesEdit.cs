using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task ShowEditLocacaoInlineAsync(LocacaoSummary locacao)
    {
        LocacaoDetails details;
        IReadOnlyList<ImovelSummary> imoveis;
        IReadOnlyList<PessoaSummary> pessoas;

        try
        {
            details = await _rentalManagementService.GetLocacaoAsync(locacao.Id);
            imoveis = await _rentalManagementService.GetImoveisAsync();
            pessoas = await _rentalManagementService.GetPessoasAsync();
        }
        catch (Exception ex)
        {
            ShowLocacaoDetails(locacao, $"Não foi possível carregar a locação para edição. {ex.Message}");
            return;
        }

        var dados = details.Dados;
        var pessoasSelecionaveis = pessoas.OrderBy(x => x.Nome).ToList();

        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = $"Editar locação {details.Summary.Codigo ?? "-"}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        root.Children.Add(new TextBlock
        {
            Text = "Edição básica. Imóvel, proprietário e locatário ficam travados após a criação; mudança dessas partes deve ser feita em fluxo próprio, com histórico.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });

        var form = new WrapPanel();
        root.Children.Add(form);

        var proprietarioAtual = FindPrincipalLocacaoParte(details, TipoParteLocacao.Proprietario);
        var locatarioAtual = FindPrincipalLocacaoParte(details, TipoParteLocacao.Locatario);

        var imovelAtual = imoveis.FirstOrDefault(x => x.Id == dados.ImovelId)
            ?? throw new InvalidOperationException("Imóvel da locação não encontrado.");

        var imovelBox = CreateComboBox(imoveis, nameof(ImovelSummary.Endereco));
        imovelBox.SelectedItem = imovelAtual;
        imovelBox.IsEnabled = false;

        var proprietarioPessoa = proprietarioAtual is null
            ? null
            : pessoasSelecionaveis.FirstOrDefault(x => x.Id == proprietarioAtual.PessoaId);
        var locatarioPessoa = locatarioAtual is null
            ? null
            : pessoasSelecionaveis.FirstOrDefault(x => x.Id == locatarioAtual.PessoaId);

        if (proprietarioPessoa is null)
        {
            throw new InvalidOperationException("Proprietário principal da locação não encontrado.");
        }

        if (locatarioPessoa is null)
        {
            throw new InvalidOperationException("Locatário principal da locação não encontrado.");
        }

        var proprietarioBox = CreateComboBox(pessoasSelecionaveis, nameof(PessoaSummary.Nome));
        proprietarioBox.SelectedItem = proprietarioPessoa;
        proprietarioBox.IsEnabled = false;

        var locatarioBox = CreateComboBox(pessoasSelecionaveis, nameof(PessoaSummary.Nome));
        locatarioBox.SelectedItem = locatarioPessoa;
        locatarioBox.IsEnabled = false;

        var tipoBox = CreateComboBox(Enum.GetValues<TipoLocacao>().Cast<object>().ToList(), null);
        tipoBox.SelectedItem = dados.TipoLocacao;

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var codigoBox = new TextBox
        {
            Text = !string.IsNullOrWhiteSpace(dados.Codigo)
                ? dados.Codigo
                : !string.IsNullOrWhiteSpace(details.Summary.Codigo)
                    ? details.Summary.Codigo
                    : await GetNextAvailableLocacaoCodeAsync(details.Summary.Id),
            Margin = new Thickness(0, 6, 0, 12)
        };
        var valorAtual = dados.ValorAluguelAtual ?? details.Summary.ValorAluguelAtual ?? dados.ValorAluguelInicial;
        var valorBox = new TextBox
        {
            Text = valorAtual > 0 ? valorAtual.ToString("N2", culture) : string.Empty,
            Margin = new Thickness(0, 6, 0, 12)
        };

        var dataInicioBox = new DatePicker
        {
            SelectedDate = ToLocacaoDateTime(dados.DataInicioLocacao),
            Margin = new Thickness(0, 6, 0, 12)
        };
        var dataCobrancaBox = new DatePicker
        {
            SelectedDate = ToLocacaoDateTime(dados.DataInicioCobranca),
            Margin = new Thickness(0, 6, 0, 12)
        };
        var diaBaseBox = new TextBox
        {
            Text = dados.DiaBase?.ToString() ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var diaVencimentoBox = new TextBox
        {
            Text = dados.DiaVencimentoLocatario?.ToString() ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var diaRepasseBox = new TextBox
        {
            Text = dados.DiaRepasseProprietario?.ToString() ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var antecipadoBox = new CheckBox
        {
            Content = "Aluguel antecipado",
            IsChecked = dados.AluguelAntecipado,
            Margin = new Thickness(0, 28, 0, 12)
        };
        var observacoesBox = new TextBox
        {
            Text = dados.ObservacoesInternas ?? string.Empty,
            AcceptsReturn = true,
            Height = 70,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 12)
        };
        var errorText = new TextBlock
        {
            Foreground = System.Windows.Media.Brushes.Firebrick,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        ConfigureLocacaoCodeTextBox(codigoBox, errorText);
        ConfigureLocacaoDecimalTextBox(valorBox, errorText);
        ConfigureLocacaoDatePicker(dataInicioBox, errorText);
        ConfigureLocacaoDatePicker(dataCobrancaBox, errorText);
        ConfigureLocacaoDayTextBox(diaBaseBox, errorText);
        ConfigureLocacaoDayTextBox(diaVencimentoBox, errorText);
        ConfigureLocacaoDayTextBox(diaRepasseBox, errorText);

        AddLabeledControl(form, "Código", codigoBox, 120);
        AddLabeledControl(form, "Imóvel (travado)", imovelBox, 320);
        AddLabeledControl(form, "Proprietário principal (travado)", proprietarioBox, 260);
        AddLabeledControl(form, "Locatário principal (travado)", locatarioBox, 260);
        AddLabeledControl(form, "Tipo de locação", tipoBox, 180);
        AddLabeledControl(form, "Aluguel atual", valorBox, 180);
        AddLabeledControl(form, "Data início locação", dataInicioBox, 180);
        AddLabeledControl(form, "Data início cobrança", dataCobrancaBox, 180);
        AddLabeledControl(form, "Dia base", diaBaseBox, 120);
        AddLabeledControl(form, "Dia vencimento locatário", diaVencimentoBox, 170);
        AddLabeledControl(form, "Dia repasse proprietário", diaRepasseBox, 170);
        AddLabeledControl(form, string.Empty, antecipadoBox, 190);
        AddLabeledControl(form, "Observações internas", observacoesBox, 520);
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
            Content = "Salvar alterações",
            Style = (Style)FindResource("PrimaryButtonSmall"),
            Margin = new Thickness(0, 0, 8, 0)
        };
        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("SecondaryButton")
        };
        cancelButton.Click += (_, _) => ShowLocacaoDetails(details.Summary);
        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);
        root.Children.Add(buttons);

        ModuleDetailsBorder.Visibility = Visibility.Visible;
        ModuleDetailsHost.Content = root;

        saveButton.Click += async (_, _) =>
        {
            errorText.Text = string.Empty;
            saveButton.IsEnabled = false;

            try
            {
                var imovel = imovelAtual;

                TryApplyBrazilianDate(dataInicioBox, message => errorText.Text = message);
                TryApplyBrazilianDate(dataCobrancaBox, message => errorText.Text = message);
                if (!string.IsNullOrWhiteSpace(errorText.Text))
                {
                    throw new InvalidOperationException(errorText.Text);
                }

                ValidateReasonableLocacaoDate(dataInicioBox, "Data início locação");
                ValidateReasonableLocacaoDate(dataCobrancaBox, "Data início cobrança");

                var valor = ParseNullableDecimal(valorBox.Text)
                    ?? throw new InvalidOperationException("Informe o valor do aluguel.");

                var updatedRequest = dados with
                {
                    ImovelId = imovel.Id,
                    Partes = dados.Partes,
                    Codigo = string.IsNullOrWhiteSpace(codigoBox.Text) ? null : codigoBox.Text.Trim(),
                    TipoLocacao = tipoBox.SelectedItem is TipoLocacao tipo ? tipo : dados.TipoLocacao,
                    DataInicioLocacao = ToLocacaoDateOnly(dataInicioBox.SelectedDate),
                    DataInicioCobranca = ToLocacaoDateOnly(dataCobrancaBox.SelectedDate),
                    DiaBase = ParseRequiredDay(diaBaseBox.Text, "Dia base"),
                    DiaVencimentoLocatario = ParseRequiredDay(diaVencimentoBox.Text, "Dia vencimento locatário"),
                    DiaRepasseProprietario = ParseRequiredDay(diaRepasseBox.Text, "Dia repasse proprietário"),
                    ValorAluguelInicial = valor,
                    ValorAluguelAtual = valor,
                    AluguelAntecipado = antecipadoBox.IsChecked == true,
                    ObservacoesInternas = string.IsNullOrWhiteSpace(observacoesBox.Text) ? null : observacoesBox.Text.Trim()
                };

                var updated = await _rentalManagementService.UpdateLocacaoAsync(
                    new UpdateLocacaoRequest(details.Summary.Id, updatedRequest));

                await LoadGenericModuleAsync(ShellPage.Locacoes);
                RestoreDataGridSelection(ModuleGrid, updated.Summary.Id);
                ShowLocacaoDetails(updated.Summary, "Locação atualizada.");
            }
            catch (Exception ex)
            {
                errorText.Text = ex.Message;
                saveButton.IsEnabled = true;
            }
        };
    }

    private static void ValidateReasonableLocacaoDate(DatePicker datePicker, string label)
    {
        if (datePicker.SelectedDate is not DateTime selectedDate)
        {
            throw new InvalidOperationException($"Informe {label.ToLowerInvariant()}.");
        }

        if (selectedDate.Year is < 1900 or > 2100)
        {
            throw new InvalidOperationException($"{label} deve ficar entre 1900 e 2100.");
        }
    }

    private static LocacaoParteSummary? FindPrincipalLocacaoParte(LocacaoDetails details, TipoParteLocacao tipoParte) =>
        details.Partes.FirstOrDefault(x => x.TipoParte == tipoParte && x.IsPrincipal)
        ?? details.Partes.FirstOrDefault(x => x.TipoParte == tipoParte);

    private static DateTime? ToLocacaoDateTime(DateOnly? value) =>
        value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : null;
}

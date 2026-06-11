using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task ShowCreateLocacaoInlineAsync()
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
            SetModuleNotice($"NÃ£o foi possÃ­vel carregar os dados para nova locaÃ§Ã£o. {ex.Message}");
            return;
        }

        if (imoveis.Count == 0)
        {
            SetModuleNotice("Cadastre ao menos um imÃ³vel antes de criar uma locaÃ§Ã£o.");
            return;
        }

        var pessoasSelecionaveis = pessoas.OrderBy(x => x.Nome).ToList();
        if (pessoasSelecionaveis.Count == 0)
        {
            SetModuleNotice("Cadastre ao menos uma pessoa antes de criar uma locaÃ§Ã£o.");
            return;
        }

        ModuleGrid.SelectedItem = null;
        ModuleDetailsBorder.Visibility = Visibility.Visible;

        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = "Nova locaÃ§Ã£o",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        root.Children.Add(new TextBlock
        {
            Text = "Cadastro bÃ¡sico em rascunho. EdiÃ§Ã£o completa, encargos e geraÃ§Ã£o de contrato ficam para uma prÃ³xima etapa.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });

        var form = new WrapPanel();
        root.Children.Add(form);

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
        var antecipadoBox = new CheckBox { Content = "Aluguel antecipado", Margin = new Thickness(0, 28, 0, 12) };
        var observacoesBox = new TextBox
        {
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

        ConfigureLocacaoDecimalTextBox(valorBox, errorText);
        ConfigureLocacaoDatePicker(dataInicioBox, errorText);
        ConfigureLocacaoDatePicker(dataCobrancaBox, errorText);
        ConfigureLocacaoDayTextBox(diaBaseBox, errorText);
        ConfigureLocacaoDayTextBox(diaVencimentoBox, errorText);
        ConfigureLocacaoDayTextBox(diaRepasseBox, errorText);

        AddLabeledControl(form, "ImÃ³vel", imovelBox, 320);
        AddLabeledControl(form, "ProprietÃ¡rio principal", proprietarioBox, 260);
        AddLabeledControl(form, "LocatÃ¡rio principal", locatarioBox, 260);
        AddLabeledControl(form, "Tipo de locaÃ§Ã£o", tipoBox, 180);
        AddLabeledControl(form, "Valor aluguel inicial", valorBox, 180);
        AddLabeledControl(form, "Data inÃ­cio locaÃ§Ã£o", dataInicioBox, 180);
        AddLabeledControl(form, "Data inÃ­cio cobranÃ§a", dataCobrancaBox, 180);
        AddLabeledControl(form, "Dia base", diaBaseBox, 120);
        AddLabeledControl(form, "Dia vencimento locatÃ¡rio", diaVencimentoBox, 170);
        AddLabeledControl(form, "Dia repasse proprietÃ¡rio", diaRepasseBox, 170);
        AddLabeledControl(form, string.Empty, antecipadoBox, 190);
        AddLabeledControl(form, "ObservaÃ§Ãµes internas", observacoesBox, 520);
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
            Content = "Salvar locaÃ§Ã£o",
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
                    throw new InvalidOperationException("Selecione o imÃ³vel.");
                }

                if (proprietarioBox.SelectedItem is not PessoaSummary proprietario)
                {
                    throw new InvalidOperationException("Selecione o proprietÃ¡rio.");
                }

                if (locatarioBox.SelectedItem is not PessoaSummary locatario)
                {
                    throw new InvalidOperationException("Selecione o locatÃ¡rio.");
                }

                if (proprietario.Id == locatario.Id)
                {
                    throw new InvalidOperationException("A mesma pessoa nÃ£o pode ser proprietÃ¡rio e locatÃ¡rio na mesma locaÃ§Ã£o.");
                }

                TryApplyBrazilianDate(dataInicioBox, message => errorText.Text = message);
                TryApplyBrazilianDate(dataCobrancaBox, message => errorText.Text = message);
                if (!string.IsNullOrWhiteSpace(errorText.Text))
                {
                    throw new InvalidOperationException(errorText.Text);
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
                    DiaVencimentoLocatario: ParseRequiredDay(diaVencimentoBox.Text, "Dia vencimento locatÃ¡rio"),
                    DiaRepasseProprietario: ParseRequiredDay(diaRepasseBox.Text, "Dia repasse proprietÃ¡rio"),
                    ValorAluguelInicial: valor,
                    AluguelAntecipado: antecipadoBox.IsChecked == true,
                    ObservacoesInternas: string.IsNullOrWhiteSpace(observacoesBox.Text) ? null : observacoesBox.Text.Trim());

                var created = await _rentalManagementService.CreateLocacaoAsync(request);
                await LoadGenericModuleAsync(ShellPage.Locacoes);
                RestoreDataGridSelection(ModuleGrid, created.Summary.Id);
                ShowLocacaoDetails(created.Summary, "LocaÃ§Ã£o criada como rascunho.");
                MessageBox.Show(this, "LocaÃ§Ã£o criada como rascunho.", "Nova locaÃ§Ã£o", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                errorText.Text = ex.Message;
                saveButton.IsEnabled = true;
            }
        };
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

    private static void AddLabeledControl(Panel panel, string label, Control control, double width)
    {
        var fieldPanel = new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 14, 12)
        };

        if (!string.IsNullOrWhiteSpace(label))
        {
            fieldPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold
            });
        }

        fieldPanel.Children.Add(control);
        panel.Children.Add(fieldPanel);
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

    private void ShowLocacaoSelectionDetails()
    {
        if (ModuleGrid.SelectedItem is LocacaoSummary locacao)
        {
            ShowLocacaoDetails(locacao);
            return;
        }

        if (_activeModulePage == ShellPage.Locacoes)
        {
            ModuleDetailsBorder.Visibility = Visibility.Visible;
            ModuleDetailsHost.Content = new TextBlock
            {
                Text = "Selecione uma locaÃ§Ã£o na lista ou clique em Nova locaÃ§Ã£o.",
                Foreground = System.Windows.Media.Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }

    private void ShowLocacaoDetails(LocacaoSummary locacao, string? statusMessage = null)
    {
        ModuleDetailsBorder.Visibility = Visibility.Visible;

        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = $"LocaÃ§Ã£o {locacao.Codigo ?? "-"}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            root.Children.Add(new TextBlock
            {
                Text = statusMessage,
                Foreground = System.Windows.Media.Brushes.SeaGreen,
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        var details = new WrapPanel();
        root.Children.Add(details);
        AddDetailText(details, "Status", locacao.Status);
        AddDetailText(details, "Tipo", locacao.TipoLocacao);
        AddDetailText(details, "ImÃ³vel", locacao.ImovelResumo, 360);
        AddDetailText(details, "LocatÃ¡rio principal", locacao.LocatarioPrincipalNome);
        AddDetailText(details, "ProprietÃ¡rio principal", locacao.ProprietarioPrincipalNome);
        AddDetailText(details, "Aluguel atual", locacao.ValorAluguelAtual?.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) ?? "-");
        AddDetailText(details, "Vencimento locatÃ¡rio", locacao.DiaVencimentoLocatario?.ToString() ?? "-");
        AddDetailText(details, "InÃ­cio", locacao.DataInicioLocacao?.ToString("dd/MM/yyyy") ?? "-");
        AddDetailText(details, "Fim previsto", locacao.DataFimPrevista?.ToString("dd/MM/yyyy") ?? "-");
        AddDetailText(details, "Alertas", string.IsNullOrWhiteSpace(locacao.AlertasTexto) ? "-" : locacao.AlertasTexto, 360);

        root.Children.Add(new TextBlock
        {
            Text = "EdiÃ§Ã£o completa da locaÃ§Ã£o serÃ¡ implementada em uma prÃ³xima etapa.",
            Foreground = System.Windows.Media.Brushes.DimGray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        });

        ModuleDetailsHost.Content = root;
    }

    private static void AddDetailText(Panel panel, string label, string value, double width = 220)
    {
        var field = new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 18, 12)
        };
        field.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
        field.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(value) ? "-" : value, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(field);
    }

    private void ConfigureLocacaoDecimalTextBox(TextBox textBox, TextBlock errorText)
    {
        textBox.PreviewTextInput += (_, e) =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[\d,.]+$"))
            {
                return;
            }

            e.Handled = true;
            errorText.Text = "Digite apenas nÃºmeros, vÃ­rgula ou ponto para valores.";
        };
        DataObject.AddPastingHandler(textBox, (_, e) =>
        {
            var text = e.DataObject.GetDataPresent(DataFormats.Text)
                ? e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty
                : string.Empty;
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^[\d,.]+$"))
            {
                return;
            }

            e.CancelCommand();
            errorText.Text = "Cole apenas nÃºmeros, vÃ­rgula ou ponto para valores.";
        });
        textBox.LostKeyboardFocus += (_, _) => FormatDecimalTextBox(textBox);
    }

    private void ConfigureLocacaoDatePicker(DatePicker datePicker, TextBlock errorText)
    {
        datePicker.Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR");
        datePicker.SelectedDateFormat = DatePickerFormat.Short;
        datePicker.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;
            Dispatcher.BeginInvoke(
                () => TryApplyBrazilianDate(datePicker, message => errorText.Text = message),
                System.Windows.Threading.DispatcherPriority.Background);
        };
        datePicker.LostKeyboardFocus += (_, _) => TryApplyBrazilianDate(datePicker, message => errorText.Text = message);
    }

    private static void ConfigureLocacaoDayTextBox(TextBox textBox, TextBlock errorText)
    {
        textBox.PreviewTextInput += (_, e) =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^\d+$"))
            {
                return;
            }

            e.Handled = true;
            errorText.Text = "Dias devem ser informados com nÃºmeros de 1 a 31.";
        };
        textBox.LostKeyboardFocus += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text) && (!int.TryParse(textBox.Text, out var day) || day is < 1 or > 31))
            {
                errorText.Text = "Dias devem ficar entre 1 e 31.";
            }
        };
    }
}

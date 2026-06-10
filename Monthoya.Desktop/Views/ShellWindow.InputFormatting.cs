using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Monthoya.Core.Entities;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly HttpClient ViaCepHttpClient = new();
    private readonly Dictionary<string, IReadOnlyList<string>> _citySuggestionsByState = new(StringComparer.OrdinalIgnoreCase);
    private bool _dynamicPessoaInputConfigured;


    private static DateOnly? ToDateOnly(DateTime? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static DateTime? ToDateTime(DateOnly? value) =>
        value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : null;

    private void ConfigurePessoaInputBehavior()
    {
        foreach (var textBox in new[]
        {
            PessoaDocumentoBox, PessoaConjugeCpfBox, PessoaResponsavelCpfBox,
            PessoaTelefoneBox, PessoaConjugeTelefoneBox, PessoaResponsavelTelefoneBox, PessoaTelefoneEmpresaTrabalhoBox, PessoaResponsavelTelefoneEmpresaTrabalhoBox,
            ImovelChaveTelefoneBox, ChavesRetiradoPorTelefoneBox,
            PessoaCepBox, PessoaEmpresaCepBox, PessoaResponsavelCepBox, ImovelCepBox,
            PessoaRgBox, PessoaConjugeRgBox, PessoaResponsavelRgBox
        })
        {
            textBox.PreviewTextInput += NumericMaskedTextBox_PreviewTextInput;
            DataObject.AddPastingHandler(textBox, NumericMaskedTextBox_OnPaste);
        }

        PessoaDocumentoBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaDocumentoBox, FormatPessoaDocumento);
        PessoaConjugeCpfBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaConjugeCpfBox, FormatCpf);
        PessoaResponsavelCpfBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelCpfBox, FormatCpf);
        PessoaTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaTelefoneBox, FormatBrazilPhone);
        PessoaConjugeTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaConjugeTelefoneBox, FormatBrazilPhone);
        PessoaResponsavelTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelTelefoneBox, FormatBrazilPhone);
        PessoaTelefoneEmpresaTrabalhoBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaTelefoneEmpresaTrabalhoBox, FormatBrazilPhone);
        PessoaResponsavelTelefoneEmpresaTrabalhoBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelTelefoneEmpresaTrabalhoBox, FormatBrazilPhone);
        ImovelChaveTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(ImovelChaveTelefoneBox, FormatBrazilPhone);
        ChavesRetiradoPorTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(ChavesRetiradoPorTelefoneBox, FormatBrazilPhone);

        foreach (var textBox in new[] { ImovelValorAluguelBox, ImovelValorVendaBox, ImovelValorCondominioBox, ImovelValorIptuBox, ImovelAreaConstruidaBox, ImovelAreaTerrenoBox })
        {
            textBox.PreviewTextInput += DecimalTextBox_PreviewTextInput;
            DataObject.AddPastingHandler(textBox, DecimalTextBox_OnPaste);
            textBox.LostKeyboardFocus += (_, _) => FormatDecimalTextBox(textBox);
        }
        PessoaCepBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaCepBox, FormatCep);
        PessoaEmpresaCepBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaEmpresaCepBox, FormatCep);
        PessoaResponsavelCepBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelCepBox, FormatCep);
        ImovelCepBox.TextChanged += (_, _) => FormatMaskedTextBox(ImovelCepBox, FormatCep);
        PessoaRgBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaRgBox, FormatRg);
        PessoaConjugeRgBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaConjugeRgBox, FormatRg);
        PessoaResponsavelRgBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelRgBox, FormatRg);
        PessoaEstadoBox.TextChanged += (_, _) => ApplyTextSuggestion(PessoaEstadoBox, BrazilianAddressSuggestions.States);
        PessoaEmpresaEstadoBox.TextChanged += (_, _) => ApplyTextSuggestion(PessoaEmpresaEstadoBox, BrazilianAddressSuggestions.States);
        PessoaResponsavelEstadoBox.TextChanged += (_, _) => ApplyTextSuggestion(PessoaResponsavelEstadoBox, BrazilianAddressSuggestions.States);
        ImovelEstadoBox.TextChanged += (_, _) => ApplyTextSuggestion(ImovelEstadoBox, BrazilianAddressSuggestions.States);
        PessoaCidadeBox.TextChanged += async (_, _) => await ApplyCitySuggestionAsync(PessoaCidadeBox, PessoaEstadoBox);
        PessoaEmpresaCidadeBox.TextChanged += async (_, _) => await ApplyCitySuggestionAsync(PessoaEmpresaCidadeBox, PessoaEmpresaEstadoBox);
        PessoaResponsavelCidadeBox.TextChanged += async (_, _) => await ApplyCitySuggestionAsync(PessoaResponsavelCidadeBox, PessoaResponsavelEstadoBox);
        ImovelCidadeBox.TextChanged += async (_, _) => await ApplyCitySuggestionAsync(ImovelCidadeBox, ImovelEstadoBox);
        PessoaRuaBox.TextChanged += (_, _) => ApplyTextSuggestion(PessoaRuaBox, _streetSuggestions);
        PessoaEmpresaRuaBox.TextChanged += (_, _) => ApplyTextSuggestion(PessoaEmpresaRuaBox, _streetSuggestions);
        PessoaResponsavelRuaBox.TextChanged += (_, _) => ApplyTextSuggestion(PessoaResponsavelRuaBox, _streetSuggestions);
        ImovelRuaBox.TextChanged += (_, _) => ApplyTextSuggestion(ImovelRuaBox, _streetSuggestions);
        foreach (var textBox in new[]
        {
            PessoaEstadoBox, PessoaEmpresaEstadoBox, PessoaResponsavelEstadoBox,
            PessoaCidadeBox, PessoaEmpresaCidadeBox, PessoaResponsavelCidadeBox,
            PessoaRuaBox, PessoaEmpresaRuaBox, PessoaResponsavelRuaBox,
            ImovelEstadoBox, ImovelCidadeBox, ImovelRuaBox
        })
        {
            textBox.PreviewKeyDown += AddressSuggestionTextBox_PreviewKeyDown;
        }

        foreach (var datePicker in new[] { PessoaDataNascimentoBox, PessoaConjugeDataNascimentoBox, PessoaResponsavelDataNascimentoBox, PessoaDocumentoValidadeBox, ChavesPrevisaoBox })
        {
            datePicker.Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR");
            datePicker.SelectedDateFormat = DatePickerFormat.Short;
            datePicker.CalendarOpened += (_, _) =>
            {
                if (datePicker.SelectedDate is null)
                {
                    datePicker.DisplayDate = new DateTime(2000, 1, 1);
                }
            };
            datePicker.PreviewKeyDown += PessoaDatePicker_KeyDown;
            datePicker.KeyDown += PessoaDatePicker_KeyDown;
            datePicker.LostKeyboardFocus += PessoaDatePicker_LostKeyboardFocus;
        }
    }

    private void ConfigureDynamicPessoaInputBehavior()
    {
        if (_dynamicPessoaInputConfigured)
        {
            return;
        }

        _dynamicPessoaInputConfigured = true;
        foreach (var textBox in new[]
        {
            _pessoaCnpjEmpresaTrabalhoBox,
            _pessoaConjugeCnpjEmpresaTrabalhoBox,
            _pessoaInscricaoEstadualBox,
            _pessoaInscricaoMunicipalBox,
            _pessoaConjugeTelefoneEmpresaTrabalhoBox,
            _pessoaTrabalhoCepBox,
            _pessoaConjugeEmpresaCepBox
        }.OfType<TextBox>())
        {
            textBox.PreviewTextInput += NumericMaskedTextBox_PreviewTextInput;
            DataObject.AddPastingHandler(textBox, NumericMaskedTextBox_OnPaste);
        }

        AttachMask(_pessoaCnpjEmpresaTrabalhoBox, FormatCnpj);
        AttachMask(_pessoaConjugeCnpjEmpresaTrabalhoBox, FormatCnpj);
        AttachMask(_pessoaConjugeTelefoneEmpresaTrabalhoBox, FormatBrazilPhone);
        AttachMask(_pessoaTrabalhoCepBox, FormatCep);
        AttachMask(_pessoaConjugeEmpresaCepBox, FormatCep);

        foreach (var datePicker in new[] { _pessoaDataAberturaBox }.OfType<DatePicker>())
        {
            datePicker.Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR");
            datePicker.SelectedDateFormat = DatePickerFormat.Short;
            datePicker.CalendarOpened += (_, _) =>
            {
                if (datePicker.SelectedDate is null)
                {
                    datePicker.DisplayDate = new DateTime(2000, 1, 1);
                }
            };
            datePicker.PreviewKeyDown += PessoaDatePicker_KeyDown;
            datePicker.KeyDown += PessoaDatePicker_KeyDown;
            datePicker.LostKeyboardFocus += PessoaDatePicker_LostKeyboardFocus;
        }

        AttachCepLookup(PessoaCepBox, PessoaRuaBox, PessoaComplementoBox, PessoaBairroBox, PessoaCidadeBox, PessoaEstadoBox);
        AttachCepLookup(PessoaEmpresaCepBox, PessoaEmpresaRuaBox, PessoaEmpresaComplementoBox, PessoaEmpresaBairroBox, PessoaEmpresaCidadeBox, PessoaEmpresaEstadoBox);
        AttachCepLookup(PessoaResponsavelCepBox, PessoaResponsavelRuaBox, PessoaResponsavelComplementoBox, PessoaResponsavelBairroBox, PessoaResponsavelCidadeBox, PessoaResponsavelEstadoBox);
        AttachCepLookup(_pessoaTrabalhoCepBox, _pessoaTrabalhoRuaBox, _pessoaTrabalhoComplementoBox, _pessoaTrabalhoBairroBox, _pessoaTrabalhoCidadeBox, _pessoaTrabalhoEstadoBox);
        AttachCepLookup(_pessoaConjugeEmpresaCepBox, _pessoaConjugeEmpresaRuaBox, _pessoaConjugeEmpresaComplementoBox, _pessoaConjugeEmpresaBairroBox, _pessoaConjugeEmpresaCidadeBox, _pessoaConjugeEmpresaEstadoBox);
        AttachCepLookup(ImovelCepBox, ImovelRuaBox, ImovelComplementoBox, ImovelBairroBox, ImovelCidadeBox, ImovelEstadoBox);

        foreach (var ruaBox in new[] { PessoaRuaBox, PessoaEmpresaRuaBox, PessoaResponsavelRuaBox, _pessoaTrabalhoRuaBox, _pessoaConjugeEmpresaRuaBox, ImovelRuaBox }.OfType<TextBox>())
        {
            ruaBox.LostKeyboardFocus += RuaBox_LostKeyboardFocus;
        }

        foreach (var stateBox in new[] { _pessoaTrabalhoEstadoBox, _pessoaConjugeEmpresaEstadoBox }.OfType<TextBox>())
        {
            stateBox.TextChanged += (_, _) => ApplyTextSuggestion(stateBox, BrazilianAddressSuggestions.States);
            stateBox.PreviewKeyDown += AddressSuggestionTextBox_PreviewKeyDown;
        }

        foreach (var cityBox in new[] { _pessoaTrabalhoCidadeBox, _pessoaConjugeEmpresaCidadeBox }.OfType<TextBox>())
        {
            cityBox.PreviewKeyDown += AddressSuggestionTextBox_PreviewKeyDown;
        }
        if (_pessoaTrabalhoCidadeBox is not null && _pessoaTrabalhoEstadoBox is not null)
        {
            _pessoaTrabalhoCidadeBox.TextChanged += async (_, _) => await ApplyCitySuggestionAsync(_pessoaTrabalhoCidadeBox, _pessoaTrabalhoEstadoBox);
        }
        if (_pessoaConjugeEmpresaCidadeBox is not null && _pessoaConjugeEmpresaEstadoBox is not null)
        {
            _pessoaConjugeEmpresaCidadeBox.TextChanged += async (_, _) => await ApplyCitySuggestionAsync(_pessoaConjugeEmpresaCidadeBox, _pessoaConjugeEmpresaEstadoBox);
        }

        foreach (var streetBox in new[] { _pessoaTrabalhoRuaBox, _pessoaConjugeEmpresaRuaBox }.OfType<TextBox>())
        {
            streetBox.TextChanged += (_, _) => ApplyTextSuggestion(streetBox, _streetSuggestions);
            streetBox.PreviewKeyDown += AddressSuggestionTextBox_PreviewKeyDown;
        }
    }

    private void AttachMask(TextBox? textBox, Func<string, string> formatter)
    {
        if (textBox is not null)
        {
            textBox.TextChanged += (_, _) => FormatMaskedTextBox(textBox, formatter);
        }
    }

    private void AttachCepLookup(TextBox? cepBox, TextBox? ruaBox, TextBox? complementoBox, TextBox? bairroBox, TextBox? cidadeBox, TextBox? estadoBox)
    {
        if (cepBox is null || ruaBox is null || complementoBox is null || bairroBox is null || cidadeBox is null || estadoBox is null)
        {
            return;
        }

        cepBox.LostKeyboardFocus += async (_, _) => await TryFillAddressFromCepAsync(cepBox, ruaBox, complementoBox, bairroBox, cidadeBox, estadoBox);
        cepBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await TryFillAddressFromCepAsync(cepBox, ruaBox, complementoBox, bairroBox, cidadeBox, estadoBox);
            }
        };
    }

    private async Task TryFillAddressFromCepAsync(TextBox cepBox, TextBox ruaBox, TextBox complementoBox, TextBox bairroBox, TextBox cidadeBox, TextBox estadoBox)
    {
        var cep = OnlyDigits(cepBox.Text);
        if (cep.Length != 8)
        {
            return;
        }

        try
        {
            var result = await ViaCepHttpClient.GetFromJsonAsync<ViaCepResult>($"https://viacep.com.br/ws/{cep}/json/");
            if (result is null || result.Erro == true)
            {
                PessoaErrorText.Text = "CEP não encontrado no ViaCEP.";
                return;
            }

            FillBlank(ruaBox, result.Logradouro);
            FillBlank(bairroBox, result.Bairro);
            FillBlank(cidadeBox, result.Localidade);
            FillBlank(estadoBox, result.Uf);
            PessoaErrorText.Text = string.Empty;
        }
        catch
        {
            PessoaErrorText.Text = "Não foi possível consultar o CEP no ViaCEP agora.";
        }
    }

    private static void FillBlank(TextBox textBox, string? value)
    {
        if (string.IsNullOrWhiteSpace(textBox.Text) && !string.IsNullOrWhiteSpace(value))
        {
            textBox.Text = value.Trim();
        }
    }

    private void RuaBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox || string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        var match = Regex.Match(textBox.Text, @"(?:,\s*|\s+)(\d+[A-Za-z]?)\s*$");
        if (!match.Success)
        {
            return;
        }

        MessageBox.Show(
            "Digite somente o nome da rua neste campo. O número deve ficar no campo Número.",
            "Número no campo da rua",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        textBox.Focus();
        textBox.Select(match.Groups[1].Index, match.Groups[1].Length);
    }

    private void ApplyTextSuggestion(TextBox textBox, IReadOnlyList<string> suggestions)
    {
        if (_isAutoCompletingText)
        {
            return;
        }

        if (!_isPessoaEditing || string.IsNullOrWhiteSpace(textBox.Text))
        {
            CloseAddressSuggestions(textBox);
            return;
        }

        var typed = textBox.Text;
        var matches = suggestions
            .Where(x => x.StartsWith(typed, StringComparison.CurrentCultureIgnoreCase))
            .Take(8)
            .ToList();
        ShowAddressSuggestions(textBox, matches);
    }

    private async Task ApplyCitySuggestionAsync(TextBox cityBox, TextBox stateBox)
    {
        if (_isAutoCompletingText)
        {
            return;
        }

        var uf = GetStateUf(stateBox.Text);
        if (!_isPessoaEditing || uf is null || string.IsNullOrWhiteSpace(cityBox.Text))
        {
            CloseAddressSuggestions(cityBox);
            return;
        }

        var suggestions = await GetCitiesForStateAsync(uf);
        ApplyTextSuggestion(cityBox, suggestions);
    }

    private async Task<IReadOnlyList<string>> GetCitiesForStateAsync(string uf)
    {
        if (_citySuggestionsByState.TryGetValue(uf, out var cached))
        {
            return cached;
        }

        if (uf.Equals("PR", StringComparison.OrdinalIgnoreCase))
        {
            _citySuggestionsByState[uf] = BrazilianAddressSuggestions.ParanaCities;
            return BrazilianAddressSuggestions.ParanaCities;
        }

        try
        {
            var cities = await ViaCepHttpClient.GetFromJsonAsync<IReadOnlyList<IbgeCity>>(
                $"https://servicodados.ibge.gov.br/api/v1/localidades/estados/{uf}/municipios");
            var names = cities?
                .Select(x => x.Nome)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x)
                .ToList() ?? [];
            _citySuggestionsByState[uf] = names;
            return names;
        }
        catch
        {
            return [];
        }
    }

    private static string? GetStateUf(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return Regex.IsMatch(normalized, "^[A-Z]{2}$") ? normalized : null;
    }

    private void ShowAddressSuggestions(TextBox textBox, IReadOnlyList<string> matches)
    {
        if (matches.Count == 0)
        {
            CloseAddressSuggestions(textBox);
            return;
        }

        EnsureAddressSuggestionPopup();
        _addressSuggestionTarget = textBox;
        _addressSuggestionList!.MinWidth = Math.Max(220, textBox.ActualWidth);
        _addressSuggestionList.ItemsSource = matches;
        _addressSuggestionList.SelectedIndex = 0;
        _addressSuggestionPopup!.PlacementTarget = textBox;
        _addressSuggestionPopup.IsOpen = textBox.IsKeyboardFocusWithin;
    }

    private void EnsureAddressSuggestionPopup()
    {
        if (_addressSuggestionPopup is not null)
        {
            return;
        }

        _addressSuggestionList = new ListBox
        {
            MaxHeight = 180,
            Padding = new Thickness(4),
            BorderThickness = new Thickness(0),
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
        };
        _addressSuggestionList.PreviewMouseLeftButtonUp += (_, _) => ApplySelectedAddressSuggestion();

        _addressSuggestionPopup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            Child = new Border
            {
                Margin = new Thickness(0, 4, 0, 0),
                Padding = new Thickness(2),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = _addressSuggestionList
            }
        };
    }

    private void ApplySelectedAddressSuggestion()
    {
        if (_addressSuggestionList?.SelectedItem is not string selected || _addressSuggestionTarget is null)
        {
            return;
        }

        _isAutoCompletingText = true;
        _addressSuggestionTarget.Text = selected;
        _addressSuggestionTarget.CaretIndex = selected.Length;
        _isAutoCompletingText = false;
        _addressSuggestionPopup!.IsOpen = false;
        _addressSuggestionTarget.Focus();
    }

    private void AddressSuggestionTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_addressSuggestionPopup?.IsOpen != true || _addressSuggestionList is null)
        {
            return;
        }

        if (e.Key == Key.Down)
        {
            e.Handled = true;
            _addressSuggestionList.SelectedIndex = Math.Min(
                _addressSuggestionList.Items.Count - 1,
                _addressSuggestionList.SelectedIndex + 1);
            _addressSuggestionList.ScrollIntoView(_addressSuggestionList.SelectedItem);
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = true;
            _addressSuggestionList.SelectedIndex = Math.Max(0, _addressSuggestionList.SelectedIndex - 1);
            _addressSuggestionList.ScrollIntoView(_addressSuggestionList.SelectedItem);
        }
        else if (e.Key == Key.Enter && _addressSuggestionList.SelectedItem is not null)
        {
            e.Handled = true;
            ApplySelectedAddressSuggestion();
        }
        else if (e.Key == Key.Escape && sender is TextBox textBox)
        {
            e.Handled = true;
            CloseAddressSuggestions(textBox);
        }
    }

    private void CloseAddressSuggestions(TextBox textBox)
    {
        if (_addressSuggestionTarget == textBox && _addressSuggestionPopup is not null)
        {
            _addressSuggestionPopup.IsOpen = false;
        }
    }

    private void NumericMaskedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Regex.IsMatch(e.Text, @"^\d+$"))
        {
            return;
        }

        e.Handled = true;
        PessoaErrorText.Text = "Digite apenas números. Pontos, traços, parênteses e espaços são preenchidos automaticamente pelo sistema.";
    }

    private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Regex.IsMatch(e.Text, @"^[\d,.]+$"))
        {
            return;
        }

        e.Handled = true;
        PessoaErrorText.Text = "Digite apenas números, vírgula ou ponto para valores.";
    }

    private void DecimalTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (Regex.IsMatch(text, @"^[\d,.]+$"))
        {
            return;
        }

        e.CancelCommand();
        PessoaErrorText.Text = "Cole apenas números, vírgula ou ponto para valores.";
    }

    private static void FormatDecimalTextBox(TextBox textBox)
    {
        var value = ParseNullableDecimal(textBox.Text);
        textBox.Text = value?.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty;
    }

    private void NumericMaskedTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (Regex.IsMatch(text, @"^\d+$"))
        {
            return;
        }

        e.CancelCommand();
        PessoaErrorText.Text = "Cole apenas números. Pontos, traços, parênteses e espaços são preenchidos automaticamente pelo sistema.";
    }

    private void FormatMaskedTextBox(TextBox textBox, Func<string, string> formatter)
    {
        if (_isFormattingPessoaText)
        {
            return;
        }

        _isFormattingPessoaText = true;
        var formatted = formatter(OnlyDigits(textBox.Text));
        textBox.Text = formatted;
        textBox.CaretIndex = formatted.Length;
        _isFormattingPessoaText = false;
    }

    private void PessoaDatePicker_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not DatePicker datePicker)
        {
            return;
        }

        e.Handled = true;
        Dispatcher.BeginInvoke(
            () => TryApplyBrazilianDate(datePicker),
            System.Windows.Threading.DispatcherPriority.Background);
    }

    private void PessoaDatePicker_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is DatePicker datePicker)
        {
            TryApplyBrazilianDate(datePicker);
        }
    }

    private bool TryApplyBrazilianDate(DatePicker datePicker)
    {
        var text = datePicker.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var digits = OnlyDigits(text);
        if (digits.Length == 6)
        {
            PessoaErrorText.Text = "Use o ano com quatro números. Exemplo: 25/04/1998.";
            return false;
        }

        if (digits.Length != 8)
        {
            PessoaErrorText.Text = "Data inválida. Use dia/mês/ano no formato brasileiro. Exemplo: 25/04/1998.";
            return false;
        }

        var normalized = $"{digits[..2]}/{digits.Substring(2, 2)}/{digits.Substring(4, 4)}";
        if (!DateTime.TryParseExact(normalized, "dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var parsed))
        {
            PessoaErrorText.Text = "Data inválida. Use dia/mês/ano no formato brasileiro. Exemplo: 25/04/1998.";
            return false;
        }

        datePicker.SelectedDate = parsed;
        datePicker.Text = parsed.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
        PessoaErrorText.Text = string.Empty;
        return true;
    }


}




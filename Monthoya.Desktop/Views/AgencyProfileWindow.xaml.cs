using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class AgencyProfileWindow : Window
{
    private readonly IAgenciaPerfilService _agenciaPerfilService;
    private readonly bool _isRequiredSetup;
    private bool _isFormattingMaskedFields;

    public AgencyProfileWindow(IAgenciaPerfilService agenciaPerfilService)
    {
        InitializeComponent();
        _agenciaPerfilService = agenciaPerfilService;
        Loaded += AgencyProfileWindow_Loaded;
        RegisterMaskedFieldFormatters();
    }

    public AgencyProfileWindow(IAgenciaPerfilService agenciaPerfilService, bool isRequiredSetup) : this(agenciaPerfilService)
    {
        _isRequiredSetup = isRequiredSetup;
        CancelButton.Visibility = isRequiredSetup ? Visibility.Collapsed : Visibility.Visible;
        TitleText.Text = isRequiredSetup ? "Cadastro inicial da imobiliária" : "Dados da imobiliária";
    }

    private async void AgencyProfileWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProfileAsync();
        RazaoSocialBox.Focus();
        Keyboard.Focus(RazaoSocialBox);
    }

    private void RegisterMaskedFieldFormatters()
    {
        CnpjBox.TextChanged += (_, _) => FormatMaskedTextBox(CnpjBox, FormatCnpj, 14);
        ResponsavelCpfBox.TextChanged += (_, _) => FormatMaskedTextBox(ResponsavelCpfBox, FormatCpf, 11);
        CepBox.TextChanged += (_, _) => FormatMaskedTextBox(CepBox, FormatCep, 8);
        TelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(TelefoneBox, FormatPhone, 11);
        WhatsAppBox.TextChanged += (_, _) => FormatMaskedTextBox(WhatsAppBox, FormatPhone, 11);
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            var profile = await _agenciaPerfilService.GetAsync();
            if (profile is null)
            {
                return;
            }

            Fill(profile);
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void Fill(AgenciaPerfil profile)
    {
        RazaoSocialBox.Text = profile.RazaoSocial;
        NomeFantasiaBox.Text = profile.NomeFantasia ?? string.Empty;
        CnpjBox.Text = FormatCnpj(profile.Cnpj);
        InscricaoMunicipalBox.Text = profile.InscricaoMunicipal ?? string.Empty;
        InscricaoEstadualBox.Text = profile.InscricaoEstadual ?? string.Empty;
        CreciBox.Text = profile.Creci ?? string.Empty;
        ResponsavelNomeBox.Text = profile.ResponsavelNome ?? string.Empty;
        ResponsavelCpfBox.Text = FormatCpf(profile.ResponsavelCpf);
        ResponsavelCargoBox.Text = profile.ResponsavelCargo ?? string.Empty;
        EmailBox.Text = profile.Email ?? string.Empty;
        TelefoneBox.Text = FormatPhone(profile.Telefone);
        WhatsAppBox.Text = FormatPhone(profile.WhatsApp);
        SiteBox.Text = profile.Site ?? string.Empty;
        RuaBox.Text = profile.Rua ?? string.Empty;
        NumeroBox.Text = profile.Numero ?? string.Empty;
        ComplementoBox.Text = profile.Complemento ?? string.Empty;
        BairroBox.Text = profile.Bairro ?? string.Empty;
        CidadeBox.Text = profile.Cidade ?? string.Empty;
        EstadoBox.Text = profile.Estado ?? string.Empty;
        CepBox.Text = FormatCep(profile.Cep);
        DadosBancariosBox.Text = profile.DadosBancarios ?? string.Empty;
        TextoPadraoRodapeBox.Text = profile.TextoPadraoRodape ?? string.Empty;
        ObservacoesBox.Text = profile.Observacoes ?? string.Empty;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(RazaoSocialBox.Text))
        {
            ErrorText.Text = "Informe a razão social ou nome da imobiliária.";
            RazaoSocialBox.Focus();
            return;
        }

        try
        {
            await _agenciaPerfilService.SaveAsync(new AgenciaPerfilRequest(
                RazaoSocialBox.Text,
                NomeFantasiaBox.Text,
                CnpjBox.Text,
                InscricaoMunicipalBox.Text,
                InscricaoEstadualBox.Text,
                CreciBox.Text,
                ResponsavelNomeBox.Text,
                ResponsavelCpfBox.Text,
                ResponsavelCargoBox.Text,
                EmailBox.Text,
                TelefoneBox.Text,
                WhatsAppBox.Text,
                SiteBox.Text,
                RuaBox.Text,
                NumeroBox.Text,
                ComplementoBox.Text,
                BairroBox.Text,
                CidadeBox.Text,
                EstadoBox.Text,
                CepBox.Text,
                DadosBancariosBox.Text,
                TextoPadraoRodapeBox.Text,
                ObservacoesBox.Text));

            MessageBox.Show(this, "Dados da imobiliária salvos com sucesso.", "Dados da imobiliária", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void FormatMaskedTextBox(TextBox textBox, Func<string?, string> formatter, int maxDigits)
    {
        if (_isFormattingMaskedFields)
        {
            return;
        }

        _isFormattingMaskedFields = true;
        try
        {
            var digits = OnlyDigits(textBox.Text, maxDigits);
            var formatted = formatter(digits);
            if (!string.Equals(textBox.Text, formatted, StringComparison.Ordinal))
            {
                textBox.Text = formatted;
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
        finally
        {
            _isFormattingMaskedFields = false;
        }
    }

    private static string FormatCnpj(string? value)
    {
        var digits = OnlyDigits(value, 14);
        return digits.Length switch
        {
            <= 2 => digits,
            <= 5 => $"{digits[..2]}.{digits[2..]}",
            <= 8 => $"{digits[..2]}.{digits[2..5]}.{digits[5..]}",
            <= 12 => $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..]}",
            _ => $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}"
        };
    }

    private static string FormatCpf(string? value)
    {
        var digits = OnlyDigits(value, 11);
        return digits.Length switch
        {
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits[3..6]}.{digits[6..]}",
            _ => $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}"
        };
    }

    private static string FormatCep(string? value)
    {
        var digits = OnlyDigits(value, 8);
        return digits.Length <= 5 ? digits : $"{digits[..5]}-{digits[5..]}";
    }

    private static string FormatPhone(string? value)
    {
        var digits = OnlyDigits(value, 11);
        return digits.Length switch
        {
            <= 2 => digits,
            <= 6 => $"({digits[..2]}) {digits[2..]}",
            <= 10 => $"({digits[..2]}) {digits[2..6]}-{digits[6..]}",
            _ => $"({digits[..2]}) {digits[2..7]}-{digits[7..]}"
        };
    }

    private static string OnlyDigits(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).Take(maxLength).ToArray());
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_isRequiredSetup && DialogResult != true)
        {
            e.Cancel = true;
            ErrorText.Text = "Conclua o cadastro da imobiliária para continuar.";
            return;
        }

        base.OnClosing(e);
    }
}
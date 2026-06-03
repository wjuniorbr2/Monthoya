using System.Windows;
using System.Windows.Input;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class AgencyProfileWindow : Window
{
    private readonly IAgenciaPerfilService _agenciaPerfilService;
    private readonly bool _isRequiredSetup;

    public AgencyProfileWindow(IAgenciaPerfilService agenciaPerfilService)
    {
        InitializeComponent();
        _agenciaPerfilService = agenciaPerfilService;
        Loaded += AgencyProfileWindow_Loaded;
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
        CnpjBox.Text = profile.Cnpj ?? string.Empty;
        InscricaoMunicipalBox.Text = profile.InscricaoMunicipal ?? string.Empty;
        InscricaoEstadualBox.Text = profile.InscricaoEstadual ?? string.Empty;
        CreciBox.Text = profile.Creci ?? string.Empty;
        ResponsavelNomeBox.Text = profile.ResponsavelNome ?? string.Empty;
        ResponsavelCpfBox.Text = profile.ResponsavelCpf ?? string.Empty;
        ResponsavelCargoBox.Text = profile.ResponsavelCargo ?? string.Empty;
        EmailBox.Text = profile.Email ?? string.Empty;
        TelefoneBox.Text = profile.Telefone ?? string.Empty;
        WhatsAppBox.Text = profile.WhatsApp ?? string.Empty;
        SiteBox.Text = profile.Site ?? string.Empty;
        RuaBox.Text = profile.Rua ?? string.Empty;
        NumeroBox.Text = profile.Numero ?? string.Empty;
        ComplementoBox.Text = profile.Complemento ?? string.Empty;
        BairroBox.Text = profile.Bairro ?? string.Empty;
        CidadeBox.Text = profile.Cidade ?? string.Empty;
        EstadoBox.Text = profile.Estado ?? string.Empty;
        CepBox.Text = profile.Cep ?? string.Empty;
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

using System.Windows;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool FreshTabResetRegistrationRegistered = RegisterFreshTabResetRegistration();
    private bool _freshTabResetRegistrationApplied;

    private static bool RegisterFreshTabResetRegistration()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForFreshTabResetRegistration));

        return true;
    }

    private static void OnShellWindowLoadedForFreshTabResetRegistration(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyFreshTabResetRegistration, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyFreshTabResetRegistration()
    {
        if (_freshTabResetRegistrationApplied)
        {
            return;
        }

        _freshTabResetRegistrationApplied = true;
        RegisterFreshTabReset(PessoasNavButton, ShellPage.Pessoas, ResetPessoasSharedStateForFreshTab);
        RegisterFreshTabReset(ImoveisNavButton, ShellPage.Imoveis, ResetImoveisSharedStateForFreshTab);
    }

    private void ResetPessoasSharedStateForFreshTab()
    {
        InvalidatePessoaSelectionLoads();
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        _pendingPessoaDocumentos.Clear();
        _pessoaDocumentos = [];

        PessoasGrid.SelectedItem = null;
        SetPessoaDocumentoSelection(null);
        ClearPessoaDocumentoInputs();
        ClearPessoaForm();
        PessoaTipoBox.SelectedValue = Monthoya.Core.Entities.TipoPessoa.Fisica;
        PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
        PessoaDocumentosTitleText.Text = "Documentos anexos";
        SetPessoaEditMode(true, isNew: true);
        UpdatePeopleTopRowSpacingAndRolesVisibility();
        UpdatePessoaDocumentoEditorAvailability();
    }

    private void ResetImoveisSharedStateForFreshTab()
    {
        _selectedImovelId = null;
        _selectedImovelDetails = null;
        _pendingImovelMedia.Clear();
        _imovelImagens = [];
        _imovelVistorias = [];

        ImoveisGrid.SelectedItem = null;
        ImovelDetailsTabControl.SelectedIndex = 0;
        ClearImovelForm();
        SetImovelEditMode(true, isNew: true);
        ImovelDetailsTabControl.SelectedIndex = 0;
        ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
        RefreshImovelMediaGrid();
    }
}

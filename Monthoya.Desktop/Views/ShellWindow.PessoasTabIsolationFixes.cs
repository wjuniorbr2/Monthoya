using System.Windows;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool PessoasTabIsolationRegistered = RegisterPessoasTabIsolation();
    private bool _pessoasTabIsolationApplied;

    private static bool RegisterPessoasTabIsolation()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPessoasTabIsolation));

        return true;
    }

    private static void OnShellWindowLoadedForPessoasTabIsolation(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPessoasTabIsolation, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyPessoasTabIsolation()
    {
        if (_pessoasTabIsolationApplied)
        {
            return;
        }

        _pessoasTabIsolationApplied = true;
        RegisterFreshTabReset(PessoasNavButton, ShellPage.Pessoas, ResetPessoasSharedStateForFreshTab);
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
}

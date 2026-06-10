using Monthoya.Core.Entities;
using System.Threading.Tasks;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private PessoasPageState CapturePessoasPageState() =>
        new()
        {
            SearchText = PessoasSearchBox.Text,
            StatusFilter = PessoaStatusFilterBox.SelectedValue as string ?? "ativo",
            SelectedPessoaId = _selectedPessoaId,
            IsEditing = _isPessoaEditing,
            IsNew = _selectedPessoaId is null,
            DocumentoTipo = PessoaDocumentoTipoBox.SelectedValue as string ?? "cpf",
            DocumentoDono = PessoaDocumentoDonoBox.SelectedValue as string ?? "",
            DocumentoNome = PessoaDocumentoNomeBox.Text,
            DocumentoArquivo = PessoaDocumentoArquivoBox.Text,
            DocumentoValidade = ToDateOnly(PessoaDocumentoValidadeBox.SelectedDate),
            DocumentoObservacoes = PessoaDocumentoObservacoesBox.Text
        };

    private async Task RestorePessoasPageStateAsync(PessoasPageState state)
    {
        InvalidatePessoaSelectionLoads();
        PessoasSearchBox.Text = state.SearchText;
        PessoaStatusFilterBox.SelectedValue = state.StatusFilter;
        ApplyPessoasFilter();

        var selected = state.SelectedPessoaId.HasValue
            ? _pessoas.SingleOrDefault(x => x.Id == state.SelectedPessoaId.Value)
            : null;
        PessoasGrid.SelectedItem = selected;

        if (selected is null)
        {
            _selectedPessoaId = null;
            _selectedPessoaDetails = null;
            _pendingPessoaDocumentos.Clear();
            SetPessoaDocumentoSelection(null);
            await LoadPessoaDocumentosAsync(null);
            ClearPessoaForm();
            ClearPessoaDocumentoInputs();
            PessoaTipoBox.SelectedValue = TipoPessoa.Fisica;
            SetPessoaEditMode(true, isNew: true);
        }
        else
        {
            SetPessoaDocumentoSelection(selected);
            _selectedPessoaDetails = await _rentalManagementService.GetPessoaAsync(selected.Id);
            if (_selectedPessoaDetails is not null)
            {
                PopulatePessoaForm(_selectedPessoaDetails);
            }

            SetPessoaEditMode(state.IsEditing, isNew: false);
            await LoadPessoaDocumentosAsync(selected.Id);
        }

        PessoaDocumentoTipoBox.SelectedValue = state.DocumentoTipo;
        PessoaDocumentoDonoBox.SelectedValue = state.DocumentoDono;
        PessoaDocumentoNomeBox.Text = state.DocumentoNome;
        PessoaDocumentoArquivoBox.Text = state.DocumentoArquivo;
        PessoaDocumentoValidadeBox.SelectedDate = ToDateTime(state.DocumentoValidade);
        PessoaDocumentoObservacoesBox.Text = state.DocumentoObservacoes;
        UpdatePeopleTopRowSpacingAndRolesVisibility();
        UpdatePessoaDocumentoEditorAvailability();
    }
}

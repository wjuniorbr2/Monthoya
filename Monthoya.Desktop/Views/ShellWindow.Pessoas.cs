using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{

    private async Task LoadPessoasAsync()
    {
        _pessoas = await _rentalManagementService.GetPessoasAsync();
        _ = LoadPessoasStreetSuggestionsInBackgroundAsync();
        ApplyPessoasFilter();

        ImovelProprietarioBox.ItemsSource = _pessoas.Where(x => x.Status == "Ativo").ToList();
    }


    private async Task LoadPessoasStreetSuggestionsInBackgroundAsync()
    {
        try
        {
            _streetSuggestions = await _rentalManagementService.GetStreetSuggestionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Street suggestions failed: {ex.Message}");
        }
    }
    private async void ReloadPessoasButton_Click(object sender, RoutedEventArgs e) => await LoadPessoasAsync();

    private void PessoasSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyPessoasFilter();
        SaveActiveTabState();
    }

    private void PessoaStatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyPessoasFilter();
        SaveActiveTabState();
    }

    private async void PessoasGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRestoringTabState)
        {
            return;
        }

        if (PessoasGrid.SelectedItem is not PessoaSummary pessoa)
        {
            SetPessoaDocumentoSelection(null);
            await LoadPessoaDocumentosAsync(null);
            SaveActiveTabState();
            return;
        }

        SetPessoaDocumentoSelection(pessoa);
        _selectedPessoaDetails = await _rentalManagementService.GetPessoaAsync(pessoa.Id);
        if (_selectedPessoaDetails is not null)
        {
            PopulatePessoaForm(_selectedPessoaDetails);
            SetPessoaEditMode(false, isNew: false);
        }

        await LoadPessoaDocumentosAsync(pessoa.Id);
        SaveActiveTabState();
    }

    private void PessoaTipoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TogglePessoaTypePanels();
        UpdatePessoaConditionalSections();
    }

    private void TogglePessoaTypePanels()
    {
        if (PessoaFisicaFieldsPanel is null || PessoaJuridicaFieldsPanel is null)
        {
            return;
        }

        var tipo = PessoaTipoBox.SelectedValue is TipoPessoa selectedTipo ? selectedTipo : TipoPessoa.Fisica;
        PessoaFisicaFieldsPanel.Visibility = tipo == TipoPessoa.Fisica ? Visibility.Visible : Visibility.Collapsed;
        PessoaJuridicaFieldsPanel.Visibility = tipo == TipoPessoa.Juridica ? Visibility.Visible : Visibility.Collapsed;
        PessoaDocumentoDonoBox.ItemsSource = tipo == TipoPessoa.Fisica ? PessoaDocumentoDonoFisicaOptions : PessoaDocumentoDonoJuridicaOptions;
        PessoaDocumentoTipoBox.ItemsSource = tipo == TipoPessoa.Fisica ? PessoaDocumentoTipoFisicaOptions : PessoaDocumentoTipoJuridicaOptions;
        if (PessoaDocumentoDonoBox.SelectedValue is null)
        {
            PessoaDocumentoDonoBox.SelectedIndex = 0;
        }

        if (PessoaDocumentoTipoBox.SelectedValue is null)
        {
            PessoaDocumentoTipoBox.SelectedIndex = 0;
        }
    }

    private async Task LoadPessoaDocumentosAsync(Guid? pessoaId)
    {
        PessoaDocumentosListErrorText.Text = string.Empty;

        if (!pessoaId.HasValue)
        {
            _pessoaDocumentos = [];
            PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
            PessoaDocumentosTitleText.Text = "Documentos anexos";
            return;
        }

        _pessoaDocumentos = await _rentalManagementService.GetPessoaDocumentosAsync(pessoaId.Value);
        PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
        PessoaDocumentosTitleText.Text = "Documentos anexos";
    }

    private void SetPessoaDocumentoSelection(PessoaSummary? pessoa)
    {
        PessoaDocumentosListErrorText.Text = string.Empty;
        PessoaDocumentoErrorText.Text = string.Empty;
        _selectedPessoaId = pessoa?.Id;
        PessoaDocumentoPessoaText.Text = pessoa is null ? "Nenhuma pessoa selecionada" : pessoa.Nome;
        PessoaProprietarioBox.IsChecked = pessoa?.IsProprietario == true;
        PessoaLocatarioBox.IsChecked = pessoa?.IsLocatario == true;
        PessoaFiadorBox.IsChecked = pessoa?.IsFiador == true;
        UpdatePessoaDocumentoEditorAvailability();
    }

    private bool ValidatePessoaForm()
    {
        if (!ValidateEmail(PessoaEmailBox.Text, "E-mail"))
        {
            return false;
        }

        if (!ValidateEmail(PessoaResponsavelEmailBox.Text, "E-mail do responsável"))
        {
            return false;
        }

        foreach (var (value, field) in new[]
        {
            (_pessoaEmailEmpresaTrabalhoBox?.Text, "E-mail da empresa"),
            (_pessoaConjugeEmailBox?.Text, "E-mail do cônjuge"),
            (_pessoaConjugeEmailEmpresaTrabalhoBox?.Text, "E-mail da empresa do cônjuge")
        })
        {
            if (!ValidateEmail(value ?? string.Empty, field))
            {
                return false;
            }
        }

        return TryApplyBrazilianDate(PessoaDataNascimentoBox)
            && TryApplyBrazilianDate(PessoaConjugeDataNascimentoBox)
            && TryApplyBrazilianDate(PessoaResponsavelDataNascimentoBox)
            && (_pessoaDataAberturaBox is null || TryApplyBrazilianDate(_pessoaDataAberturaBox));
    }

    private bool ValidateEmail(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant))
        {
            return true;
        }

        PessoaErrorText.Text = $"{fieldName} não está no formato correto. Exemplo: cliente@email.com";
        return false;
    }

    private void ApplyPessoasFilter()
    {
        var query = PessoasSearchBox.Text;
        var statusFilter = PessoaStatusFilterBox.SelectedValue as string ?? "ativo";
        PessoasGrid.ItemsSource = _pessoas
            .Where(x => statusFilter switch
            {
                "inativo" => x.Status == "Inativo",
                "todos" => true,
                _ => x.Status == "Ativo"
            })
            .Where(x => ContainsSearch(query, x.Nome, x.Documento, x.Roles, x.Telefone, x.Email))
            .ToList();
    }

    private async void SavePessoaButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaErrorText.Text = string.Empty;

        try
        {
            if (!ValidatePessoaForm())
            {
                return;
            }

            var request = BuildPessoaRequest();
            if (_selectedPessoaId.HasValue && _selectedPessoaDetails is not null)
            {
                await _rentalManagementService.UpdatePessoaAsync(new UpdatePessoaRequest(_selectedPessoaId.Value, request));
                SetPessoaEditMode(false, isNew: false);
            }
            else
            {
                await _rentalManagementService.CreatePessoaAsync(request);
                ClearPessoaForm();
                SetPessoaEditMode(true, isNew: true);
            }

            await LoadPessoasAsync();
        }
        catch (Exception ex)
        {
            PessoaErrorText.Text = ex.Message;
        }
    }

    private async void SavePessoaDocumentoButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        try
        {
            var pessoaId = _selectedPessoaId ?? Guid.Empty;
            var tipo = PessoaDocumentoTipoBox.SelectedValue as string ?? "outros";
            var documentoDe = PessoaDocumentoDonoBox.SelectedValue as string ?? "pessoa";

            await _rentalManagementService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
                pessoaId,
                tipo,
                PessoaDocumentoNomeBox.Text,
                PessoaDocumentoArquivoBox.Text,
                null,
                ToDateOnly(PessoaDocumentoValidadeBox.SelectedDate),
                PessoaDocumentoObservacoesBox.Text,
                documentoDe));

            PessoaDocumentoNomeBox.Clear();
            PessoaDocumentoArquivoBox.Clear();
            PessoaDocumentoValidadeBox.SelectedDate = null;
            PessoaDocumentoObservacoesBox.Clear();
            await LoadPessoasAsync();
            await LoadPessoaDocumentosAsync(_selectedPessoaId);
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }


    private void NewPessoaButton_Click(object sender, RoutedEventArgs e)
    {
        InvalidatePessoaSelectionLoads();
        PessoasGrid.SelectedItem = null;
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        _pendingPessoaDocumentos.Clear();
        SetPessoaDocumentoSelection(null);
        ClearPessoaDocumentoInputs();
        ClearPessoaForm();
        _pessoaDocumentos = [];
        PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
        PessoaDocumentosTitleText.Text = "Documentos anexos";
        SetPessoaEditMode(true, isNew: true);
        SaveActiveTabState();
    }

    private void EditPessoaButton_Click(object sender, RoutedEventArgs e)
    {
        SetPessoaEditMode(true, isNew: false);
        SaveActiveTabState();
    }

    private void CancelPessoaEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPessoaDetails is not null)
        {
            PopulatePessoaForm(_selectedPessoaDetails);
            SetPessoaEditMode(false, isNew: false);
            SaveActiveTabState();
        }
    }

    private async void DeactivatePessoaButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedPessoaId.HasValue)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "Remover esta pessoa apenas altera o status para inativo. Deseja continuar?",
            "Confirmar remoção",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var password = PromptPassword("Digite sua senha para confirmar a remoção.");
        if (password is null)
        {
            return;
        }

        if (!await _userService.VerifyPasswordAsync(_currentUser.Id, password))
        {
            PessoaErrorText.Text = "Senha incorreta. A pessoa não foi removida.";
            return;
        }

        await _rentalManagementService.SetPessoaActiveAsync(_selectedPessoaId.Value, false);
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        ClearPessoaForm();
        SetPessoaDocumentoSelection(null);
        SetPessoaEditMode(true, isNew: true);
        SaveActiveTabState();
        await LoadPessoasAsync();
    }


}










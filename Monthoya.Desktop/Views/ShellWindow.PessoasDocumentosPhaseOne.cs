using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Monthoya.Core.Entities;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentosPhaseOnePatchApplied;

    private static readonly bool PessoaDocumentosPhaseOneClassHandlerRegistered = RegisterPessoaDocumentosPhaseOneClassHandler();

    private static readonly IReadOnlyList<PessoaDocumentoDonoOption> PessoaDocumentoDonoFisicaPhaseOneOptions =
    [
        new("Pessoa", "pessoa"),
        new("Trabalho da pessoa", "empresa_trabalho"),
        new("Cônjuge", "conjuge"),
        new("Trabalho do cônjuge", "trabalho_conjuge"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaDocumentoDonoOption> PessoaDocumentoDonoJuridicaPhaseOneOptions =
    [
        new("Empresa", "empresa"),
        new("Responsável", "responsavel"),
        new("Cônjuge do responsável", "conjuge_responsavel"),
        new("Trabalho do cônjuge do responsável", "trabalho_conjuge_responsavel"),
        new("Outros", "outros")
    ];

    private static bool RegisterPessoaDocumentosPhaseOneClassHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentosPhaseOnePatch()));

        return true;
    }

    private void ApplyPessoaDocumentosPhaseOnePatch()
    {
        _ = PessoaDocumentosPhaseOneClassHandlerRegistered;

        if (_pessoaDocumentosPhaseOnePatchApplied)
        {
            return;
        }

        _pessoaDocumentosPhaseOnePatchApplied = true;
        ApplyPessoaDocumentoDonoPhaseOneOptions();

        PessoaTipoBox.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
            ApplyPessoaDocumentoDonoPhaseOneOptions,
            DispatcherPriority.Background);

        PessoaTipoBox.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
            EnsurePessoaResponsavelEstadoCivilInMainRow,
            DispatcherPriority.Background);

        PessoasNavButton.Click += (_, _) => Dispatcher.BeginInvoke(
            EnsurePessoaResponsavelEstadoCivilInMainRow,
            DispatcherPriority.Background);

        Dispatcher.BeginInvoke(EnsurePessoaResponsavelEstadoCivilInMainRow, DispatcherPriority.Background);
    }

    private void ApplyPessoaDocumentoDonoPhaseOneOptions()
    {
        var isJuridica = PessoaTipoBox.SelectedValue is TipoPessoa.Juridica;
        var options = isJuridica
            ? PessoaDocumentoDonoJuridicaPhaseOneOptions
            : PessoaDocumentoDonoFisicaPhaseOneOptions;

        var previousValue = PessoaDocumentoDonoBox.SelectedValue as string;
        PessoaDocumentoDonoBox.ItemsSource = options;

        if (!string.IsNullOrWhiteSpace(previousValue) && options.Any(option => option.Tipo == previousValue))
        {
            PessoaDocumentoDonoBox.SelectedValue = previousValue;
            return;
        }

        PessoaDocumentoDonoBox.SelectedValue = isJuridica ? "empresa" : "pessoa";
    }

    private void EnsurePessoaResponsavelEstadoCivilInMainRow()
    {
        if (PessoaResponsavelEstadoCivilBox.Parent is not null)
        {
            return;
        }

        var responsibleRow = PessoaJuridicaFieldsPanel.Children
            .OfType<WrapPanel>()
            .FirstOrDefault(panel => panel.Children
                .OfType<StackPanel>()
                .Any(stack => StackContainsControl(stack, PessoaResponsavelNomeBox)));

        if (responsibleRow is null)
        {
            return;
        }

        var field = FieldStack("Estado civil", PessoaResponsavelEstadoCivilBox, 170);
        var insertIndex = GetInsertIndexAfterControl(responsibleRow, PessoaResponsavelNacionalidadeBox);
        if (insertIndex < 0)
        {
            insertIndex = GetInsertIndexBeforeControl(responsibleRow, PessoaResponsavelDadosBancariosBox);
        }

        responsibleRow.Children.Insert(insertIndex < 0 ? responsibleRow.Children.Count : insertIndex, field);
    }

    private static bool StackContainsControl(StackPanel stack, Control control) =>
        stack.Children.OfType<Control>().Any(child => ReferenceEquals(child, control));

    private static int GetInsertIndexAfterControl(WrapPanel panel, Control control)
    {
        for (var index = 0; index < panel.Children.Count; index++)
        {
            if (panel.Children[index] is StackPanel stack && StackContainsControl(stack, control))
            {
                return index + 1;
            }
        }

        return -1;
    }

    private static int GetInsertIndexBeforeControl(WrapPanel panel, Control control)
    {
        for (var index = 0; index < panel.Children.Count; index++)
        {
            if (panel.Children[index] is StackPanel stack && StackContainsControl(stack, control))
            {
                return index;
            }
        }

        return -1;
    }
}

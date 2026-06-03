using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesExtraAdjustmentsRegistered = RegisterChavesExtraAdjustments();
    private bool _chavesExtraAdjustmentsApplied;
    private bool _isFormattingChavesTime;
    private readonly Dictionary<Guid, string?> _chavesBoardCodeByImovelId = [];

    private static bool RegisterChavesExtraAdjustments()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesExtraAdjustments));

        return true;
    }

    private static void OnShellWindowLoadedForChavesExtraAdjustments(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesExtraAdjustments, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyChavesExtraAdjustments()
    {
        if (_chavesExtraAdjustmentsApplied)
        {
            return;
        }

        _chavesExtraAdjustmentsApplied = true;

        ChavesStatusFilterBox.Visibility = Visibility.Collapsed;
        CollapseImmediateLabelBefore(ChavesStatusFilterBox);
        CollapseFieldContainer(ChavesCodigoBox);

        ChavesSearchBox.Width = 360;
        ChavesSearchBox.HorizontalAlignment = HorizontalAlignment.Left;

        ChavesRetiradoPorTelefoneBox.Width = 108;
        ChavesRetiradoPorTelefoneBox.HorizontalAlignment = HorizontalAlignment.Left;
        ChavesMotivoBox.Width = 230;
        ChavesObservacoesBox.Width = 300;

        ChavesGrid.SelectionChanged += (_, _) => UpdateChavesBoardCodeDisplayFromSelection();
        ChavesPanel.IsVisibleChanged += (_, _) =>
        {
            if (ChavesPanel.IsVisible)
            {
                Dispatcher.BeginInvoke(ApplyChavesExtraAdjustmentsAfterLayout, DispatcherPriority.ApplicationIdle);
            }
        };

        Dispatcher.BeginInvoke(ApplyChavesExtraAdjustmentsAfterLayout, DispatcherPriority.ApplicationIdle);
    }

    private async void ApplyChavesExtraAdjustmentsAfterLayout()
    {
        ReorderChavesRetiradaFields();
        ConfigureChavesRelationAndTimeFields();
        ConfigureChavesColumnsForCurrentMode();
        UpdateChavesBoardCodeDisplayFromSelection();
        await RefreshChavesBoardCodesFromImoveisAsync();
    }

    private void ReorderChavesRetiradaFields()
    {
        var takenByContainer = GetFieldContainer(ChavesRetiradoPorNomeBox);
        var phoneContainer = GetFieldContainer(ChavesRetiradoPorTelefoneBox);
        var documentContainer = GetFieldContainer(ChavesRetiradoPorDocumentoBox);
        var reasonContainer = GetFieldContainer(ChavesMotivoBox);
        var dateContainer = GetFieldContainer(ChavesPrevisaoBox);
        var obsContainer = GetFieldContainer(ChavesObservacoesBox);

        if (takenByContainer?.Parent is not WrapPanel wrap)
        {
            return;
        }

        var relationContainer = _chavesRelacaoComboBox is null ? null : GetFieldContainer(_chavesRelacaoComboBox);
        var ordered = new List<UIElement?>
        {
            takenByContainer,
            relationContainer,
            phoneContainer,
            documentContainer,
            reasonContainer,
            dateContainer,
            obsContainer
        };

        foreach (var child in ordered.Where(child => child is not null).Cast<UIElement>().ToList())
        {
            if (wrap.Children.Contains(child))
            {
                wrap.Children.Remove(child);
            }
        }

        foreach (var child in ordered.Where(child => child is not null).Cast<UIElement>())
        {
            wrap.Children.Add(child);
        }
    }

    private void ConfigureChavesRelationAndTimeFields()
    {
        if (_chavesRelacaoComboBox is not null)
        {
            _chavesRelacaoComboBox.Width = 112;
            if (GetFieldContainer(_chavesRelacaoComboBox) is FrameworkElement relationContainer)
            {
                relationContainer.Width = 112;
            }
        }

        if (_chavesPrevisaoHoraBox is not null)
        {
            _chavesPrevisaoHoraBox.Width = 62;
            _chavesPrevisaoHoraBox.PreviewTextInput -= ChavesPrevisaoHoraBox_PreviewTextInput;
            _chavesPrevisaoHoraBox.PreviewTextInput += ChavesPrevisaoHoraBox_PreviewTextInput;
            _chavesPrevisaoHoraBox.TextChanged -= ChavesPrevisaoHoraBox_TextChanged;
            _chavesPrevisaoHoraBox.TextChanged += ChavesPrevisaoHoraBox_TextChanged;
            DataObject.RemovePastingHandler(_chavesPrevisaoHoraBox, ChavesPrevisaoHoraBox_OnPaste);
            DataObject.AddPastingHandler(_chavesPrevisaoHoraBox, ChavesPrevisaoHoraBox_OnPaste);
        }

        if (GetFieldContainer(ChavesRetiradoPorTelefoneBox) is FrameworkElement phoneContainer)
        {
            phoneContainer.Width = 108;
        }
    }

    private static void ChavesPrevisaoHoraBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
    }

    private void ChavesPrevisaoHoraBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingChavesTime || sender is not TextBox textBox)
        {
            return;
        }

        _isFormattingChavesTime = true;
        try
        {
            var digits = new string(textBox.Text.Where(char.IsDigit).Take(4).ToArray());
            var formatted = digits.Length <= 2
                ? digits
                : $"{digits[..2]}:{digits[2..]}";

            if (!string.Equals(textBox.Text, formatted, StringComparison.Ordinal))
            {
                textBox.Text = formatted;
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
        finally
        {
            _isFormattingChavesTime = false;
        }
    }

    private static void ChavesPrevisaoHoraBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        var digits = new string(text.Where(char.IsDigit).Take(4).ToArray());
        e.DataObject = new DataObject(DataFormats.Text, digits);
    }

    private async Task RefreshChavesBoardCodesFromImoveisAsync()
    {
        if (ChavesGrid.ItemsSource is not IEnumerable<object> items)
        {
            return;
        }

        var list = items.OfType<ChavesListItem>().ToList();
        if (list.Count == 0)
        {
            return;
        }

        var changed = false;
        foreach (var item in list)
        {
            if (!_chavesBoardCodeByImovelId.TryGetValue(item.ImovelId, out var code))
            {
                var details = await _rentalManagementService.GetImovelAsync(item.ImovelId);
                code = details?.Dados.ChaveCodigo;
                _chavesBoardCodeByImovelId[item.ImovelId] = code;
            }

            if (!string.IsNullOrWhiteSpace(code) && !string.Equals(item.ChaveCodigo, code, StringComparison.Ordinal))
            {
                changed = true;
            }
        }

        if (!changed)
        {
            UpdateChavesBoardCodeDisplayFromSelection();
            return;
        }

        ChavesGrid.ItemsSource = list
            .Select(item => _chavesBoardCodeByImovelId.TryGetValue(item.ImovelId, out var code) && !string.IsNullOrWhiteSpace(code)
                ? item with { ChaveCodigo = code }
                : item)
            .ToList();
        ConfigureChavesColumnsForCurrentMode();
        UpdateChavesBoardCodeDisplayFromSelection();
    }

    private void ConfigureChavesColumnsForCurrentMode()
    {
        var mode = _chavesMovimentoTipoBox?.SelectedItem as string;
        ChavesGrid.Columns.Clear();

        AddChavesColumn("Código", "ChaveCodigo", 0.65);
        AddChavesColumn("Endereço", "Imovel", 2.2);
        AddChavesColumn("Proprietário", "Proprietario", 1.6);
        AddChavesColumn("Situação", "Status", 1.1);

        if (string.Equals(mode, "Retirada", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AddChavesColumn("Retirado por", "RetiradoPorNome", 1.3);
        AddChavesColumn("Telefone", "RetiradoPorTelefone", 0.9);
        AddChavesColumn("Relação", "Relacao", 0.8);
        AddChavesColumn("Motivo", "Motivo", 1.1);
        AddChavesColumn("Retirado em", "RetiradoEm", 1.0, "dd/MM/yyyy HH:mm");
        AddChavesColumn("Previsão", "PrevisaoDevolucaoEm", 1.0, "dd/MM/yyyy HH:mm");
    }

    private void AddChavesColumn(string header, string binding, double width, string? stringFormat = null)
    {
        var columnBinding = new System.Windows.Data.Binding(binding);
        if (!string.IsNullOrWhiteSpace(stringFormat))
        {
            columnBinding.StringFormat = stringFormat;
        }

        ChavesGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = columnBinding,
            Width = new DataGridLength(width, DataGridLengthUnitType.Star)
        });
    }

    private void UpdateChavesBoardCodeDisplayFromSelection()
    {
        if (_chavesSelectedImovelText is null)
        {
            return;
        }

        if (ChavesGrid.SelectedItem is not ChavesListItem item)
        {
            _chavesSelectedImovelText.Text = string.Empty;
            return;
        }

        var codigo = item.ChaveCodigo;
        if (string.IsNullOrWhiteSpace(codigo) && _chavesBoardCodeByImovelId.TryGetValue(item.ImovelId, out var loadedCode))
        {
            codigo = loadedCode;
        }

        codigo = string.IsNullOrWhiteSpace(codigo) ? "-" : codigo;
        _chavesSelectedImovelText.Text = $"Código: {codigo} | Imóvel: {item.Imovel} | Proprietário: {item.Proprietario}";
    }

    private static FrameworkElement? GetFieldContainer(FrameworkElement field)
    {
        var current = field.Parent as FrameworkElement;
        while (current is not null)
        {
            if (current.Parent is WrapPanel)
            {
                return current;
            }

            current = current.Parent as FrameworkElement;
        }

        return null;
    }

    private static void CollapseImmediateLabelBefore(FrameworkElement field)
    {
        if (field.Parent is not Panel panel)
        {
            return;
        }

        var index = panel.Children.IndexOf(field);
        if (index > 0 && panel.Children[index - 1] is TextBlock label)
        {
            label.Visibility = Visibility.Collapsed;
        }
    }

    private static void CollapseFieldContainer(FrameworkElement field)
    {
        if (field.Parent is not FrameworkElement parentElement)
        {
            field.Visibility = Visibility.Collapsed;
            return;
        }

        parentElement.Visibility = Visibility.Collapsed;
    }
}

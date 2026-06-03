using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        ChavesRetiradoPorTelefoneBox.Width = 130;
        ChavesRetiradoPorTelefoneBox.HorizontalAlignment = HorizontalAlignment.Left;
        ChavesMotivoBox.Width = 230;
        ChavesObservacoesBox.Width = 300;

        ApplyChavesPrimaryButtonStyle(SaveChaveRetiradaButton);
        ApplyChavesPrimaryButtonStyle(ReturnChaveButton);

        ChavesRetiradoPorNomeBox.TextChanged += (_, _) => UpdateChavesWithdrawalButtonState();
        ChavesRetiradoPorTelefoneBox.TextChanged += (_, _) => UpdateChavesWithdrawalButtonState();
        ChavesRetiradoPorRelacaoBox.TextChanged += (_, _) => UpdateChavesWithdrawalButtonState();
        ChavesGrid.SelectionChanged += (_, _) =>
        {
            UpdateChavesBoardCodeDisplayFromSelection();
            UpdateChavesWithdrawalButtonState();
        };

        ChavesPanel.IsVisibleChanged += (_, _) =>
        {
            if (ChavesPanel.IsVisible)
            {
                Dispatcher.BeginInvoke(ApplyChavesExtraAdjustmentsAfterLayout, DispatcherPriority.ApplicationIdle);
            }
        };

        Dispatcher.BeginInvoke(ApplyChavesExtraAdjustmentsAfterLayout, DispatcherPriority.ApplicationIdle);
    }

    private void ApplyChavesExtraAdjustmentsAfterLayout()
    {
        ReorderChavesRetiradaFields();
        ConfigureChavesRelationAndTimeFields();
        ConfigureChavesListCardBounds();
        ApplyChavesPrimaryButtonStyle(SaveChaveRetiradaButton);
        ApplyChavesPrimaryButtonStyle(ReturnChaveButton);
        UpdateChavesWithdrawalButtonState();
        UpdateChavesBoardCodeDisplayFromSelection();
    }

    private void ConfigureChavesListCardBounds()
    {
        var listHost = FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => border.Child == ChavesGrid);
        if (listHost is null)
        {
            return;
        }

        listHost.Margin = new Thickness(0, 6, 0, 0);
        listHost.HorizontalAlignment = HorizontalAlignment.Stretch;
        listHost.VerticalAlignment = VerticalAlignment.Stretch;
        listHost.Padding = new Thickness(10);

        ChavesGrid.Margin = new Thickness(0);
        ChavesGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        ChavesGrid.VerticalAlignment = VerticalAlignment.Stretch;
        ChavesGrid.MinHeight = 0;
        ChavesGrid.MaxHeight = double.PositiveInfinity;
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
            _chavesRelacaoComboBox.SelectionChanged -= ChavesRelacaoComboBox_SelectionChangedForButtonState;
            _chavesRelacaoComboBox.SelectionChanged += ChavesRelacaoComboBox_SelectionChangedForButtonState;
            _chavesRelacaoComboBox.LostFocus -= ChavesRelacaoComboBox_LostFocusForButtonState;
            _chavesRelacaoComboBox.LostFocus += ChavesRelacaoComboBox_LostFocusForButtonState;
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
            phoneContainer.Width = 130;
        }
    }

    private void ChavesRelacaoComboBox_SelectionChangedForButtonState(object sender, SelectionChangedEventArgs e) =>
        UpdateChavesWithdrawalButtonState();

    private void ChavesRelacaoComboBox_LostFocusForButtonState(object sender, RoutedEventArgs e) =>
        UpdateChavesWithdrawalButtonState();

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
        var items = GetAllVisibleChavesItems().ToList();
        if (items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            if (_chavesBoardCodeByImovelId.ContainsKey(item.ImovelId))
            {
                continue;
            }

            var details = await _rentalManagementService.GetImovelAsync(item.ImovelId);
            _chavesBoardCodeByImovelId[item.ImovelId] = details?.Dados.ChaveCodigo;
        }

        UpdateChavesBoardCodeDisplayFromSelection();
    }

    private IEnumerable<ChavesListItem> GetAllVisibleChavesItems()
    {
        if (ChavesGrid.ItemsSource is IEnumerable<object> leftItems)
        {
            foreach (var item in leftItems.OfType<ChavesListItem>())
            {
                yield return item;
            }
        }

        if (_chavesTakenGrid?.ItemsSource is IEnumerable<object> rightItems)
        {
            foreach (var item in rightItems.OfType<ChavesListItem>())
            {
                yield return item;
            }
        }
    }

    private void UpdateChavesBoardCodeDisplayFromSelection()
    {
        if (_chavesSelectedImovelText is null)
        {
            return;
        }

        var item = ChavesGrid.SelectedItem as ChavesListItem
            ?? _chavesTakenGrid?.SelectedItem as ChavesListItem;

        if (item is null)
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

    private void UpdateChavesSelectedImovelSummary(string? imovel, string? proprietario)
    {
        if (_chavesSelectedImovelText is null)
        {
            return;
        }

        var selectedItem = ChavesGrid.SelectedItem as ChavesListItem
            ?? _chavesTakenGrid?.SelectedItem as ChavesListItem;
        var codigo = selectedItem?.ChaveCodigo;
        if (selectedItem is not null
            && string.IsNullOrWhiteSpace(codigo)
            && _chavesBoardCodeByImovelId.TryGetValue(selectedItem.ImovelId, out var loadedCode))
        {
            codigo = loadedCode;
        }

        codigo = string.IsNullOrWhiteSpace(codigo) ? "-" : codigo;
        _chavesSelectedImovelText.Text = string.IsNullOrWhiteSpace(imovel)
            ? string.Empty
            : $"Código: {codigo} | Imóvel: {imovel} | Proprietário: {proprietario ?? "-"}";
    }

    private void ClearChavesSelectedImovelSummary()
    {
        if (_chavesSelectedImovelText is not null)
        {
            _chavesSelectedImovelText.Text = string.Empty;
        }
    }

    private void ResetChavesRelacaoDropdown()
    {
        if (_chavesRelacaoComboBox is not null)
        {
            _chavesRelacaoComboBox.SelectedIndex = -1;
            _chavesRelacaoComboBox.Text = string.Empty;
        }
    }

    private TimeSpan GetChavesPrevisaoHorario()
    {
        if (_chavesPrevisaoHoraBox is not null
            && TimeSpan.TryParse(_chavesPrevisaoHoraBox.Text, CultureInfo.GetCultureInfo("pt-BR"), out var parsed))
        {
            return parsed;
        }

        return TimeSpan.FromHours(18);
    }

    private void UpdateChavesWithdrawalButtonState()
    {
        var hasSelectedAvailableProperty = ChavesGrid.SelectedItem is ChavesListItem item && !item.MovimentoId.HasValue;
        var hasName = !string.IsNullOrWhiteSpace(ChavesRetiradoPorNomeBox.Text);
        var hasPhone = !string.IsNullOrWhiteSpace(ChavesRetiradoPorTelefoneBox.Text);
        var hasRelation = !string.IsNullOrWhiteSpace(ChavesRetiradoPorRelacaoBox.Text)
            || !string.IsNullOrWhiteSpace(_chavesRelacaoComboBox?.Text);
        SaveChaveRetiradaButton.IsEnabled = hasSelectedAvailableProperty && hasName && hasRelation && hasPhone;
    }

    private static void ApplyChavesPrimaryButtonStyle(Button button)
    {
        var enabledBrush = new SolidColorBrush(Color.FromRgb(0, 109, 176));
        var disabledBrush = new SolidColorBrush(Color.FromRgb(116, 181, 216));
        var borderBrush = new SolidColorBrush(Color.FromRgb(0, 93, 150));

        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, enabledBrush));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, borderBrush));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(18, 7, 18, 7)));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0));

        var disabledTrigger = new Trigger
        {
            Property = UIElement.IsEnabledProperty,
            Value = false
        };
        disabledTrigger.Setters.Add(new Setter(Control.BackgroundProperty, disabledBrush));
        disabledTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        disabledTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, disabledBrush));
        disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0));
        style.Triggers.Add(disabledTrigger);

        button.Style = style;
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

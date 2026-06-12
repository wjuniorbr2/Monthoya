using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private ComboBox CreateComboBox(IEnumerable<object> items, string? displayMemberPath)
    {
        var itemList = items.ToList();
        var isSearchable = !string.IsNullOrWhiteSpace(displayMemberPath);
        var comboBox = new ComboBox
        {
            ItemsSource = itemList,
            Margin = new Thickness(0, 6, 0, 12),
            IsEditable = isSearchable,
            IsTextSearchEnabled = !isSearchable,
            StaysOpenOnEdit = isSearchable,
            SelectedIndex = -1
        };

        if (isSearchable)
        {
            comboBox.DisplayMemberPath = displayMemberPath;
            TextSearch.SetTextPath(comboBox, displayMemberPath!);
            AttachSearchableComboBox(comboBox, itemList, displayMemberPath!);
        }

        return comboBox;
    }

    private static void AttachSearchableComboBox(ComboBox comboBox, IReadOnlyList<object> sourceItems, string displayMemberPath)
    {
        var isFiltering = false;

        comboBox.PreviewTextInput += (_, _) =>
        {
            if (comboBox.SelectedItem is not null)
            {
                comboBox.SelectedItem = null;
            }
        };

        comboBox.Loaded += (_, _) =>
        {
            if (comboBox.Template?.FindName("PART_EditableTextBox", comboBox) is not TextBox textBox)
            {
                return;
            }

            textBox.TextChanged += (_, _) =>
            {
                if (isFiltering || !comboBox.IsKeyboardFocusWithin || comboBox.SelectedItem is not null)
                {
                    return;
                }

                isFiltering = true;
                var searchText = textBox.Text ?? string.Empty;
                var filtered = string.IsNullOrWhiteSpace(searchText)
                    ? sourceItems.ToList()
                    : sourceItems
                        .Where(item => GetComboBoxDisplayValue(item, displayMemberPath)
                            .Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();

                comboBox.ItemsSource = filtered;
                comboBox.IsDropDownOpen = filtered.Count > 0 && comboBox.IsKeyboardFocusWithin;
                textBox.SelectionStart = (textBox.Text ?? string.Empty).Length;
                textBox.SelectionLength = 0;
                isFiltering = false;
            };
        };

        comboBox.SelectionChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is not null)
            {
                comboBox.ItemsSource = sourceItems;
            }
        };
    }

    private static string GetComboBoxDisplayValue(object item, string displayMemberPath)
    {
        var property = item.GetType().GetProperty(displayMemberPath);
        return property?.GetValue(item)?.ToString() ?? item.ToString() ?? string.Empty;
    }

    private static void AddLabeledControl(Panel panel, string label, Control control)
    {
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(control);
    }

    private static void AddLabeledControl(Panel panel, string label, Control control, double width)
    {
        var fieldPanel = new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 14, 12)
        };

        if (!string.IsNullOrWhiteSpace(label))
        {
            fieldPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold
            });
        }

        fieldPanel.Children.Add(control);
        panel.Children.Add(fieldPanel);
    }

    private async Task<string> GetNextAvailableLocacaoCodeAsync(Guid? currentLocacaoId = null)
    {
        IReadOnlyList<LocacaoSummary> locacoes;
        try
        {
            locacoes = await _rentalManagementService.GetLocacoesAsync();
        }
        catch
        {
            locacoes = _moduleItems.OfType<LocacaoSummary>().ToList();
        }

        var usedCodes = locacoes
            .Where(x => !currentLocacaoId.HasValue || x.Id != currentLocacaoId.Value)
            .Where(x => !IsFinalizedLocacaoStatus(x.Status))
            .Select(x => int.TryParse(x.Codigo?.Trim(), out var parsed) && parsed > 0 ? parsed : 0)
            .Where(x => x > 0)
            .ToHashSet();

        var candidate = 1;
        while (usedCodes.Contains(candidate))
        {
            candidate++;
        }

        return candidate.ToString();
    }

    private static bool IsFinalizedLocacaoStatus(string? status) =>
        (status ?? string.Empty).Contains("Cancelada", StringComparison.OrdinalIgnoreCase)
        || (status ?? string.Empty).Contains("Encerrada", StringComparison.OrdinalIgnoreCase);

    private static DateOnly? ToLocacaoDateOnly(DateTime? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static int ParseRequiredDay(string? value, string label)
    {
        if (!int.TryParse(value, out var day) || day is < 1 or > 31)
        {
            throw new InvalidOperationException($"{label} deve ficar entre 1 e 31.");
        }

        return day;
    }

    private void ConfigureLocacaoCodeTextBox(TextBox textBox, TextBlock errorText)
    {
        textBox.PreviewTextInput += (_, e) =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^\d+$"))
            {
                return;
            }

            e.Handled = true;
            errorText.Text = "O código da locação deve usar apenas números.";
        };
        DataObject.AddPastingHandler(textBox, (_, e) =>
        {
            var text = e.DataObject.GetDataPresent(DataFormats.Text)
                ? e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty
                : string.Empty;
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+$"))
            {
                return;
            }

            e.CancelCommand();
            errorText.Text = "Cole apenas números no código da locação.";
        });
        textBox.LostKeyboardFocus += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text) && (!int.TryParse(textBox.Text, out var code) || code < 1))
            {
                errorText.Text = "O código da locação deve ser um número maior ou igual a 1.";
            }
        };
    }

    private void ConfigureLocacaoDecimalTextBox(TextBox textBox, TextBlock errorText)
    {
        textBox.PreviewTextInput += (_, e) =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[\d,.]+$"))
            {
                return;
            }

            e.Handled = true;
            errorText.Text = "Digite apenas números, vírgula ou ponto para valores.";
        };
        DataObject.AddPastingHandler(textBox, (_, e) =>
        {
            var text = e.DataObject.GetDataPresent(DataFormats.Text)
                ? e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty
                : string.Empty;
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^[\d,.]+$"))
            {
                return;
            }

            e.CancelCommand();
            errorText.Text = "Cole apenas números, vírgula ou ponto para valores.";
        });
        textBox.LostKeyboardFocus += (_, _) => FormatDecimalTextBox(textBox);
    }

    private void ConfigureLocacaoDatePicker(DatePicker datePicker, TextBlock errorText)
    {
        datePicker.Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR");
        datePicker.SelectedDateFormat = DatePickerFormat.Short;
        datePicker.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;
            Dispatcher.BeginInvoke(
                () => TryApplyBrazilianDate(datePicker, message => errorText.Text = message),
                System.Windows.Threading.DispatcherPriority.Background);
        };
        datePicker.LostKeyboardFocus += (_, _) => TryApplyBrazilianDate(datePicker, message => errorText.Text = message);
    }

    private static void ConfigureLocacaoDayTextBox(TextBox textBox, TextBlock errorText)
    {
        textBox.MaxLength = 2;
        textBox.PreviewTextInput += (_, e) =>
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^\d+$"))
            {
                e.Handled = true;
                errorText.Text = "Dias devem ser informados com números de 1 a 31.";
                return;
            }

            var selectedTextLength = textBox.SelectedText?.Length ?? 0;
            var nextLength = textBox.Text.Length - selectedTextLength + e.Text.Length;
            if (nextLength > 2)
            {
                e.Handled = true;
                errorText.Text = "Dias devem ter no máximo 2 dígitos.";
            }
        };
        DataObject.AddPastingHandler(textBox, (_, e) =>
        {
            var text = e.DataObject.GetDataPresent(DataFormats.Text)
                ? e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty
                : string.Empty;
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d{1,2}$"))
            {
                e.CancelCommand();
                errorText.Text = "Cole apenas números de 1 a 31, com no máximo 2 dígitos.";
            }
        });
        textBox.LostKeyboardFocus += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text) && (!int.TryParse(textBox.Text, out var day) || day is < 1 or > 31))
            {
                errorText.Text = "Dias devem ficar entre 1 e 31.";
            }
        };
    }
}

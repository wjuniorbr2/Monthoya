using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesTimeDropdownRegistered = RegisterChavesTimeDropdown();
    private bool _chavesTimeDropdownApplied;
    private ComboBox? _chavesPrevisaoHoraComboBox;

    private static bool RegisterChavesTimeDropdown()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesTimeDropdown));

        return true;
    }

    private static void OnShellWindowLoadedForChavesTimeDropdown(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesTimeDropdown, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyChavesTimeDropdown()
    {
        if (_chavesTimeDropdownApplied)
        {
            return;
        }

        if (_chavesPrevisaoHoraBox is null)
        {
            Dispatcher.BeginInvoke(ApplyChavesTimeDropdown, DispatcherPriority.ApplicationIdle);
            return;
        }

        if (_chavesPrevisaoHoraBox.Parent is not Panel parent)
        {
            return;
        }

        _chavesTimeDropdownApplied = true;

        var index = parent.Children.IndexOf(_chavesPrevisaoHoraBox);
        _chavesPrevisaoHoraBox.Visibility = Visibility.Collapsed;

        _chavesPrevisaoHoraComboBox = new ComboBox
        {
            Width = 104,
            Margin = new Thickness(0, 6, 0, 0),
            IsEditable = true,
            ToolTip = "Digite o horário ou escolha uma hora da lista",
            ItemsSource = Enumerable.Range(0, 24).Select(hour => $"{hour:00}:00").ToList(),
            Text = string.IsNullOrWhiteSpace(_chavesPrevisaoHoraBox.Text) ? "18:00" : _chavesPrevisaoHoraBox.Text
        };

        _chavesPrevisaoHoraComboBox.SelectionChanged += (_, _) => SyncChavesTimeDropdownToHiddenTextBox();
        _chavesPrevisaoHoraComboBox.LostFocus += (_, _) => NormalizeChavesTimeDropdown();
        _chavesPrevisaoHoraComboBox.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
        _chavesPrevisaoHoraComboBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_, _) => SyncChavesTimeDropdownToHiddenTextBox()));

        if (index >= 0 && index < parent.Children.Count)
        {
            parent.Children.Insert(index + 1, _chavesPrevisaoHoraComboBox);
        }
        else
        {
            parent.Children.Add(_chavesPrevisaoHoraComboBox);
        }

        SyncChavesTimeDropdownToHiddenTextBox();
    }

    private void SyncChavesTimeDropdownToHiddenTextBox()
    {
        if (_chavesPrevisaoHoraComboBox is null || _chavesPrevisaoHoraBox is null)
        {
            return;
        }

        var digits = new string((_chavesPrevisaoHoraComboBox.Text ?? string.Empty).Where(char.IsDigit).Take(4).ToArray());
        var formatted = digits.Length <= 2 ? digits : $"{digits[..2]}:{digits[2..]}";
        _chavesPrevisaoHoraBox.Text = formatted;
    }

    private void NormalizeChavesTimeDropdown()
    {
        if (_chavesPrevisaoHoraComboBox is null || _chavesPrevisaoHoraBox is null)
        {
            return;
        }

        var digits = new string((_chavesPrevisaoHoraComboBox.Text ?? string.Empty).Where(char.IsDigit).Take(4).ToArray());
        if (digits.Length == 0)
        {
            _chavesPrevisaoHoraComboBox.Text = "18:00";
            _chavesPrevisaoHoraBox.Text = "18:00";
            return;
        }

        if (digits.Length <= 2 && int.TryParse(digits, out var hourOnly))
        {
            hourOnly = Math.Clamp(hourOnly, 0, 23);
            _chavesPrevisaoHoraComboBox.Text = $"{hourOnly:00}:00";
            _chavesPrevisaoHoraBox.Text = _chavesPrevisaoHoraComboBox.Text;
            return;
        }

        var hour = int.TryParse(digits[..2], out var parsedHour) ? Math.Clamp(parsedHour, 0, 23) : 18;
        var minuteText = digits.Length > 2 ? digits[2..] : "00";
        var minute = int.TryParse(minuteText, out var parsedMinute) ? Math.Clamp(parsedMinute, 0, 59) : 0;
        _chavesPrevisaoHoraComboBox.Text = $"{hour:00}:{minute:00}";
        _chavesPrevisaoHoraBox.Text = _chavesPrevisaoHoraComboBox.Text;
    }
}

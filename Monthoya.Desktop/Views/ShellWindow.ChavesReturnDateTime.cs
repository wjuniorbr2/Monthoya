using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesReturnDateTimeRegistered = RegisterChavesReturnDateTime();
    private bool _chavesReturnDateTimeApplied;
    private DatePicker? _chavesDevolucaoDataBox;
    private ComboBox? _chavesDevolucaoHoraBox;

    private static bool RegisterChavesReturnDateTime()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesReturnDateTime));

        return true;
    }

    private static void OnShellWindowLoadedForChavesReturnDateTime(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesReturnDateTime, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyChavesReturnDateTime()
    {
        if (_chavesReturnDateTimeApplied)
        {
            return;
        }

        if (ChavesDevolvidoParaBox.Parent is not FrameworkElement receivedContainer
            || receivedContainer.Parent is not WrapPanel wrap)
        {
            Dispatcher.BeginInvoke(ApplyChavesReturnDateTime, DispatcherPriority.ApplicationIdle);
            return;
        }

        _chavesReturnDateTimeApplied = true;

        _chavesDevolucaoDataBox = new DatePicker
        {
            Width = 125,
            Margin = new Thickness(0, 6, 6, 0),
            SelectedDate = DateTime.Today
        };

        _chavesDevolucaoHoraBox = new ComboBox
        {
            Width = 76,
            Margin = new Thickness(0, 6, 0, 0),
            IsEditable = true,
            ItemsSource = Enumerable.Range(0, 24).Select(hour => $"{hour:00}:00").ToList(),
            Text = DateTime.Now.ToString("HH:00"),
            ToolTip = "Digite o horário ou escolha uma hora da lista"
        };
        _chavesDevolucaoHoraBox.PreviewTextInput += (_, input) => input.Handled = input.Text.Any(ch => !char.IsDigit(ch));
        _chavesDevolucaoHoraBox.LostFocus += (_, _) => NormalizeReturnTimeDropdown();

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(_chavesDevolucaoDataBox);
        row.Children.Add(_chavesDevolucaoHoraBox);

        var container = new StackPanel
        {
            Width = 210,
            Margin = new Thickness(0, 0, 14, 12)
        };
        container.Children.Add(new TextBlock
        {
            Text = "Data/hora da devolução",
            FontWeight = FontWeights.SemiBold
        });
        container.Children.Add(row);

        var insertIndex = wrap.Children.IndexOf(receivedContainer) + 1;
        wrap.Children.Insert(insertIndex, container);
    }

    private void NormalizeReturnTimeDropdown()
    {
        if (_chavesDevolucaoHoraBox is null)
        {
            return;
        }

        var digits = new string((_chavesDevolucaoHoraBox.Text ?? string.Empty).Where(char.IsDigit).Take(4).ToArray());
        if (digits.Length == 0)
        {
            _chavesDevolucaoHoraBox.Text = DateTime.Now.ToString("HH:00");
            return;
        }

        if (digits.Length <= 2 && int.TryParse(digits, out var hourOnly))
        {
            _chavesDevolucaoHoraBox.Text = $"{Math.Clamp(hourOnly, 0, 23):00}:00";
            return;
        }

        var hour = int.TryParse(digits[..2], out var parsedHour) ? Math.Clamp(parsedHour, 0, 23) : DateTime.Now.Hour;
        var minuteText = digits.Length > 2 ? digits[2..] : "00";
        var minute = int.TryParse(minuteText, out var parsedMinute) ? Math.Clamp(parsedMinute, 0, 59) : 0;
        _chavesDevolucaoHoraBox.Text = $"{hour:00}:{minute:00}";
    }
}

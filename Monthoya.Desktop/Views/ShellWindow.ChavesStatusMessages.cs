using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ShowChavesStatusMessage(string message)
    {
        ChavesErrorText.Foreground = Brushes.DimGray;
        ChavesErrorText.Text = message;
    }
}

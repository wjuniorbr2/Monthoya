using System.Windows;

namespace Monthoya.Desktop.Views;

public partial class ConfigurationWindow : Window
{
    public ConfigurationWindow()
    {
        InitializeComponent();
    }

    public void SetMessage(string message)
    {
        MessageText.Text = message;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool ShowPessoaDocumentoInformationDialog(string title, string message)
    {
        var workArea = SystemParameters.WorkArea;
        var dialog = new Window
        {
            Owner = this,
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
            SizeToContent = SizeToContent.Height,
            Width = Math.Min(760, Math.Max(520, workArea.Width - 120)),
            MaxHeight = Math.Max(420, workArea.Height - 80),
            Background = TryFindResource("SoftBrush") as Brush ?? Brushes.White,
            ShowInTaskbar = false
        };

        var root = new Grid { Margin = new Thickness(18) };
        var card = new Border
        {
            Style = TryFindResource("CardBorder") as Style,
            Padding = new Thickness(18),
            Background = TryFindResource("CardBrush") as Brush ?? Brushes.White,
            BorderBrush = TryFindResource("LineBrush") as Brush ?? Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Child = BuildPessoaDocumentoInformationDialogContent(dialog, title, message, workArea)
        };

        root.Children.Add(card);
        dialog.Content = root;
        return dialog.ShowDialog() == true;
    }

    private UIElement BuildPessoaDocumentoInformationDialogContent(Window dialog, string title, string message, System.Windows.Rect workArea)
    {
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(heading, 0);
        layout.Children.Add(heading);

        var contentText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 19
        };

        var scrollViewer = new ScrollViewer
        {
            Content = contentText,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = Math.Max(220, workArea.Height - 260),
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(scrollViewer, 1);
        layout.Children.Add(scrollViewer);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = TryFindResource("SecondaryButton") as Style,
            MinWidth = 100,
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;

        var applyButton = new Button
        {
            Content = "Aplicar informações",
            Style = TryFindResource("PrimaryButton") as Style,
            MinWidth = 150,
            IsDefault = true
        };
        applyButton.Click += (_, _) => dialog.DialogResult = true;

        buttons.Children.Add(cancelButton);
        buttons.Children.Add(applyButton);
        Grid.SetRow(buttons, 2);
        layout.Children.Add(buttons);

        return layout;
    }
}

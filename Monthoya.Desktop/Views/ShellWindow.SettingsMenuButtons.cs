using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private StackPanel? _settingsMenuButtonsPanel;

    private void ShowSettingsMenuButtons()
    {
        ModuleSearchBox.Visibility = Visibility.Collapsed;
        ModuleGrid.Visibility = Visibility.Collapsed;
        ModulePrimaryActionButton.Visibility = Visibility.Collapsed;

        if (_settingsMenuButtonsPanel is null)
        {
            _settingsMenuButtonsPanel = BuildSettingsMenuButtonsPanel();
            if (ModuleGrid.Parent is Panel parent)
            {
                parent.Children.Add(_settingsMenuButtonsPanel);
                Grid.SetRow(_settingsMenuButtonsPanel, Grid.GetRow(ModuleGrid));
            }
        }

        _settingsMenuButtonsPanel.Visibility = Visibility.Visible;
    }

    private void HideSettingsMenuButtons()
    {
        ModuleSearchBox.Visibility = Visibility.Visible;
        ModuleGrid.Visibility = Visibility.Visible;
        ModulePrimaryActionButton.Visibility = Visibility.Visible;

        if (_settingsMenuButtonsPanel is not null)
        {
            _settingsMenuButtonsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private StackPanel BuildSettingsMenuButtonsPanel()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 0)
        };

        panel.Children.Add(CreateSettingsMenuButton(
            "Índices de reajuste",
            "Configure IGP-M, IPCA, INPC e índices personalizados usados nos contratos.",
            "Abrir índices",
            (_, _) => MessageBox.Show(this, "A tela própria dos índices será separada na próxima etapa.", "Índices de reajuste", MessageBoxButton.OK, MessageBoxImage.Information)));

        panel.Children.Add(CreateSettingsMenuButton(
            "IA / OCR inteligente",
            "Configure a chave Gemini usada para leitura inteligente de documentos digitalizados.",
            "Abrir IA / OCR",
            (_, _) => ShowAiSettingsDialog()));

        return panel;
    }

    private Button CreateSettingsMenuButton(string title, string description, string actionText, RoutedEventHandler clickHandler)
    {
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 14),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        button.Click += clickHandler;

        var border = new Border
        {
            Style = TryFindResource("CardBorder") as Style,
            Padding = new Thickness(18),
            BorderThickness = new Thickness(1)
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel { Orientation = Orientation.Vertical };
        textStack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = TryFindResource("TextBrush") as Brush ?? Brushes.Black
        });
        textStack.Children.Add(new TextBlock
        {
            Text = description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = TryFindResource("MutedBrush") as Brush,
            Margin = new Thickness(0, 6, 0, 0)
        });

        var action = new TextBlock
        {
            Text = actionText,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = TryFindResource("AccentBrush") as Brush
        };
        Grid.SetColumn(action, 1);

        root.Children.Add(textStack);
        root.Children.Add(action);
        border.Child = root;
        button.Content = border;
        return button;
    }
}

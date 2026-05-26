using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        StretchSidePanelBackground();
    }

    private void StretchSidePanelBackground()
    {
        foreach (var image in FindVisualChildren<Image>(this))
        {
            var sourceText = image.Source?.ToString();
            if (string.IsNullOrWhiteSpace(sourceText) ||
                !sourceText.Contains("Side_panel.png", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            image.Stretch = Stretch.UniformToFill;
            image.VerticalAlignment = VerticalAlignment.Stretch;
            image.HorizontalAlignment = HorizontalAlignment.Stretch;

            // The sidebar content has padding in XAML. This negative margin lets the
            // decorative background image cover the full sidebar while the menu keeps
            // its original spacing.
            image.Margin = new Thickness(-18, -190, -18, -18);
            image.IsHitTestVisible = false;
            image.SnapsToDevicePixels = true;
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}

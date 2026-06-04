using System.Windows;

namespace Monthoya.Desktop.Views;

public partial class ChangePasswordWindow
{
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        MinHeight = Math.Max(MinHeight, 460);
        Height = Math.Max(ActualHeight, 490);
        InvalidateMeasure();
        InvalidateArrange();
    }
}

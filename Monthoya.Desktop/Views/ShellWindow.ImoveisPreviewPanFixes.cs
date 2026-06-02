using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ImoveisPreviewPanRegistered = RegisterImoveisPreviewPan();

    private static bool RegisterImoveisPreviewPan()
    {
        EventManager.RegisterClassHandler(
            typeof(Window),
            LoadedEvent,
            new RoutedEventHandler(OnWindowLoadedForImoveisPreviewPan));

        return true;
    }

    private static void OnWindowLoadedForImoveisPreviewPan(object sender, RoutedEventArgs e)
    {
        if (sender is not Window { Owner: ShellWindow } previewWindow)
        {
            return;
        }

        var scrollViewer = FindImoveisPreviewScrollViewer(previewWindow);
        if (scrollViewer is null || scrollViewer.Tag as string == "ImoveisPreviewPanReady")
        {
            return;
        }

        scrollViewer.Tag = "ImoveisPreviewPanReady";
        EnableDragPan(scrollViewer);
    }

    private static ScrollViewer? FindImoveisPreviewScrollViewer(DependencyObject root)
    {
        foreach (var scrollViewer in FindVisualChildrenForImoveisPreviewPan<ScrollViewer>(root))
        {
            if (scrollViewer.Content is Image)
            {
                return scrollViewer;
            }
        }

        return null;
    }

    private static void EnableDragPan(ScrollViewer scrollViewer)
    {
        var isDragging = false;
        var dragStart = new System.Windows.Point();
        var horizontalStart = 0.0;
        var verticalStart = 0.0;
        var previousCursor = scrollViewer.Cursor;

        scrollViewer.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (scrollViewer.ScrollableWidth <= 0 && scrollViewer.ScrollableHeight <= 0)
            {
                return;
            }

            isDragging = true;
            dragStart = e.GetPosition(scrollViewer);
            horizontalStart = scrollViewer.HorizontalOffset;
            verticalStart = scrollViewer.VerticalOffset;
            previousCursor = scrollViewer.Cursor;
            scrollViewer.Cursor = Cursors.SizeAll;
            scrollViewer.CaptureMouse();
            e.Handled = true;
        };

        scrollViewer.PreviewMouseMove += (_, e) =>
        {
            if (!isDragging)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopDragPan(scrollViewer, ref isDragging, previousCursor);
                return;
            }

            var current = e.GetPosition(scrollViewer);
            var deltaX = current.X - dragStart.X;
            var deltaY = current.Y - dragStart.Y;

            scrollViewer.ScrollToHorizontalOffset(horizontalStart - deltaX);
            scrollViewer.ScrollToVerticalOffset(verticalStart - deltaY);
            e.Handled = true;
        };

        scrollViewer.PreviewMouseLeftButtonUp += (_, e) =>
        {
            if (!isDragging)
            {
                return;
            }

            StopDragPan(scrollViewer, ref isDragging, previousCursor);
            e.Handled = true;
        };

        scrollViewer.LostMouseCapture += (_, _) =>
        {
            if (isDragging)
            {
                StopDragPan(scrollViewer, ref isDragging, previousCursor);
            }
        };
    }

    private static void StopDragPan(ScrollViewer scrollViewer, ref bool isDragging, Cursor? previousCursor)
    {
        isDragging = false;
        scrollViewer.Cursor = previousCursor;
        if (scrollViewer.IsMouseCaptured)
        {
            scrollViewer.ReleaseMouseCapture();
        }
    }

    private static IEnumerable<T> FindVisualChildrenForImoveisPreviewPan<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var descendant in FindVisualChildrenForImoveisPreviewPan<T>(child))
            {
                yield return descendant;
            }
        }
    }
}

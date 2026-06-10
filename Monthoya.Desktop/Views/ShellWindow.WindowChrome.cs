using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    // Allow dragging the window from the top stripe (where tabs live).
    private void TopDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            e.Handled = true;
            ToggleWindowMaximized();
            return;
        }

        BeginWindowDrag(e);
    }

    private void BeginWindowDrag(MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            RestoreWindowForDrag(e);
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove can throw if Windows has already ended the mouse operation.
        }
    }

    private void RestoreWindowForDrag(MouseButtonEventArgs e)
    {
        var mouseInWindow = e.GetPosition(this);
        var mouseOnScreen = PointToScreen(mouseInWindow);
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is not null)
        {
            mouseOnScreen = source.CompositionTarget.TransformFromDevice.Transform(mouseOnScreen);
        }

        var horizontalRatio = ActualWidth > 0 ? mouseInWindow.X / ActualWidth : 0.5;
        var restoredWidth = RestoreBounds.Width > 0 ? RestoreBounds.Width : Width;
        var restoredHeight = RestoreBounds.Height > 0 ? RestoreBounds.Height : Height;

        WindowState = WindowState.Normal;
        Width = restoredWidth;
        Height = restoredHeight;
        Left = mouseOnScreen.X - restoredWidth * horizontalRatio;
        Top = Math.Max(0, mouseOnScreen.Y - 14);
        UpdateMaximizeButtonIcon();
    }

    public bool IsLogoutRequested { get; private set; }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideTitleBarButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleWindowMaximized();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            BeginWindowDrag(e);
        }
    }

    private static bool IsInsideTitleBarButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void TitleBarMinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TitleBarMaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowMaximized();
    }

    private void TitleBarCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShellWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeButtonIcon();
    }


    private void ShellWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
    }

    // New pessoa action handled by ResetPessoaFormForPageOpen in the Pessoas layout partial.

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetMinMaxInfo)
        {
            ApplyMaximizedWorkArea(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void ApplyMaximizedWorkArea(IntPtr hwnd, IntPtr lParam)
    {
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var workArea = monitorInfo.WorkArea;
        var monitorArea = monitorInfo.Monitor;

        minMaxInfo.MaxPosition.X = Math.Abs(workArea.Left - monitorArea.Left);
        minMaxInfo.MaxPosition.Y = Math.Abs(workArea.Top - monitorArea.Top);
        minMaxInfo.MaxSize.X = Math.Abs(workArea.Right - workArea.Left);
        minMaxInfo.MaxSize.Y = Math.Abs(workArea.Bottom - workArea.Top);

        Marshal.StructureToPtr(minMaxInfo, lParam, true);
    }
    private void ToggleWindowMaximized()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaximizeButtonIcon();
    }

    private void UpdateMaximizeButtonIcon()
    {
        if (FindName("TitleBarMaximizeButton") is Button originalMax)
        {
            originalMax.Content = new TextBlock
            {
                Text = "□",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center
            };
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect NativeMonitor;
        public Rect NativeWorkArea;
        public int Flags;

        public NativeRect Monitor => new(NativeMonitor.Left, NativeMonitor.Top, NativeMonitor.Right, NativeMonitor.Bottom);
        public NativeRect WorkArea => new(NativeWorkArea.Left, NativeWorkArea.Top, NativeWorkArea.Right, NativeWorkArea.Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public NativeRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}

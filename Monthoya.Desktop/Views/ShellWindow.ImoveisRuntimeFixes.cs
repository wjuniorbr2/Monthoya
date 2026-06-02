using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ImoveisRuntimeFixesRegistered = RegisterImoveisRuntimeFixes();
    private bool _imoveisRuntimeFixesApplied;
    private int _imoveisSelectionVersion;

    private static bool RegisterImoveisRuntimeFixes()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForImoveisRuntimeFixes));

        return true;
    }

    private static void OnShellWindowLoadedForImoveisRuntimeFixes(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyImoveisRuntimeFixes, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyImoveisRuntimeFixes()
    {
        if (_imoveisRuntimeFixesApplied)
        {
            return;
        }

        _imoveisRuntimeFixesApplied = true;

        // Replace the original async-void handler. The original handler returns during tab-state
        // restore, which leaves media/vistorias from the previous Imóveis tab visible in the new tab.
        ImoveisGrid.SelectionChanged -= ImoveisGrid_SelectionChanged;
        ImoveisGrid.SelectionChanged += SafeImoveisGrid_SelectionChanged;

        ImovelImagensGrid.ItemContainerGenerator.StatusChanged += (_, _) =>
        {
            if (ImovelImagensGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                Dispatcher.BeginInvoke(ApplyImovelMediaPreviews, DispatcherPriority.Background);
            }
        };
    }

    private async void SafeImoveisGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectionVersion = Interlocked.Increment(ref _imoveisSelectionVersion);

        if (TryGetItemId(ImoveisGrid.SelectedItem) is not Guid imovelId)
        {
            ClearImovelSelectionMediaAndVistorias();
            if (!_isRestoringTabState)
            {
                _selectedImovelId = null;
                _selectedImovelDetails = null;
                SetImovelEditMode(true, isNew: true);
                SaveActiveTabState();
            }

            return;
        }

        try
        {
            await LoadSelectedImovelAsync(imovelId);
            if (selectionVersion != _imoveisSelectionVersion)
            {
                return;
            }

            Dispatcher.BeginInvoke(ApplyImovelMediaPreviews, DispatcherPriority.Background);

            if (!_isRestoringTabState)
            {
                SaveActiveTabState();
            }
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = $"Não foi possível carregar o imóvel selecionado: {ex.Message}";
        }
    }

    private void ClearImovelSelectionMediaAndVistorias()
    {
        _pendingImovelMedia.Clear();
        _imovelImagens = [];
        RefreshImovelMediaGrid();
        _imovelVistorias = [];
        ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
    }

    private void ApplyImovelMediaPreviews()
    {
        foreach (var image in FindVisualChildrenForPeopleRuntimeAdjustment<Image>(ImovelImagensGrid))
        {
            if (image.DataContext is not ImovelMediaListItem media)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(media.PreviewPath))
            {
                SetImageSourceIfPossible(image, media.PreviewPath);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(media.FileKind))
            {
                continue;
            }

            var storagePath = _imovelImagens
                .FirstOrDefault(saved => string.Equals(saved.FileName, media.FileName, StringComparison.OrdinalIgnoreCase))
                ?.StoragePath;

            var previewPath = ResolveLocalImovelPreviewPath(storagePath);
            if (!string.IsNullOrWhiteSpace(previewPath))
            {
                SetImageSourceIfPossible(image, previewPath);
            }
        }
    }

    private static void SetImageSourceIfPossible(Image image, string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            image.Source = bitmap;
        }
        catch
        {
            // Keep the text/icon fallback if WPF cannot decode the file.
        }
    }

    private static string? ResolveLocalImovelPreviewPath(string? storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath) || !IsImageFile(storagePath))
        {
            return null;
        }

        if (Path.IsPathRooted(storagePath) && File.Exists(storagePath))
        {
            return storagePath;
        }

        var localStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Monthoya",
            "storage",
            storagePath.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal));

        return File.Exists(localStoragePath) ? localStoragePath : null;
    }
}

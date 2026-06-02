using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;
using Monthoya.Data.RentalManagement;

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

        // Replace the original async-void handlers with tab-safe versions.
        ImoveisGrid.SelectionChanged -= ImoveisGrid_SelectionChanged;
        ImoveisGrid.SelectionChanged += SafeImoveisGrid_SelectionChanged;
        SaveImovelButton.Click -= SaveImovelButton_Click;
        SaveImovelButton.Click += SafeSaveImovelButton_Click;

        ImoveisPanel.IsVisibleChanged += (_, _) =>
        {
            if (ImoveisPanel.IsVisible)
            {
                Dispatcher.BeginInvoke(EnsureImoveisVisibleStateMatchesActiveTab, DispatcherPriority.ContextIdle);
            }
        };

        ImovelDetailsTabControl.SelectionChanged += (_, e) =>
        {
            if (ReferenceEquals(e.OriginalSource, ImovelDetailsTabControl))
            {
                ScheduleApplyImovelMediaUi();
            }
        };

        ImovelImagensGrid.ItemContainerGenerator.StatusChanged += (_, _) =>
        {
            if (ImovelImagensGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                ScheduleApplyImovelMediaUi();
            }
        };

        ImovelEditButton.Click += (_, _) => ScheduleApplyImovelMediaUi();
        CancelImovelEditButton.Click += (_, _) => ScheduleApplyImovelMediaUi();
    }

    private void EnsureImoveisVisibleStateMatchesActiveTab()
    {
        if (_activeTab?.Page != ShellPage.Imoveis)
        {
            return;
        }

        var selectedIdForThisTab = _activeTab.PageStates.TryGetValue(ShellPage.Imoveis, out var state)
            && state is ImoveisPageState imoveisState
                ? imoveisState.SelectedImovelId
                : null;

        if (selectedIdForThisTab.HasValue)
        {
            ScheduleApplyImovelMediaUi();
            return;
        }

        _selectedImovelId = null;
        _selectedImovelDetails = null;
        ImoveisGrid.SelectedItem = null;
        ClearImovelSelectionMediaAndVistorias();
        ClearImovelForm();
        SetImovelEditMode(true, isNew: true);
        SetActiveImovelTabLabel("Criar novo");
        RenderTabs();
        ScheduleApplyImovelMediaUi();
    }

    private async void SafeSaveImovelButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelErrorText.Text = string.Empty;

        try
        {
            var request = BuildImovelRequestFromForm();
            var saved = _selectedImovelId.HasValue
                ? await _rentalManagementService.UpdateImovelAsync(new UpdateImovelRequest(_selectedImovelId.Value, request))
                : await _rentalManagementService.CreateImovelAsync(request);

            var savedImovelId = saved.Id;
            await SavePendingImovelMediaAsync(savedImovelId);
            _pendingImovelMedia.Clear();

            _selectedImovelId = savedImovelId;
            await LoadImoveisAsync();
            RestoreDataGridSelection(ImoveisGrid, savedImovelId);
            await LoadSelectedImovelAsync(savedImovelId);
            SaveActiveTabState();
            ScheduleApplyImovelMediaUi();
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = ex.Message;
        }
    }

    private async void SafeImoveisGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectionVersion = Interlocked.Increment(ref _imoveisSelectionVersion);

        if (TryGetItemId(ImoveisGrid.SelectedItem) is not Guid imovelId)
        {
            _selectedImovelId = null;
            _selectedImovelDetails = null;
            ClearImovelSelectionMediaAndVistorias();
            SetImovelEditMode(true, isNew: true);
            SetActiveImovelTabLabel("Criar novo");

            if (!_isRestoringTabState)
            {
                SaveActiveTabState();
            }

            ScheduleApplyImovelMediaUi();
            return;
        }

        try
        {
            await LoadSelectedImovelAsync(imovelId);
            if (selectionVersion != _imoveisSelectionVersion)
            {
                return;
            }

            ScheduleApplyImovelMediaUi();

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

    private void ScheduleApplyImovelMediaUi()
    {
        Dispatcher.BeginInvoke(ApplyImovelMediaUi, DispatcherPriority.Background);
        Dispatcher.BeginInvoke(ApplyImovelMediaUi, DispatcherPriority.ContextIdle);
        _ = ApplyImovelMediaUiAfterDelayAsync(150);
        _ = ApplyImovelMediaUiAfterDelayAsync(450);
    }

    private async Task ApplyImovelMediaUiAfterDelayAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
        await Dispatcher.InvokeAsync(ApplyImovelMediaUi, DispatcherPriority.ContextIdle);
    }

    private void ApplyImovelMediaUi()
    {
        ApplyImovelMediaPreviews();
        ApplyImovelMediaCardActions();
    }

    private void ApplyImovelMediaPreviews()
    {
        foreach (var image in FindVisualChildrenForPeopleRuntimeAdjustment<Image>(ImovelImagensGrid))
        {
            if (image.DataContext is not ImovelMediaListItem media)
            {
                continue;
            }

            var previewPath = ResolveMediaPreviewPath(media);
            if (!string.IsNullOrWhiteSpace(previewPath))
            {
                SetImageSourceIfPossible(image, previewPath);
            }
        }
    }

    private void ApplyImovelMediaCardActions()
    {
        foreach (var image in FindVisualChildrenForPeopleRuntimeAdjustment<Image>(ImovelImagensGrid))
        {
            if (image.DataContext is not ImovelMediaListItem media)
            {
                continue;
            }

            var card = FindAncestorWithSize<Border>(image, 142, 156);
            if (card is not null && card.Tag as string != "ImovelMediaCardReady")
            {
                card.Tag = "ImovelMediaCardReady";
                card.Cursor = Cursors.Hand;
                card.MouseLeftButtonUp += ImovelMediaCard_MouseLeftButtonUp;
                card.ToolTip = "Clique para ampliar";
            }

            if (card?.Child is StackPanel stackPanel)
            {
                ApplyMediaCaptionToCard(stackPanel, media);
            }

            var previewGrid = FindAncestor<Grid>(image);
            if (previewGrid is null)
            {
                continue;
            }

            var existingRemoveButton = previewGrid.Children
                .OfType<Button>()
                .FirstOrDefault(button => button.Tag as string == "RemoveImovelMediaButton");

            if (existingRemoveButton is not null)
            {
                existingRemoveButton.Visibility = _isImovelEditing ? Visibility.Visible : Visibility.Collapsed;
                existingRemoveButton.IsEnabled = _isImovelEditing;
                existingRemoveButton.DataContext = media;
                continue;
            }

            var removeButton = new Button
            {
                Content = "×",
                Width = 22,
                Height = 22,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 3, 0),
                FontWeight = FontWeights.Bold,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                Foreground = Brushes.DarkRed,
                ToolTip = "Remover esta mídia",
                Tag = "RemoveImovelMediaButton",
                DataContext = media,
                Visibility = _isImovelEditing ? Visibility.Visible : Visibility.Collapsed,
                IsEnabled = _isImovelEditing
            };
            removeButton.Click += RemoveImovelMediaButton_Click;
            previewGrid.Children.Add(removeButton);
        }
    }

    private static void ApplyMediaCaptionToCard(StackPanel stackPanel, ImovelMediaListItem media)
    {
        var captionText = stackPanel.Children
            .OfType<TextBlock>()
            .FirstOrDefault(textBlock => textBlock.Tag as string == "ImovelMediaCaptionText");

        if (captionText is null)
        {
            captionText = new TextBlock
            {
                Tag = "ImovelMediaCaptionText",
                Foreground = Brushes.DimGray,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            stackPanel.Children.Insert(Math.Min(2, stackPanel.Children.Count), captionText);
        }

        captionText.Text = string.IsNullOrWhiteSpace(media.Caption)
            ? "Sem legenda"
            : media.Caption;
        captionText.Visibility = Visibility.Visible;
    }

    private void ImovelMediaCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) is not null)
        {
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is not ImovelMediaListItem media)
        {
            return;
        }

        var previewPath = ResolveMediaPreviewPath(media);
        if (string.IsNullOrWhiteSpace(previewPath))
        {
            MessageBox.Show(this, "Pré-visualização disponível apenas para imagens locais salvas neste computador.", "Pré-visualização", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ShowImovelMediaPreviewWindow(media, previewPath);
    }

    private async void RemoveImovelMediaButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (!_isImovelEditing)
        {
            MessageBox.Show(this, "Clique em Editar antes de remover fotos ou arquivos.", "Remover mídia", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is not ImovelMediaListItem media)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            $"Remover '{media.FileName}' da lista de fotos e arquivos?",
            "Remover mídia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await RemoveImovelMediaFromCurrentListAsync(media);
            RefreshImovelMediaGrid();
            ScheduleApplyImovelMediaUi();
        }
        catch (Exception ex)
        {
            ImovelImagemErrorText.Text = $"Não foi possível remover a mídia: {ex.Message}";
        }
    }

    private async Task RemoveImovelMediaFromCurrentListAsync(ImovelMediaListItem media)
    {
        var removedPending = _pendingImovelMedia.RemoveAll(pending =>
            string.Equals(Path.GetFileName(pending.StoragePath), media.FileName, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removedPending)
        {
            ImovelImagemErrorText.Text = "Mídia pendente removida.";
            return;
        }

        var saved = _imovelImagens
            .FirstOrDefault(saved => string.Equals(saved.FileName, media.FileName, StringComparison.OrdinalIgnoreCase));
        if (saved is null)
        {
            return;
        }

        await _rentalManagementService.DeleteImovelImagemRecordAsync(saved.Id);
        _imovelImagens = _imovelImagens.Where(x => x.Id != saved.Id).ToList();
        ImovelImagemErrorText.Text = "Mídia removida do cadastro.";
    }

    private void ShowImovelMediaPreviewWindow(ImovelMediaListItem media, string previewPath)
    {
        var scale = new ScaleTransform(1, 1);
        var image = new Image
        {
            Stretch = Stretch.Uniform,
            Margin = new Thickness(12),
            LayoutTransform = scale
        };
        SetImageSourceIfPossible(image, previewPath);

        var zoomText = new TextBlock
        {
            Text = "100%",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0),
            Foreground = Brushes.DimGray
        };

        void ApplyZoom(double newZoom)
        {
            newZoom = Math.Clamp(newZoom, 0.25, 5.0);
            scale.ScaleX = newZoom;
            scale.ScaleY = newZoom;
            zoomText.Text = $"{newZoom * 100:0}%";
        }

        var zoomOutButton = new Button
        {
            Content = "−",
            Width = 34,
            Height = 30,
            ToolTip = "Diminuir zoom"
        };
        zoomOutButton.Click += (_, _) => ApplyZoom(scale.ScaleX - 0.25);

        var zoomInButton = new Button
        {
            Content = "+",
            Width = 34,
            Height = 30,
            Margin = new Thickness(6, 0, 0, 0),
            ToolTip = "Aumentar zoom"
        };
        zoomInButton.Click += (_, _) => ApplyZoom(scale.ScaleX + 0.25);

        var resetButton = new Button
        {
            Content = "100%",
            Height = 30,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(10, 0, 10, 0),
            ToolTip = "Restaurar zoom"
        };
        resetButton.Click += (_, _) => ApplyZoom(1);

        var hintText = new TextBlock
        {
            Text = "Ctrl + roda do mouse também altera o zoom",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(12, 0, 0, 0)
        };

        var toolbar = new DockPanel
        {
            LastChildFill = false,
            Margin = new Thickness(10)
        };
        toolbar.Children.Add(zoomOutButton);
        toolbar.Children.Add(zoomInButton);
        toolbar.Children.Add(resetButton);
        toolbar.Children.Add(zoomText);
        toolbar.Children.Add(hintText);

        var caption = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(media.Caption) ? string.Empty : media.Caption,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(12, 0, 12, 8),
            TextWrapping = TextWrapping.Wrap
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = image
        };
        scrollViewer.PreviewMouseWheel += (_, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                return;
            }

            e.Handled = true;
            ApplyZoom(scale.ScaleX + (e.Delta > 0 ? 0.15 : -0.15));
        };

        var layout = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(caption, Dock.Top);
        layout.Children.Add(toolbar);
        if (!string.IsNullOrWhiteSpace(media.Caption))
        {
            layout.Children.Add(caption);
        }
        layout.Children.Add(scrollViewer);

        var window = new Window
        {
            Title = media.Caption is { Length: > 0 } ? media.Caption : media.FileName,
            Owner = this,
            Width = 920,
            Height = 680,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brushes.White,
            Content = layout
        };

        window.ShowDialog();
    }

    private string? ResolveMediaPreviewPath(ImovelMediaListItem media)
    {
        if (!string.IsNullOrWhiteSpace(media.PreviewPath))
        {
            return media.PreviewPath;
        }

        var savedStoragePath = _imovelImagens
            .FirstOrDefault(saved => string.Equals(saved.FileName, media.FileName, StringComparison.OrdinalIgnoreCase))
            ?.StoragePath;
        var savedPreviewPath = ResolveLocalImovelPreviewPath(savedStoragePath);
        if (!string.IsNullOrWhiteSpace(savedPreviewPath))
        {
            return savedPreviewPath;
        }

        var pendingStoragePath = _pendingImovelMedia
            .FirstOrDefault(pending => string.Equals(Path.GetFileName(pending.StoragePath), media.FileName, StringComparison.OrdinalIgnoreCase))
            ?.StoragePath;
        return ResolveLocalImovelPreviewPath(pendingStoragePath);
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

    private static T? FindAncestor<T>(DependencyObject child)
        where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindAncestorWithSize<T>(DependencyObject child, double width, double height)
        where T : FrameworkElement
    {
        var current = child;
        while (current is not null)
        {
            if (current is T typed && Math.Abs(typed.Width - width) < 0.1 && Math.Abs(typed.Height - height) < 0.1)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}

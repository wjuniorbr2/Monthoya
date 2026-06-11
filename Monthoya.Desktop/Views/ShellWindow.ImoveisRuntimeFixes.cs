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
            ImovelErrorText.Text = GetImovelExceptionMessage(ex);
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
            ImovelErrorText.Text = $"NÃ£o foi possÃ­vel carregar o imÃ³vel selecionado: {ex.Message}";
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

}

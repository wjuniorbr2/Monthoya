using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasRuntimeAdjustmentsApplied;
    private bool _newPessoaButtonResetHandlerApplied;
    private readonly SemaphoreSlim _pessoasSelectionSemaphore = new(1, 1);
    private int _pessoasSelectionVersion;

    static ShellWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPeopleRuntimeAdjustments));
    }

    private static void OnShellWindowLoadedForPeopleRuntimeAdjustments(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPeopleRuntimeAdjustments, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyPeopleRuntimeAdjustments()
    {
        if (_pessoasRuntimeAdjustmentsApplied)
        {
            return;
        }

        _pessoasRuntimeAdjustmentsApplied = true;

        // Replace the original async-void selection handler with a serialized one.
        // This prevents fast clicks from starting two database reads on the same DbContext.
        PessoasGrid.SelectionChanged -= PessoasGrid_SelectionChanged;
        PessoasGrid.SelectionChanged += SafePessoasGrid_SelectionChanged;

        PessoasGrid.SelectionChanged += (_, _) =>
            Dispatcher.BeginInvoke(UpdatePeopleTopRowSpacingAndRolesVisibility, DispatcherPriority.Background);

        PessoasPanel.IsVisibleChanged += (_, _) =>
            Dispatcher.BeginInvoke(UpdatePeopleTopRowSpacingAndRolesVisibility, DispatcherPriority.Background);

        AttachNewPessoaButtonResetHandler();
        ApplyTitleBarCaptionButtonColors();
        UpdatePeopleTopRowSpacingAndRolesVisibility();
    }

    private void AttachNewPessoaButtonResetHandler()
    {
        if (_newPessoaButtonResetHandlerApplied)
        {
            return;
        }

        var newPessoaButton = FindVisualChildrenForPeopleRuntimeAdjustment<Button>(PessoasPanel)
            .FirstOrDefault(button => string.Equals(button.Content as string, "Novo", StringComparison.Ordinal));

        if (newPessoaButton is null)
        {
            return;
        }

        _newPessoaButtonResetHandlerApplied = true;
        newPessoaButton.Click += (_, _) =>
            Dispatcher.BeginInvoke(ClearActivePessoaTabSecondaryText, DispatcherPriority.Background);
    }

    private void ClearActivePessoaTabSecondaryText()
    {
        if (_activeTab?.Page != ShellPage.Pessoas)
        {
            return;
        }

        _activeTab.SelectedPessoaName = "Criar Novo";
        _selectedPessoaDetails = null;
        RenderTabs();
        SaveActiveTabState();
    }

    private void InvalidatePessoaSelectionLoads()
    {
        Interlocked.Increment(ref _pessoasSelectionVersion);
    }

    private void ApplyTitleBarCaptionButtonColors()
    {
        ForceTitleBarCaptionButtonTextBlack(TitleBarMinimizeButtonTopRight);
        ForceTitleBarCaptionButtonTextBlack(TitleBarMaximizeButton);

        foreach (var button in FindVisualChildrenForPeopleRuntimeAdjustment<Button>(this))
        {
            if (button == TitleBarMinimizeButtonTopRight
                || button == TitleBarMaximizeButton
                || string.Equals(button.ToolTip as string, "Fechar", StringComparison.Ordinal)
                || string.Equals(button.ToolTip as string, "Minimizar", StringComparison.Ordinal)
                || string.Equals(button.ToolTip as string, "Maximizar/Restaurar", StringComparison.Ordinal)
                || string.Equals(button.Content as string, "\uE921", StringComparison.Ordinal)
                || string.Equals(button.Content as string, "\uE922", StringComparison.Ordinal)
                || string.Equals(button.Content as string, "\uE923", StringComparison.Ordinal)
                || string.Equals(button.Content as string, "\uE8BB", StringComparison.Ordinal))
            {
                button.Foreground = Brushes.Black;
                foreach (var textBlock in FindVisualChildrenForPeopleRuntimeAdjustment<TextBlock>(button))
                {
                    textBlock.Foreground = Brushes.Black;
                }
            }
        }
    }

    private static void ForceTitleBarCaptionButtonTextBlack(Button? button)
    {
        if (button is null)
        {
            return;
        }

        button.Foreground = Brushes.Black;
        foreach (var textBlock in FindVisualChildrenForPeopleRuntimeAdjustment<TextBlock>(button))
        {
            textBlock.Foreground = Brushes.Black;
        }
    }

    private async void SafePessoasGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRestoringTabState)
        {
            _ = Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(50);
                await LoadCurrentPessoaSelectionAsync();
            }, DispatcherPriority.ContextIdle);
            return;
        }

        await LoadCurrentPessoaSelectionAsync();
    }

    private async Task LoadCurrentPessoaSelectionAsync()
    {
        var selectionVersion = Interlocked.Increment(ref _pessoasSelectionVersion);

        await _pessoasSelectionSemaphore.WaitAsync();
        try
        {
            if (selectionVersion != _pessoasSelectionVersion)
            {
                return;
            }

            if (PessoasGrid.SelectedItem is not PessoaSummary pessoa)
            {
                SetPessoaDocumentoSelection(null);
                _selectedPessoaDetails = null;
                ClearActivePessoaTabSecondaryText();
                await LoadPessoaDocumentosAsync(null);
                SaveActiveTabState();
                return;
            }

            SetPessoaDocumentoSelection(pessoa);
            var pessoaId = pessoa.Id;
            var details = await _rentalManagementService.GetPessoaAsync(pessoaId);

            if (selectionVersion != _pessoasSelectionVersion)
            {
                return;
            }

            _selectedPessoaDetails = details;
            if (details is not null)
            {
                PopulatePessoaForm(details);
                SetPessoaEditMode(false, isNew: false);
            }

            if (_activeTab is not null)
            {
                _activeTab.SelectedPessoaName = pessoa.Nome ?? string.Empty;
                RenderTabs();
            }

            await LoadPessoaDocumentosAsync(pessoaId);
            SaveActiveTabState();
        }
        catch (Exception ex)
        {
            PessoaErrorText.Text = $"Não foi possível carregar a pessoa selecionada: {ex.Message}";
        }
        finally
        {
            _pessoasSelectionSemaphore.Release();
        }
    }

    private void UpdatePeopleTopRowSpacingAndRolesVisibility()
    {
        var hasSelectedPerson = PessoasGrid.SelectedItem is not null;
        var rolesVisibility = hasSelectedPerson ? Visibility.Visible : Visibility.Collapsed;

        if (_pessoaRolesTopCell is not null)
        {
            _pessoaRolesTopCell.Visibility = rolesVisibility;
        }

        if (_pessoaRolesCell is not null)
        {
            _pessoaRolesCell.Visibility = rolesVisibility;
        }

        var topGrid = FindPessoaTopInfoGridForRuntimeAdjustment();
        if (topGrid is null)
        {
            return;
        }

        foreach (var cell in topGrid.Children.OfType<StackPanel>())
        {
            var label = cell.Children.OfType<TextBlock>().FirstOrDefault()?.Text;
            if (label == "Funções atuais")
            {
                cell.Visibility = rolesVisibility;
            }
            else if (label == "Tipo")
            {
                cell.Margin = hasSelectedPerson ? new Thickness(18, 0, 0, 0) : new Thickness(0);
            }
        }
    }

    private Grid? FindPessoaTopInfoGridForRuntimeAdjustment()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Grid>(PessoasPanel)
            .FirstOrDefault(grid => grid.Tag as string == "PessoaTopInfoGrid");
    }

    private static IEnumerable<T> FindVisualChildrenForPeopleRuntimeAdjustment<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildrenForPeopleRuntimeAdjustment<T>(child))
            {
                yield return descendant;
            }
        }
    }
}

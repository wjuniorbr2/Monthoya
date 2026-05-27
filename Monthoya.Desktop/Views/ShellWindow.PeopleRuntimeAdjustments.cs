using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasRuntimeAdjustmentsApplied;
    private readonly SemaphoreSlim _pessoasSelectionSemaphore = new(1, 1);
    private readonly Dictionary<Guid, ShellTabUiState> _tabUiStates = [];
    private int _pessoasSelectionVersion;
    private bool _isRestoringPessoaTabSelection;

    private sealed class ShellTabUiState
    {
        public ShellPage Page { get; set; }
        public Dictionary<string, string?> TextValues { get; } = [];
        public Dictionary<string, bool?> CheckValues { get; } = [];
        public Dictionary<string, object?> SelectedValues { get; } = [];
        public Dictionary<string, int> SelectedIndexes { get; } = [];
        public Dictionary<string, DateTime?> DateValues { get; } = [];
        public Dictionary<string, Guid?> SelectedRows { get; } = [];
    }

    static ShellWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPeopleRuntimeAdjustments));

        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            Button.ClickEvent,
            new RoutedEventHandler(OnShellWindowButtonClickForTabState),
            true);
    }

    private static void OnShellWindowLoadedForPeopleRuntimeAdjustments(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPeopleRuntimeAdjustments, DispatcherPriority.ContextIdle);
        }
    }

    private static void OnShellWindowButtonClickForTabState(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.SaveActiveTabUiState();
            window.QueueRestoreActiveTabUiState();
        }
    }

    private async void QueueRestoreActiveTabUiState()
    {
        await Task.Delay(350);
        await RestoreActiveTabUiStateAsync();
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
        {
            Dispatcher.BeginInvoke(UpdatePeopleTopRowSpacingAndRolesVisibility, DispatcherPriority.Background);
            _ = RestoreActiveTabUiStateAsync();
        };

        UpdatePeopleTopRowSpacingAndRolesVisibility();
    }

    private async void SafePessoasGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                if (!_isRestoringPessoaTabSelection && _activeTab is not null && _activeTab.Page == ShellPage.Pessoas)
                {
                    SaveActiveTabUiState();
                }

                SetPessoaDocumentoSelection(null);
                await LoadPessoaDocumentosAsync(null);
                return;
            }

            if (!_isRestoringPessoaTabSelection && _activeTab is not null && _activeTab.Page == ShellPage.Pessoas)
            {
                SaveActiveTabUiState();
            }

            SetPessoaDocumentoSelection(pessoa);
            var details = await _rentalManagementService.GetPessoaAsync(pessoa.Id);

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

            await LoadPessoaDocumentosAsync(pessoa.Id);
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

    private void SaveActiveTabUiState()
    {
        if (_activeTab is null)
        {
            return;
        }

        var state = new ShellTabUiState { Page = _activeTab.Page };

        foreach (var textBox in FindNamedVisualChildren<TextBox>(this))
        {
            state.TextValues[textBox.Name] = textBox.Text;
        }

        foreach (var comboBox in FindNamedVisualChildren<ComboBox>(this))
        {
            state.SelectedValues[comboBox.Name] = comboBox.SelectedValue;
            state.SelectedIndexes[comboBox.Name] = comboBox.SelectedIndex;
            state.TextValues[comboBox.Name] = comboBox.Text;
        }

        foreach (var checkBox in FindNamedVisualChildren<CheckBox>(this))
        {
            state.CheckValues[checkBox.Name] = checkBox.IsChecked;
        }

        foreach (var datePicker in FindNamedVisualChildren<DatePicker>(this))
        {
            state.DateValues[datePicker.Name] = datePicker.SelectedDate;
            state.TextValues[datePicker.Name] = datePicker.Text;
        }

        foreach (var dataGrid in FindNamedVisualChildren<DataGrid>(this))
        {
            state.SelectedRows[dataGrid.Name] = TryGetItemId(dataGrid.SelectedItem);
        }

        _tabUiStates[_activeTab.Id] = state;
    }

    private async Task RestoreActiveTabUiStateAsync()
    {
        if (_activeTab is null)
        {
            return;
        }

        await Task.Delay(120);

        if (_activeTab is null || !_tabUiStates.TryGetValue(_activeTab.Id, out var state))
        {
            // A new Pessoas tab should start blank instead of inheriting the last selected person
            // from another tab, because the Pessoas view controls are physically shared.
            if (_activeTab is not null && _activeTab.Page == ShellPage.Pessoas)
            {
                _isRestoringPessoaTabSelection = true;
                PessoasGrid.SelectedItem = null;
                ResetPessoaFormForPageOpen();
                _isRestoringPessoaTabSelection = false;
            }

            return;
        }

        if (state.Page != _activeTab.Page)
        {
            return;
        }

        foreach (var textBox in FindNamedVisualChildren<TextBox>(this))
        {
            if (state.TextValues.TryGetValue(textBox.Name, out var value))
            {
                textBox.Text = value ?? string.Empty;
            }
        }

        foreach (var comboBox in FindNamedVisualChildren<ComboBox>(this))
        {
            if (state.SelectedValues.TryGetValue(comboBox.Name, out var selectedValue))
            {
                comboBox.SelectedValue = selectedValue;
            }
            else if (state.SelectedIndexes.TryGetValue(comboBox.Name, out var selectedIndex))
            {
                comboBox.SelectedIndex = selectedIndex;
            }

            if (state.TextValues.TryGetValue(comboBox.Name, out var text))
            {
                comboBox.Text = text ?? string.Empty;
            }
        }

        foreach (var checkBox in FindNamedVisualChildren<CheckBox>(this))
        {
            if (state.CheckValues.TryGetValue(checkBox.Name, out var value))
            {
                checkBox.IsChecked = value;
            }
        }

        foreach (var datePicker in FindNamedVisualChildren<DatePicker>(this))
        {
            if (state.DateValues.TryGetValue(datePicker.Name, out var value))
            {
                datePicker.SelectedDate = value;
            }

            if (state.TextValues.TryGetValue(datePicker.Name, out var text))
            {
                datePicker.Text = text ?? string.Empty;
            }
        }

        _isRestoringPessoaTabSelection = true;
        foreach (var dataGrid in FindNamedVisualChildren<DataGrid>(this))
        {
            if (state.SelectedRows.TryGetValue(dataGrid.Name, out var selectedId))
            {
                RestoreDataGridSelection(dataGrid, selectedId);
            }
        }
        _isRestoringPessoaTabSelection = false;

        UpdatePeopleTopRowSpacingAndRolesVisibility();
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

    private static void RestoreDataGridSelection(DataGrid dataGrid, Guid? selectedId)
    {
        if (selectedId is null)
        {
            dataGrid.SelectedItem = null;
            return;
        }

        foreach (var item in dataGrid.ItemsSource ?? dataGrid.Items)
        {
            if (TryGetItemId(item) == selectedId.Value)
            {
                dataGrid.SelectedItem = item;
                dataGrid.ScrollIntoView(item);
                return;
            }
        }
    }

    private static Guid? TryGetItemId(object? item)
    {
        if (item is null)
        {
            return null;
        }

        var property = item.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
        if (property?.GetValue(item) is Guid id)
        {
            return id;
        }

        return null;
    }

    private static IEnumerable<T> FindNamedVisualChildren<T>(DependencyObject parent)
        where T : FrameworkElement
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<T>(parent)
            .Where(element => !string.IsNullOrWhiteSpace(element.Name));
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

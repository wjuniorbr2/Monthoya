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
                SetPessoaDocumentoSelection(null);
                await LoadPessoaDocumentosAsync(null);
                return;
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

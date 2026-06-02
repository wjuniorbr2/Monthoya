using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool PessoasFastLoadRegistered = RegisterPessoasFastLoad();
    private bool _pessoasFastLoadApplied;
    private bool _isOpeningPessoasFast;

    private static bool RegisterPessoasFastLoad()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPessoasFastLoad));

        return true;
    }

    private static void OnShellWindowLoadedForPessoasFastLoad(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPessoasFastLoad, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyPessoasFastLoad()
    {
        if (_pessoasFastLoadApplied)
        {
            return;
        }

        _pessoasFastLoadApplied = true;

        // Opening Pessoas should load the list only. Address/street suggestions must not be rebuilt
        // every time the module opens, because that causes one detailed query per person.
        PessoasNavButton.PreviewMouseLeftButtonDown += PessoasNavButton_PreviewMouseLeftButtonDownForFastOpen;
        PessoasNavButton.PreviewKeyDown += PessoasNavButton_PreviewKeyDownForFastOpen;

        // The Pessoas refresh button has no x:Name in XAML, so intercept mouse refresh before
        // the original Click handler. This keeps refresh fast and avoids rebuilding address suggestions.
        PessoasPanel.PreviewMouseLeftButtonDown += PessoasPanel_PreviewMouseLeftButtonDownForFastRefresh;
    }

    private async void PessoasNavButton_PreviewMouseLeftButtonDownForFastOpen(object sender, MouseButtonEventArgs e)
    {
        if (_isOpeningPessoasFast)
        {
            return;
        }

        e.Handled = true;
        await OpenPessoasFastAsync();
    }

    private async void PessoasNavButton_PreviewKeyDownForFastOpen(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space) || _isOpeningPessoasFast)
        {
            return;
        }

        e.Handled = true;
        await OpenPessoasFastAsync();
    }

    private async Task OpenPessoasFastAsync()
    {
        if (_isOpeningPessoasFast)
        {
            return;
        }

        try
        {
            _isOpeningPessoasFast = true;
            await UpdateActiveTabAsync(ShellPage.Pessoas, "Pessoas", loadData: false);
            await LoadPessoasFastAsync();
            await RestoreActiveTabStateAsync(ShellPage.Pessoas);
        }
        finally
        {
            _isOpeningPessoasFast = false;
        }
    }

    private async void PessoasPanel_PreviewMouseLeftButtonDownForFastRefresh(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not { Content: string content } button
            || !string.Equals(content, "Atualizar", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Handled = true;
        await ShowPessoasFastRefreshFeedbackAsync(button);
    }

    private async Task ShowPessoasFastRefreshFeedbackAsync(Button button)
    {
        var originalContent = button.Content;
        var wasEnabled = button.IsEnabled;

        try
        {
            button.Content = "Atualizando...";
            button.IsEnabled = false;
            await LoadPessoasFastAsync();
            button.Content = "Atualizado";
            await Task.Delay(500);
        }
        finally
        {
            button.Content = originalContent;
            button.IsEnabled = wasEnabled;
        }
    }

    private async Task LoadPessoasFastAsync()
    {
        _pessoas = await _rentalManagementService.GetPessoasAsync();
        ApplyPessoasFilter();
        ImovelProprietarioBox.ItemsSource = _pessoas.Where(x => x.Status == "Ativo").ToList();
    }
}

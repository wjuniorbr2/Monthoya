using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async void AddTabButton_Click(object sender, RoutedEventArgs e)
    {
        SaveActiveTabState();
        AddShellTab(ShellPage.Dashboard, "Tela Inicial");
        await ShowPageAsync(ShellPage.Dashboard, true);
    }

    private void AddShellTab(ShellPage page, string title)
    {
        var tab = new ShellTab(Guid.NewGuid(), title, page);
        _tabs.Add(tab);
        _activeTab = tab;
        RenderTabs();
    }

    private async Task UpdateActiveTabAsync(ShellPage page, string title, bool loadData)
    {
        SaveActiveTabState();
        if (_activeTab is null)
        {
            AddShellTab(page, title);
        }
        else
        {
            _activeTab.Page = page;
            _activeTab.Title = title;
            RenderTabs();
        }

        await ShowPageAsync(page, loadData);
    }

    private async Task SelectTabAsync(ShellTab tab)
    {
        SaveActiveTabState();
        _activeTab = tab;
        RenderTabs();
        await ShowPageAsync(tab.Page, true);
    }

    private void RenderTabs()
    {
        TabsPanel.Children.Clear();

        foreach (var tab in _tabs)
        {
            var tabContent = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            tabContent.Children.Add(new TextBlock
            {
                Text = GetShellPageIcon(tab.Page),
                Style = (Style)FindResource("ShellTabIconText"),
                FontFamily = tab.Page == ShellPage.Financeiro
                    ? new FontFamily("Segoe UI Black")
                    : new FontFamily("Segoe MDL2 Assets"),
                FontSize = tab.Page == ShellPage.Financeiro ? 14 : 13
            });

            // Title area: for Pessoas tabs show a stacked title + selected person name (smaller)
            if (tab.Page == ShellPage.Pessoas)
            {
                var titleStack = new StackPanel { Orientation = Orientation.Vertical };
                titleStack.Children.Add(new TextBlock
                {
                    Text = tab.Title,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, -4, 0, 0)
                });

                titleStack.Children.Add(new TextBlock
                {
                    Text = GetTabSecondaryText(tab),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("MutedBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 160
                });

                tabContent.Children.Add(titleStack);
            }
            else
            {
                tabContent.Children.Add(new TextBlock
                {
                    Text = tab.Title,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            var closeButton = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(10, 0, -4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Gray,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            closeButton.Click += async (sender, e) =>
            {
                e.Handled = true;
                await CloseTabAsync(tab);
            };

            tabContent.Children.Add(closeButton);

            var tabButton = new Button
            {
                Content = tabContent,
                Margin = tab == _activeTab
                    ? new Thickness(0, 0, 4, -1)
                    : new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Style = (Style)FindResource(tab == _activeTab ? "ShellTabButtonActive" : "ShellTabButton")
            };

            tabButton.Click += async (_, _) => await SelectTabAsync(tab);
            TabsPanel.Children.Add(tabButton);
        }
    }

    private static string GetShellPageIcon(ShellPage page) =>
        page switch
        {
            ShellPage.Dashboard => "\uE80F",
            ShellPage.Users => "\uE77B",
            ShellPage.Pessoas => "\uE716",
            ShellPage.Imoveis => "\uE80F",
            ShellPage.Locacoes => "\uE8A1",
            ShellPage.Financeiro => "$",
            ShellPage.Boletos => "\uE8C7",
            ShellPage.NotasFiscais => "\uE9D9",
            ShellPage.Documentos => "\uE8A5",
            ShellPage.Relatorios => "\uE9D2",
            ShellPage.Dimob => "\uE9F9",
            ShellPage.Manutencoes => "\uE90F",
            ShellPage.Vistorias => "\uE721",
            ShellPage.Configuracoes => "\uE713",
            ShellPage.Diagnostics => "\uE950",
            _ => "\uE80F"
        };

    private void SaveActiveTabState()
    {
        if (_activeTab is null || _isRestoringTabState)
        {
            return;
        }

        _activeTab.PageStates[_activeTab.Page] = CapturePageState(_activeTab.Page);
    }

    private IShellPageState CapturePageState(ShellPage page) =>
        page switch
        {
            ShellPage.Pessoas => CapturePessoasPageState(),
            ShellPage.Imoveis => CaptureImoveisPageState(),
            ShellPage.Users => CaptureUsersPageState(),
            _ when IsGenericModulePage(page) => CaptureModulePageState(),
            _ => NoShellPageState.Instance
        };

    private IShellPageState CreateDefaultPageState(ShellPage page) =>
        page switch
        {
            ShellPage.Pessoas => PessoasPageState.Default,
            ShellPage.Imoveis => ImoveisPageState.Default,
            ShellPage.Users => UsersPageState.Default,
            _ when IsGenericModulePage(page) => ModulePageState.Default,
            _ => NoShellPageState.Instance
        };

    private async Task RestoreActiveTabStateAsync(ShellPage page)
    {
        if (_activeTab is null)
        {
            return;
        }

        var state = _activeTab.PageStates.TryGetValue(page, out var storedState)
            ? storedState
            : CreateDefaultPageState(page);

        try
        {
            _isRestoringTabState = true;
            await RestorePageStateAsync(page, state);
        }
        finally
        {
            _isRestoringTabState = false;
        }
    }

    private Task RestorePageStateAsync(ShellPage page, IShellPageState state) =>
        page switch
        {
            ShellPage.Pessoas when state is PessoasPageState pessoasState => RestorePessoasPageStateAsync(pessoasState),
            ShellPage.Imoveis when state is ImoveisPageState imoveisState => RestoreImoveisPageStateAsync(imoveisState),
            ShellPage.Users when state is UsersPageState usersState => RestoreUsersPageStateAsync(usersState),
            _ when IsGenericModulePage(page) && state is ModulePageState moduleState => RestoreModulePageStateAsync(moduleState),
            _ => Task.CompletedTask
        };

    private async Task CloseTabAsync(ShellTab tab)
    {
        if (_tabs.Count == 1)
        {
            await UpdateActiveTabAsync(ShellPage.Dashboard, "Tela Inicial", true);
            return;
        }

        if (_activeTab == tab)
        {
            SaveActiveTabState();
        }

        var closedTabIndex = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        if (_activeTab == tab)
        {
            var nextTabIndex = Math.Clamp(closedTabIndex, 0, _tabs.Count - 1);
            _activeTab = _tabs[nextTabIndex];
            RenderTabs();
            await ShowPageAsync(_activeTab.Page, true);
            return;
        }

        RenderTabs();
    }

    private async Task ShowPageAsync(ShellPage page, bool loadData)
    {
        DashboardPanel.Visibility = page == ShellPage.Dashboard ? Visibility.Visible : Visibility.Collapsed;
        UsersPanel.Visibility = page == ShellPage.Users ? Visibility.Visible : Visibility.Collapsed;
        PessoasPanel.Visibility = page == ShellPage.Pessoas ? Visibility.Visible : Visibility.Collapsed;
        ImoveisPanel.Visibility = page == ShellPage.Imoveis ? Visibility.Visible : Visibility.Collapsed;
        ModulePanel.Visibility = IsGenericModulePage(page) ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticsPanel.Visibility = page == ShellPage.Diagnostics ? Visibility.Visible : Visibility.Collapsed;

        SetActiveNavigation(page switch
        {
            ShellPage.Users => UsersNavButton,
            ShellPage.Pessoas => PessoasNavButton,
            ShellPage.Imoveis => ImoveisNavButton,
            ShellPage.Locacoes => LocacoesNavButton,
            ShellPage.Financeiro => FinanceiroNavButton,
            ShellPage.Boletos => BoletosNavButton,
            ShellPage.NotasFiscais => NotasFiscaisNavButton,
            ShellPage.Documentos => DocumentosNavButton,
            ShellPage.Relatorios => RelatoriosNavButton,
            ShellPage.Dimob => DimobNavButton,
            ShellPage.Manutencoes => ManutencoesNavButton,
            ShellPage.Vistorias => VistoriasNavButton,
            ShellPage.Configuracoes => ConfiguracoesNavButton,
            ShellPage.Diagnostics => DiagnosticsNavButton,
            _ => DashboardNavButton
        });

        if (loadData)
        {
            if (page == ShellPage.Dashboard)
            {
                await LoadDashboardAsync();
            }
            else if (page == ShellPage.Users)
            {
                await LoadUsersAsync();
            }
            else if (page == ShellPage.Pessoas)
            {
                await LoadPessoasAsync();
            }
            else if (page == ShellPage.Imoveis)
            {
                await LoadImoveisAsync();
            }
            else if (IsGenericModulePage(page))
            {
                await LoadGenericModuleAsync(page);
            }
        }

        await RestoreActiveTabStateAsync(page);
    }

    private static Guid? TryGetItemId(object? item)
    {
        if (item is null)
        {
            return null;
        }

        var property = item.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(item) is Guid id ? id : null;
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

        dataGrid.SelectedItem = null;
    }

    private interface IShellPageState;

    private string GetTabSecondaryText(ShellTab tab)
    {
        try
        {
            if (tab.Page != ShellPage.Pessoas)
            {
                return string.Empty;
            }

            // Prefer per-tab stored name. If empty, fall back to the currently loaded selected pessoa for the active tab.
            var fullName = tab.SelectedPessoaName;
            if (string.IsNullOrWhiteSpace(fullName) && tab == _activeTab && _selectedPessoaDetails is not null)
            {
                fullName = _selectedPessoaDetails.Summary.Nome;
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return string.Empty;
            }
            var tb = new TextBlock { Text = fullName };
            // If it fits in the allowed width, return full name.
            // Otherwise, compress last names to initials from the end until it fits.
            var maxWidth = 160.0;
            var formatted = fullName;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
            {
                return fullName;
            }

            // Use a simple measurement via a FormattedText alternative not available; approximate by length.
            if (formatted.Length <= 28)
            {
                return formatted;
            }

            for (var i = parts.Length - 1; i >= 1; i--)
            {
                parts[i] = parts[i][0] + ".";
                formatted = string.Join(' ', parts);
                if (formatted.Length <= 28)
                {
                    return formatted;
                }
            }

            return formatted;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed class ShellTab
    {
        public ShellTab(Guid id, string title, ShellPage page)
        {
            Id = id;
            Title = title;
            Page = page;
        }

        public Guid Id { get; }

        public string Title { get; set; }

        public ShellPage Page { get; set; }
        public Dictionary<ShellPage, IShellPageState> PageStates { get; } = new Dictionary<ShellPage, IShellPageState>();

        // Store per-tab selected person name so each tab can show its own secondary text
        public string SelectedPessoaName { get; set; } = string.Empty;
    }

    private sealed class NoShellPageState : IShellPageState
    {
        public static NoShellPageState Instance { get; } = new();

        private NoShellPageState()
        {
        }
    }
}

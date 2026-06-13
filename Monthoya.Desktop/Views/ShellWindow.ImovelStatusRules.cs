using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Monthoya.Core.Entities;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ImovelStatusRulesRegistered = RegisterImovelStatusRules();
    private bool _imovelStatusRulesApplied;
    private bool _isRefreshingImovelStatusOptions;

    private static bool RegisterImovelStatusRules()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForImovelStatusRules));

        return true;
    }

    private static void OnShellWindowLoadedForImovelStatusRules(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyImovelStatusRules, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyImovelStatusRules()
    {
        if (_imovelStatusRulesApplied)
        {
            return;
        }

        _imovelStatusRulesApplied = true;

        ImovelStatusBox.DropDownOpened += (_, _) => RefreshImovelStatusOptions();
        ImoveisGrid.SelectionChanged += (_, _) => ScheduleRefreshImovelStatusOptions();
        ImovelEditButton.Click += (_, _) => ScheduleRefreshImovelStatusOptions();
        CancelImovelEditButton.Click += (_, _) => ScheduleRefreshImovelStatusOptions();
        SaveImovelButton.Click += (_, _) => ScheduleRefreshImovelStatusOptions();
        ImovelCidadeBox.TextChanged += (_, _) => FixImovelCidadeMojibake();

        RefreshImovelStatusOptions();
        FixImovelCidadeMojibake();
    }

    private void ScheduleRefreshImovelStatusOptions()
    {
        Dispatcher.BeginInvoke(RefreshImovelStatusOptions, DispatcherPriority.ContextIdle);
        _ = RefreshImovelStatusOptionsAfterDelayAsync(150);
        _ = RefreshImovelStatusOptionsAfterDelayAsync(450);
    }

    private async Task RefreshImovelStatusOptionsAfterDelayAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
        await Dispatcher.InvokeAsync(RefreshImovelStatusOptions, DispatcherPriority.ContextIdle);
    }

    private void RefreshImovelStatusOptions()
    {
        if (_isRefreshingImovelStatusOptions)
        {
            return;
        }

        _isRefreshingImovelStatusOptions = true;
        try
        {
            var currentStatus = GetCurrentImovelStatusForOptions();
            var selectedStatus = ImovelStatusBox.SelectedValue is ImovelStatus selected
                ? selected
                : currentStatus;

            var options = GetImovelFormStatusOptions(currentStatus).ToList();
            ImovelStatusBox.ItemsSource = options;
            ImovelStatusBox.SelectedValuePath = "Status";
            ImovelStatusBox.DisplayMemberPath = "Label";

            if (selectedStatus.HasValue && options.Any(x => x.Status == selectedStatus.Value))
            {
                ImovelStatusBox.SelectedValue = selectedStatus.Value;
            }
            else if (!_selectedImovelId.HasValue && ImovelStatusBox.SelectedValue is null)
            {
                ImovelStatusBox.SelectedIndex = -1;
            }

            if (_isImovelEditing)
            {
                ImovelStatusBox.IsEnabled = !IsSystemManagedImovelStatus(currentStatus);
            }
        }
        finally
        {
            _isRefreshingImovelStatusOptions = false;
        }
    }

    private ImovelStatus? GetCurrentImovelStatusForOptions()
    {
        if (_selectedImovelDetails is not null)
        {
            return _selectedImovelDetails.Dados.Status;
        }

        return ImovelStatusBox.SelectedValue is ImovelStatus selectedStatus
            ? selectedStatus
            : null;
    }

    private static IEnumerable<ImovelStatusOption> GetImovelFormStatusOptions(ImovelStatus? currentStatus)
    {
        yield return new ImovelStatusOption("Disponível", ImovelStatus.Disponivel);
        yield return new ImovelStatusOption("Reservado", ImovelStatus.Reservado);
        yield return new ImovelStatusOption("Vendido", ImovelStatus.Vendido);

        if (currentStatus.HasValue && IsSystemManagedImovelStatus(currentStatus.Value))
        {
            yield return new ImovelStatusOption(GetImovelStatusOptionLabel(currentStatus.Value), currentStatus.Value);
        }
    }

    private static bool MatchesImovelStatusFilter(string? status, string statusFilter)
    {
        var normalizedStatus = NormalizeImovelStatusValue(status);
        return statusFilter switch
        {
            "ativos" => normalizedStatus != "inativo",
            "todos" => true,
            _ => normalizedStatus == statusFilter
        };
    }

    private static string NormalizeImovelStatusValue(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        if (status.Contains("Dispon", StringComparison.OrdinalIgnoreCase)) return "disponivel";
        if (status.Contains("Reserv", StringComparison.OrdinalIgnoreCase)) return "reservado";
        if (status.Contains("Locad", StringComparison.OrdinalIgnoreCase)) return "locado";
        if (status.Contains("Vendid", StringComparison.OrdinalIgnoreCase)) return "vendido";
        if (status.Contains("Inativ", StringComparison.OrdinalIgnoreCase)) return "inativo";

        return status.Trim().ToLowerInvariant();
    }

    private static bool IsSystemManagedImovelStatus(ImovelStatus? status) =>
        status is ImovelStatus.Locado or ImovelStatus.Inativo;

    private static string GetImovelStatusOptionLabel(ImovelStatus status) =>
        status switch
        {
            ImovelStatus.Disponivel => "Disponível",
            ImovelStatus.Reservado => "Reservado",
            ImovelStatus.Locado => "Locado",
            ImovelStatus.Vendido => "Vendido",
            ImovelStatus.Inativo => "Inativo",
            _ => status.ToString()
        };

    private void FixImovelCidadeMojibake()
    {
        if (ImovelCidadeBox.Text.Contains("Ã", StringComparison.Ordinal) ||
            ImovelCidadeBox.Text.Contains("Â", StringComparison.Ordinal) ||
            ImovelCidadeBox.Text.Contains("�", StringComparison.Ordinal))
        {
            ImovelCidadeBox.Text = "Paranavaí";
            ImovelCidadeBox.CaretIndex = ImovelCidadeBox.Text.Length;
        }
    }
}

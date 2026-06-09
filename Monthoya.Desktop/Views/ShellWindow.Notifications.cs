using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task LoadNotificationsAsync()
    {
        if (_isLoadingNotifications)
        {
            return;
        }

        try
        {
            _isLoadingNotifications = true;
            _notificationUsers = (await _userService.GetUsersAsync())
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayName)
                .ToList();
            NewNotificationRecipientsList.ItemsSource = _notificationUsers;

            var emailSettings = await _notificationEmailSettingsService.GetAsync();
            var canSendEmail = emailSettings.IsEnabled
                && emailSettings.HasPassword
                && !string.IsNullOrWhiteSpace(emailSettings.SenderEmail)
                && !string.IsNullOrWhiteSpace(emailSettings.SmtpHost)
                && !string.IsNullOrWhiteSpace(emailSettings.SmtpUsername);
            NewNotificationEmailBox.IsEnabled = canSendEmail;
            NewNotificationEmailBox.Content = canSendEmail ? "E-mail" : "E-mail não configurado";

            await RefreshNotificationsAsync();
        }
        finally
        {
            _isLoadingNotifications = false;
        }
    }

    private async Task RefreshNotificationsAsync(Guid? selectedNotificationId = null, bool reloadSelectedDetails = false)
    {
        if (_isRefreshingNotifications)
        {
            return;
        }

        try
        {
            _isRefreshingNotifications = true;
            var filter = BuildNotificationFilter();
            _notifications = await _notificationService.GetAllForUserAsync(_currentUser.Id, filter);

            _isChangingNotificationSelection = true;
            NotificationsGrid.ItemsSource = _notifications;
            RestoreDataGridSelection(NotificationsGrid, selectedNotificationId);
            if (!selectedNotificationId.HasValue)
            {
                NotificationsGrid.SelectedItem = null;
            }

            if (_isNotificationHistoryVisible)
            {
                await RefreshNotificationHistoryAsync(filter);
            }

            await RefreshNotificationBellAsync();
        }
        finally
        {
            _isChangingNotificationSelection = false;
            _isRefreshingNotifications = false;
        }

        if (reloadSelectedDetails)
        {
            await LoadSelectedNotificationDetailsAsync();
        }
    }

    private async Task RefreshNotificationHistoryAsync(NotificationFilter? filter = null)
    {
        filter ??= BuildNotificationFilter();
        _notificationHistory = await _notificationService.GetHistoryForUserAsync(_currentUser.Id, filter);
        NotificationHistoryGrid.ItemsSource = _notificationHistory;
    }

    private NotificationFilter BuildNotificationFilter() =>
        new(
            NotificationsSearchBox.Text,
            NotificationsUnreadOnlyBox.IsChecked == true,
            NotificationsCategoryFilterBox.SelectedValue as NotificationCategory?,
            NotificationsPriorityFilterBox.SelectedValue as NotificationPriority?);

    private async Task RefreshNotificationBellAsync()
    {
        var unreadCount = await _notificationService.GetUnreadCountAsync(_currentUser.Id);
        NotificationBadge.Visibility = unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        NotificationBadgeText.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();

        var recent = await _notificationService.GetRecentForUserAsync(_currentUser.Id, 5);
        NotificationRecentItems.ItemsSource = recent;
        NotificationPopupEmptyText.Visibility = recent.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task ShowRequiredNotificationsAsync()
    {
        var required = await _notificationService.GetRequiredUnreadAsync(_currentUser.Id);
        foreach (var notification in required)
        {
            MessageBox.Show(
                this,
                $"{notification.Title}{Environment.NewLine}{Environment.NewLine}{notification.Body}",
                "Confirmação necessária",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            await _notificationService.AcknowledgeAsync(notification.Id, _currentUser.Id);
        }

        if (required.Count > 0)
        {
            await RefreshNotificationBellAsync();
        }
    }

    private async void NotificationBellButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshNotificationBellAsync();
        NotificationPopup.IsOpen = true;
    }

    private async void NotificationPopupItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not Guid notificationId)
        {
            return;
        }

        NotificationPopup.IsOpen = false;
        await OpenNotificationCenterAndSelectAsync(notificationId);
    }

    private async Task OpenNotificationCenterAndSelectAsync(Guid notificationId)
    {
        await UpdateActiveTabAsync(ShellPage.Notificacoes, "Notificações", true);
        await SelectNotificationAsync(notificationId);
    }

    private async Task SelectNotificationAsync(Guid notificationId)
    {
        if (_notifications.All(x => x.Id != notificationId))
        {
            await RefreshNotificationsAsync(notificationId);
        }

        var active = _notifications.FirstOrDefault(x => x.Id == notificationId);
        if (active is not null)
        {
            _isChangingNotificationSelection = true;
            NotificationHistoryGrid.SelectedItem = null;
            NotificationsGrid.SelectedItem = active;
            NotificationsGrid.ScrollIntoView(active);
            _isChangingNotificationSelection = false;
            await LoadSelectedNotificationDetailsAsync();
            return;
        }

        if (!_isNotificationHistoryVisible)
        {
            ToggleNotificationHistoryVisibility(true);
            await RefreshNotificationHistoryAsync();
        }

        var history = _notificationHistory.FirstOrDefault(x => x.Id == notificationId);
        if (history is not null)
        {
            _isChangingNotificationSelection = true;
            NotificationsGrid.SelectedItem = null;
            NotificationHistoryGrid.SelectedItem = history;
            NotificationHistoryGrid.ScrollIntoView(history);
            _isChangingNotificationSelection = false;
            await LoadSelectedNotificationDetailsAsync();
        }
    }

    private async void OpenNotificationCenterButton_Click(object sender, RoutedEventArgs e)
    {
        NotificationPopup.IsOpen = false;
        await UpdateActiveTabAsync(ShellPage.Notificacoes, "Notificações", true);
    }

    private async void MarkAllNotificationsReadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isAcknowledgingNotification)
        {
            return;
        }

        try
        {
            _isAcknowledgingNotification = true;
            SetNotificationActionButtonsEnabled(false);
            await _notificationService.MarkAllAsReadAsync(_currentUser.Id);
            await RefreshNotificationsAsync();
            ClearNotificationDetails();
        }
        finally
        {
            _isAcknowledgingNotification = false;
        }
    }

    private async void ReloadNotificationsButton_Click(object sender, RoutedEventArgs e) =>
        await RefreshNotificationsAsync(GetSelectedNotificationId(), reloadSelectedDetails: true);

    private async void NotificationHistoryToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleNotificationHistoryVisibility(!_isNotificationHistoryVisible);
        if (_isNotificationHistoryVisible)
        {
            await RefreshNotificationHistoryAsync();
        }
    }

    private void ToggleNotificationHistoryVisibility(bool show)
    {
        _isNotificationHistoryVisible = show;
        NotificationHistoryPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        NotificationMainListRow.Height = show ? new GridLength(220) : new GridLength(1, GridUnitType.Star);
        NotificationHistoryRow.Height = show ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        NotificationHistoryToggleButton.Content = show ? "Ocultar histórico" : "Histórico";
    }

    private async void NotificationsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (NotificacoesPanel.Visibility == Visibility.Visible)
        {
            await RefreshNotificationsAsync();
        }
    }

    private async void NotificationsFilter_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (NotificacoesPanel.Visibility == Visibility.Visible)
        {
            await RefreshNotificationsAsync();
        }
    }

    private async void NotificationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingNotifications || _isRefreshingNotifications || _isChangingNotificationSelection)
        {
            return;
        }

        if (NotificationsGrid.SelectedItem is not null)
        {
            NotificationHistoryGrid.SelectedItem = null;
        }

        await LoadSelectedNotificationDetailsAsync();
    }

    private async void NotificationHistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingNotifications || _isRefreshingNotifications || _isChangingNotificationSelection)
        {
            return;
        }

        if (NotificationHistoryGrid.SelectedItem is not null)
        {
            NotificationsGrid.SelectedItem = null;
        }

        await LoadSelectedNotificationDetailsAsync();
    }

    private async Task LoadSelectedNotificationDetailsAsync()
    {
        if (_isLoadingNotificationDetails)
        {
            return;
        }

        var selected = GetSelectedNotification();
        if (selected is null)
        {
            ClearNotificationDetails();
            return;
        }

        try
        {
            _isLoadingNotificationDetails = true;
            SetNotificationActionButtonsEnabled(false);

            var details = await _notificationService.GetDetailsAsync(selected.Id, _currentUser.Id);
            if (details is null)
            {
                ClearNotificationDetails();
                return;
            }

            if (!details.Summary.IsRead)
            {
                await _notificationService.MarkAsReadAsync(selected.Id, _currentUser.Id);
                details = await _notificationService.GetDetailsAsync(selected.Id, _currentUser.Id);
                await RefreshNotificationsAsync(selected.Id);
            }

            if (details is null)
            {
                ClearNotificationDetails();
                return;
            }

            NotificationDetailsTitleText.Text = details.Summary.Title;
            NotificationDetailsMetaText.Text = $"{details.Summary.Category} | {details.Summary.Priority} | {details.Summary.CreatedAtUtc.LocalDateTime:dd/MM/yyyy HH:mm}";
            SetNotificationDetailsBody(details.Summary.Body);
            NotificationDeliveriesGrid.ItemsSource = details.Deliveries;
            UpdateNotificationActionButtons(details.Summary);
        }
        finally
        {
            _isLoadingNotificationDetails = false;
        }
    }

    private async void NotificationMarkReadButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedNotification();
        if (selected is null || _isAcknowledgingNotification)
        {
            return;
        }

        try
        {
            _isAcknowledgingNotification = true;
            SetNotificationActionButtonsEnabled(false);
            await _notificationService.MarkAsReadAsync(selected.Id, _currentUser.Id);
            await RefreshNotificationsAsync(selected.Id, reloadSelectedDetails: true);
        }
        finally
        {
            _isAcknowledgingNotification = false;
        }
    }

    private async void NotificationAcknowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedNotification();
        if (selected is null || _isAcknowledgingNotification)
        {
            return;
        }

        try
        {
            _isAcknowledgingNotification = true;
            SetNotificationActionButtonsEnabled(false);
            await _notificationService.AcknowledgeAsync(selected.Id, _currentUser.Id);
            await RefreshNotificationsAsync(selected.Id, reloadSelectedDetails: true);
        }
        finally
        {
            _isAcknowledgingNotification = false;
        }
    }

    private async void NotificationDismissButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedNotification();
        if (selected is null || _isAcknowledgingNotification)
        {
            return;
        }

        try
        {
            _isAcknowledgingNotification = true;
            SetNotificationActionButtonsEnabled(false);
            await _notificationService.DismissAsync(selected.Id, _currentUser.Id);
            await RefreshNotificationsAsync();
            ClearNotificationDetails();
        }
        finally
        {
            _isAcknowledgingNotification = false;
        }
    }

    private async void NotificationActionButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedNotification();
        if (selected is null || string.IsNullOrWhiteSpace(selected.ActionTarget))
        {
            return;
        }

        if (selected.ActionTarget.StartsWith("chaves:", StringComparison.OrdinalIgnoreCase))
        {
            await UpdateActiveTabAsync(ShellPage.Chaves, "Chaves", true);
        }
    }

    private async void CreateNotificationButton_Click(object sender, RoutedEventArgs e)
    {
        NewNotificationErrorText.Text = string.Empty;
        try
        {
            var recipients = NewNotificationRecipientsList.SelectedItems
                .OfType<UserSummary>()
                .Select(x => x.Id)
                .ToList();

            if (recipients.Count == 0)
            {
                recipients.Add(_currentUser.Id);
            }

            var created = await _notificationService.CreateManualMessageAsync(
                new CreateManualNotificationRequest(
                    NewNotificationTitleBox.Text,
                    NewNotificationBodyBox.Text,
                    recipients,
                    _currentUser.Id,
                    NewNotificationPriorityBox.SelectedValue is NotificationPriority priority ? priority : NotificationPriority.Normal,
                    NewNotificationCategoryBox.SelectedValue is NotificationCategory category ? category : NotificationCategory.ManualMessage,
                    NewNotificationRequiresAckBox.IsChecked == true,
                    SendEmail: NewNotificationEmailBox.IsChecked == true && NewNotificationEmailBox.IsEnabled,
                    SendWhatsApp: false));

            ClearNewNotificationForm();
            await RefreshNotificationsAsync(created.Id);
            await SelectNotificationAsync(created.Id);
        }
        catch (Exception ex)
        {
            NewNotificationErrorText.Text = ex.Message;
        }
    }

    private Guid? GetSelectedNotificationId() => GetSelectedNotification()?.Id;

    private NotificationSummary? GetSelectedNotification() =>
        NotificationsGrid.SelectedItem as NotificationSummary
        ?? NotificationHistoryGrid.SelectedItem as NotificationSummary;

    private void ClearNotificationDetails()
    {
        NotificationDetailsTitleText.Text = "Selecione uma notificação.";
        NotificationDetailsMetaText.Text = string.Empty;
        NotificationDetailsBodyText.Inlines.Clear();
        NotificationDeliveriesGrid.ItemsSource = Array.Empty<NotificationDeliverySummary>();
        SetNotificationActionButtonsEnabled(false);
    }

    private void SetNotificationDetailsBody(string body)
    {
        NotificationDetailsBodyText.Inlines.Clear();
        var lines = (body ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var colonIndex = line.IndexOf(':', StringComparison.Ordinal);
            if (colonIndex > 0)
            {
                NotificationDetailsBodyText.Inlines.Add(new Run(line[..colonIndex]) { FontWeight = FontWeights.SemiBold });
                NotificationDetailsBodyText.Inlines.Add(new Run(line[colonIndex..]));
            }
            else
            {
                NotificationDetailsBodyText.Inlines.Add(new Run(line));
            }

            if (i < lines.Length - 1)
            {
                NotificationDetailsBodyText.Inlines.Add(new LineBreak());
            }
        }
    }

    private void SetNotificationActionButtonsEnabled(bool isEnabled)
    {
        NotificationMarkReadButton.IsEnabled = isEnabled;
        NotificationAcknowledgeButton.IsEnabled = isEnabled;
        NotificationDismissButton.IsEnabled = isEnabled;
        NotificationActionButton.IsEnabled = isEnabled;
    }

    private void UpdateNotificationActionButtons(NotificationSummary summary)
    {
        NotificationMarkReadButton.IsEnabled = !summary.IsRead && !_isAcknowledgingNotification;
        NotificationAcknowledgeButton.IsEnabled = summary.RequiresAcknowledgement && !summary.IsAcknowledged && !_isAcknowledgingNotification;
        NotificationDismissButton.IsEnabled = !summary.IsDismissed && !_isAcknowledgingNotification;
        NotificationActionButton.Content = string.IsNullOrWhiteSpace(summary.ActionLabel) ? "Abrir" : summary.ActionLabel;
        NotificationActionButton.IsEnabled = !string.IsNullOrWhiteSpace(summary.ActionTarget) && !_isAcknowledgingNotification;
    }

    private void ClearNewNotificationForm()
    {
        NewNotificationTitleBox.Clear();
        NewNotificationBodyBox.Clear();
        NewNotificationPriorityBox.SelectedValue = NotificationPriority.Normal;
        NewNotificationCategoryBox.SelectedValue = NotificationCategory.ManualMessage;
        NewNotificationRequiresAckBox.IsChecked = false;
        NewNotificationRecipientsList.SelectedItems.Clear();
    }
}

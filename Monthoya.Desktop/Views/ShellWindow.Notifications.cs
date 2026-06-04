using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task LoadNotificationsAsync()
    {
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

    private async Task RefreshNotificationsAsync()
    {
        var filter = new NotificationFilter(
            NotificationsSearchBox.Text,
            NotificationsUnreadOnlyBox.IsChecked == true,
            NotificationsCategoryFilterBox.SelectedValue as NotificationCategory?,
            NotificationsPriorityFilterBox.SelectedValue as NotificationPriority?);

        _notifications = await _notificationService.GetAllForUserAsync(_currentUser.Id, filter);
        NotificationsGrid.ItemsSource = _notifications;
        await RefreshNotificationBellAsync();
    }

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

    private async void OpenNotificationCenterButton_Click(object sender, RoutedEventArgs e)
    {
        NotificationPopup.IsOpen = false;
        await UpdateActiveTabAsync(ShellPage.Notificacoes, "Notificações", true);
    }

    private async void MarkAllNotificationsReadButton_Click(object sender, RoutedEventArgs e)
    {
        await _notificationService.MarkAllAsReadAsync(_currentUser.Id);
        await RefreshNotificationsAsync();
    }

    private async void ReloadNotificationsButton_Click(object sender, RoutedEventArgs e) => await RefreshNotificationsAsync();

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
        await LoadSelectedNotificationDetailsAsync();
    }

    private async Task LoadSelectedNotificationDetailsAsync()
    {
        if (NotificationsGrid.SelectedItem is not NotificationSummary selected)
        {
            NotificationDetailsTitleText.Text = "Selecione uma notificação.";
            NotificationDetailsMetaText.Text = string.Empty;
            NotificationDetailsBodyText.Text = string.Empty;
            NotificationDeliveriesGrid.ItemsSource = Array.Empty<NotificationDeliverySummary>();
            NotificationMarkReadButton.IsEnabled = false;
            NotificationAcknowledgeButton.IsEnabled = false;
            NotificationActionButton.IsEnabled = false;
            return;
        }

        var details = await _notificationService.GetDetailsAsync(selected.Id, _currentUser.Id);
        if (details is null)
        {
            return;
        }

        NotificationDetailsTitleText.Text = details.Summary.Title;
        NotificationDetailsMetaText.Text = $"{details.Summary.Category} | {details.Summary.Priority} | {details.Summary.CreatedAtUtc.LocalDateTime:dd/MM/yyyy HH:mm}";
        NotificationDetailsBodyText.Text = details.Summary.Body;
        NotificationDeliveriesGrid.ItemsSource = details.Deliveries;
        NotificationMarkReadButton.IsEnabled = !details.Summary.IsRead;
        NotificationAcknowledgeButton.IsEnabled = details.Summary.RequiresAcknowledgement && !details.Summary.IsAcknowledged;
        NotificationActionButton.Content = string.IsNullOrWhiteSpace(details.Summary.ActionLabel) ? "Abrir" : details.Summary.ActionLabel;
        NotificationActionButton.IsEnabled = !string.IsNullOrWhiteSpace(details.Summary.ActionTarget);
    }

    private async void NotificationMarkReadButton_Click(object sender, RoutedEventArgs e)
    {
        if (NotificationsGrid.SelectedItem is not NotificationSummary selected)
        {
            return;
        }

        await _notificationService.MarkAsReadAsync(selected.Id, _currentUser.Id);
        await RefreshNotificationsAsync();
        RestoreDataGridSelection(NotificationsGrid, selected.Id);
        await LoadSelectedNotificationDetailsAsync();
    }

    private async void NotificationAcknowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        if (NotificationsGrid.SelectedItem is not NotificationSummary selected)
        {
            return;
        }

        await _notificationService.AcknowledgeAsync(selected.Id, _currentUser.Id);
        await RefreshNotificationsAsync();
        RestoreDataGridSelection(NotificationsGrid, selected.Id);
        await LoadSelectedNotificationDetailsAsync();
    }

    private async void NotificationActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (NotificationsGrid.SelectedItem is not NotificationSummary selected || string.IsNullOrWhiteSpace(selected.ActionTarget))
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
            await RefreshNotificationsAsync();
            RestoreDataGridSelection(NotificationsGrid, created.Id);
        }
        catch (Exception ex)
        {
            NewNotificationErrorText.Text = ex.Message;
        }
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

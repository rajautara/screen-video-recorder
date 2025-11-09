using System.Windows;
using ScreenRecorder.Domain.Models;
using Serilog;

namespace ScreenRecorder.Presentation.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger _logger;

        public NotificationService(ILogger logger)
        {
            _logger = logger;
        }

        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            _logger.Information("Notification: [{Type}] {Title} - {Message}", type, title, message);

            // For now, show as MessageBox. In production, use toast notifications
            var icon = type switch
            {
                NotificationType.Error => MessageBoxImage.Error,
                NotificationType.Warning => MessageBoxImage.Warning,
                NotificationType.Success => MessageBoxImage.Information,
                _ => MessageBoxImage.Information
            };

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            });
        }

        public void ShowToast(string message)
        {
            _logger.Debug("Toast: {Message}", message);
            // TODO: Implement toast notification using Windows notifications
            ShowNotification("Screen Recorder", message, NotificationType.Info);
        }
    }
}

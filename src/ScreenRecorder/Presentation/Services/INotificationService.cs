using ScreenRecorder.Domain.Models;

namespace ScreenRecorder.Presentation.Services
{
    public interface INotificationService
    {
        void ShowNotification(string title, string message, NotificationType type = NotificationType.Info);
        void ShowToast(string message);
    }
}

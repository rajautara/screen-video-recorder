using System.Windows;

namespace ScreenRecorder.Presentation.Services
{
    public interface IDialogService
    {
        MessageBoxResult ShowMessage(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information);
        bool ShowConfirmation(string message, string title);
        string ShowSaveFileDialog(string filter, string defaultFileName = null);
        string ShowOpenFileDialog(string filter);
        string ShowFolderBrowserDialog();
    }
}

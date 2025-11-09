using System.Windows;
using Microsoft.Win32;
using System.Windows.Forms;
using Serilog;
using WinForms = System.Windows.Forms;

namespace ScreenRecorder.Presentation.Services
{
    public class DialogService : IDialogService
    {
        private readonly ILogger _logger;

        public DialogService(ILogger logger)
        {
            _logger = logger;
        }

        public MessageBoxResult ShowMessage(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            _logger.Debug("Showing message dialog: {Title}", title);
            return System.Windows.MessageBox.Show(message, title, buttons, icon);
        }

        public bool ShowConfirmation(string message, string title)
        {
            var result = ShowMessage(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public string ShowSaveFileDialog(string filter, string defaultFileName = null)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName ?? string.Empty
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        public string ShowOpenFileDialog(string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        public string ShowFolderBrowserDialog()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }

            return null;
        }
    }
}

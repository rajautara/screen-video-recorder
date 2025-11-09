using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using ScreenRecorder.Domain.Models;
using ScreenRecorder.Infrastructure.Services;
using ScreenRecorder.Infrastructure.Settings;
using ScreenRecorder.Presentation.Services;
using Serilog;

namespace ScreenRecorder.Presentation.ViewModels
{
    public class PostRecordViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IFileService _fileService;
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;

        private string _outputFilePath;
        private bool _isFileSaved;

        public PostRecordViewModel(
            string outputFilePath,
            IDialogService dialogService,
            IFileService fileService,
            ILogger logger,
            IAppSettings settings)
        {
            _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            // Initialize commands
            SaveCommand = new RelayCommand(SaveRecording, CanSave);
            DiscardCommand = new RelayCommand(DiscardRecording);
            OpenContainingFolderCommand = new RelayCommand(OpenContainingFolder, CanOpenContainingFolder);
            PlayCommand = new RelayCommand(PlayRecording, CanPlay);
        }

        public string OutputFilePath
        {
            get => _outputFilePath;
            private set => SetProperty(ref _outputFilePath, value);
        }

        public bool IsFileSaved
        {
            get => _isFileSaved;
            private set
            {
                if (SetProperty(ref _isFileSaved, value))
                {
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)OpenContainingFolderCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)PlayCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand DiscardCommand { get; }
        public ICommand OpenContainingFolderCommand { get; }
        public ICommand PlayCommand { get; }

        public event EventHandler RequestClose;

        private bool CanSave()
        {
            return !string.IsNullOrEmpty(OutputFilePath) && !IsFileSaved;
        }

        private void SaveRecording()
        {
            try
            {
                var initialDirectory = Path.GetDirectoryName(OutputFilePath);
                var fileName = Path.GetFileName(OutputFilePath);
                var filter = "Video Files|*.mp4;*.avi;*.wmv;*.mov|All Files|*.*";

                // Show save file dialog to choose a new location
                var newPath = _dialogService.ShowSaveFileDialog(filter, fileName);
                if (string.IsNullOrEmpty(newPath))
                {
                    _logger.Information("Save operation cancelled by user");
                    return;
                }

                // Copy the file to the new location
                    _fileService.Copy(OutputFilePath, newPath, true);
                    _logger.Information("Recording saved to: {Path}", newPath);
                    
                    // Update the current file path and mark as saved
                    OutputFilePath = newPath;
                    IsFileSaved = true;
                    
                    // Update the default save location for next time
                    _settings.OutputDirectory = Path.GetDirectoryName(newPath);
                    _settings.Save();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save recording");
                _dialogService.ShowMessage("Failed to save the recording. Please try again.", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DiscardRecording()
        {
            try
            {
                var result = _dialogService.ShowConfirmation(
                    "Are you sure you want to discard this recording? This action cannot be undone.",
                    "Discard Recording");

                if (result)
                {
                    _logger.Information("Discarding recording: {Path}", OutputFilePath);
                    
                    // Delete the temporary file if it exists and is not saved
                    if (!IsFileSaved && File.Exists(OutputFilePath))
                    {
                        File.Delete(OutputFilePath);
                    }
                    
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while discarding recording");
                _dialogService.ShowMessage("An error occurred while discarding the recording.", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanOpenContainingFolder()
        {
            return !string.IsNullOrEmpty(OutputFilePath) && File.Exists(OutputFilePath);
        }

        private void OpenContainingFolder()
        {
            try
            {
                if (File.Exists(OutputFilePath))
                {
                    var folderPath = Path.GetDirectoryName(OutputFilePath);
                    Process.Start("explorer.exe", $"/select,\"{OutputFilePath}\"");
                    _logger.Debug("Opened containing folder: {Path}", folderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to open containing folder");
                _dialogService.ShowMessage("Failed to open the containing folder.", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanPlay()
        {
            return !string.IsNullOrEmpty(OutputFilePath) && File.Exists(OutputFilePath);
        }

        private void PlayRecording()
        {
            try
            {
                if (File.Exists(OutputFilePath))
                {
                    Process.Start(new ProcessStartInfo(OutputFilePath) { UseShellExecute = true });
                    _logger.Debug("Playing recording: {Path}", OutputFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to play recording");
                _dialogService.ShowMessage("Failed to play the recording. No default application is associated with this file type.", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

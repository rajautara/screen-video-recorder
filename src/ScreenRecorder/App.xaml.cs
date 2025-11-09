using System;
using System.Windows;
using ScreenRecorder.Infrastructure.Logging;
using ScreenRecorder.Infrastructure.Settings;
using ScreenRecorder.Infrastructure.Storage;
using ScreenRecorder.Presentation.Services;
using SimpleInjector;
using Serilog;

namespace ScreenRecorder
{
    public partial class App : Application
    {
        private static Container _container;
        private ILogger _logger;

        public static Container Container => _container;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize DI container
                _container = new Container();
                ConfigureServices(_container);
                _container.Verify();

                // Initialize logger
                _logger = _container.GetInstance<ILogger>();
                _logger.Information("Application starting...");

                // Set up global exception handling
                SetupExceptionHandling();

                // Check for crash recovery
                var storageService = _container.GetInstance<IStorageService>();
                storageService.RecoverFromCrash();

                _logger.Information("Application started successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start application:\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _logger?.Information("Application shutting down...");

                // Clean up tray icon
                var trayService = _container?.GetInstance<ITrayService>();
                trayService?.Dispose();

                _logger?.Information("Application shut down successfully");
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during shutdown: {ex.Message}", "Shutdown Error");
            }

            base.OnExit(e);
        }

        private void ConfigureServices(Container container)
        {
            // Logging
            container.RegisterSingleton<ILogService, LogService>();
            container.RegisterSingleton<ILogger>(() =>
            {
                var logService = container.GetInstance<ILogService>();
                return logService.CreateLogger();
            });

            // Settings & Storage
            container.RegisterSingleton<ISettingsService, SettingsService>();
            container.RegisterSingleton<IStorageService, StorageService>();
            
            // Settings wrapper - need to implement AppSettingsAdapter
            container.RegisterSingleton<IAppSettings>(() => 
            {
                var settingsService = container.GetInstance<ISettingsService>();
                return new AppSettingsAdapter(settingsService);
            });

            // Infrastructure Services
            container.RegisterSingleton<Infrastructure.Services.IRegionSelectionService, Infrastructure.Services.RegionSelectionService>();
            container.RegisterSingleton<Infrastructure.Services.IFileService, Infrastructure.Services.FileService>();
            container.RegisterSingleton<Infrastructure.Recording.IScreenCaptureService, Infrastructure.Recording.ScreenCaptureService>();
            container.RegisterSingleton<Infrastructure.Audio.IAudioDeviceService, Infrastructure.Audio.AudioDeviceService>();
            container.RegisterSingleton<Infrastructure.Hotkeys.IGlobalHotkeyService, Infrastructure.Hotkeys.GlobalHotkeyService>();
            container.RegisterSingleton<Infrastructure.System.IDpiService, Infrastructure.System.DpiService>();
            container.RegisterSingleton<Infrastructure.System.IPowerEventsService, Infrastructure.System.PowerEventsService>();

            // Orchestration
            container.RegisterSingleton<Domain.Orchestration.IRecordingController, Domain.Orchestration.RecordingController>();

            // UI Services
            container.RegisterSingleton<IDialogService, DialogService>();
            container.RegisterSingleton<ITrayService, TrayService>();
            container.RegisterSingleton<INotificationService, NotificationService>();
            container.RegisterSingleton<IThemeService, ThemeService>();

            // ViewModels
            container.Register<Presentation.ViewModels.MainViewModel>(Lifestyle.Singleton);
            container.Register<Presentation.ViewModels.SettingsViewModel>(Lifestyle.Transient);
            container.Register<Presentation.ViewModels.RegionPickerViewModel>(Lifestyle.Transient);
            // Note: PostRecordViewModel requires runtime parameters and should be created manually
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                _logger?.Fatal(ex, "Unhandled domain exception");
                MessageBox.Show(
                    $"A fatal error occurred:\n{ex?.Message}\n\nThe application will now close.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (s, e) =>
            {
                _logger?.Error(e.Exception, "Unhandled dispatcher exception");
                MessageBox.Show(
                    $"An error occurred:\n{e.Exception.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                e.Handled = true;
            };
        }
    }
}

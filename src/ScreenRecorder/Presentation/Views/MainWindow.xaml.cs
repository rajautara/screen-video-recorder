using System;
using System.Windows;
using System.Windows.Interop;
using ScreenRecorder.Infrastructure.Hotkeys;
using ScreenRecorder.Presentation.ViewModels;

namespace ScreenRecorder.Presentation.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IGlobalHotkeyService _hotkeyService;

        public MainWindow()
        {
            InitializeComponent();

            // Get services from DI container
            _viewModel = App.Container.GetInstance<MainViewModel>();
            _hotkeyService = App.Container.GetInstance<IGlobalHotkeyService>();

            DataContext = _viewModel;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize hotkey service with window handle
            var helper = new WindowInteropHelper(this);
            _hotkeyService.Initialize(helper.Handle);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _hotkeyService?.Dispose();
        }
    }
}

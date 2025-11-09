using System;
using System.Drawing;
using System.Windows.Forms;
using Serilog;

namespace ScreenRecorder.Presentation.Services
{
    public class TrayService : ITrayService
    {
        private readonly ILogger _logger;
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;

        public event EventHandler TrayIconClicked;
        public event EventHandler ExitRequested;

        public TrayService(ILogger logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "Screen Recorder",
                Visible = true
            };

            // Set default icon (you'll need to add an icon file)
            try
            {
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Icons", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    // Use default system icon
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // Create context menu
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Show", null, (s, e) => TrayIconClicked?.Invoke(this, EventArgs.Empty));
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Start Recording", null, OnStartRecordingClicked);
            _contextMenu.Items.Add("Stop Recording", null, OnStopRecordingClicked);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Settings", null, OnSettingsClicked);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Exit", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.DoubleClick += (s, e) => TrayIconClicked?.Invoke(this, EventArgs.Empty);

            _logger.Information("Tray icon initialized");
        }

        public void ShowBalloonTip(string title, string text)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
        }

        public void UpdateIcon(string iconPath)
        {
            if (_notifyIcon != null && System.IO.File.Exists(iconPath))
            {
                _notifyIcon.Icon = new Icon(iconPath);
            }
        }

        public void UpdateTooltip(string tooltip)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = tooltip;
            }
        }

        private void OnStartRecordingClicked(object sender, EventArgs e)
        {
            // This will be wired up to the recording controller in the MainViewModel
        }

        private void OnStopRecordingClicked(object sender, EventArgs e)
        {
            // This will be wired up to the recording controller in the MainViewModel
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            // This will be wired up to show settings in the MainViewModel
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            _contextMenu?.Dispose();
            _logger.Information("Tray service disposed");
        }
    }
}

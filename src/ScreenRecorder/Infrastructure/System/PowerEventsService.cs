using System;
using Microsoft.Win32;
using Serilog;

namespace ScreenRecorder.Infrastructure.System
{
    public class PowerEventsService : IPowerEventsService
    {
        private readonly ILogger _logger;
        private bool _isMonitoring;

        public event EventHandler SystemSuspending;
        public event EventHandler SystemResuming;
        public event EventHandler PowerModeChanged;

        public PowerEventsService(ILogger logger)
        {
            _logger = logger;
        }

        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _isMonitoring = true;
            _logger.Information("Power events monitoring started");
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            _isMonitoring = false;
            _logger.Information("Power events monitoring stopped");
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            _logger.Information("Power mode changed: {Mode}", e.Mode);

            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    SystemSuspending?.Invoke(this, EventArgs.Empty);
                    break;

                case PowerModes.Resume:
                    SystemResuming?.Invoke(this, EventArgs.Empty);
                    break;

                case PowerModes.StatusChange:
                    PowerModeChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}

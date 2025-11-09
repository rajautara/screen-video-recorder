using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ScreenRecorder.Domain.Models;
using ScreenRecorder.Infrastructure.Services;
using ScreenRecorder.Infrastructure.Settings;
using Serilog;

namespace ScreenRecorder.Presentation.ViewModels
{
    public class RegionPickerViewModel : ViewModelBase
    {
        private readonly IRegionSelectionService _regionSelectionService;
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;

        private Rectangle _selectedRegion;
        private bool _isSelectingRegion;

        public RegionPickerViewModel(
            IRegionSelectionService regionSelectionService,
            ILogger logger,
            IAppSettings settings)
        {
            _regionSelectionService = regionSelectionService ?? throw new ArgumentNullException(nameof(regionSelectionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            // Initialize commands
            StartSelectionCommand = new RelayCommand(StartSelection);
            ConfirmSelectionCommand = new RelayCommand(ConfirmSelection, CanConfirmSelection);
            CancelSelectionCommand = new RelayCommand(CancelSelection);
        }

        public Rectangle SelectedRegion
        {
            get => _selectedRegion;
            private set => SetProperty(ref _selectedRegion, value);
        }

        public bool IsSelectingRegion
        {
            get => _isSelectingRegion;
            private set => SetProperty(ref _isSelectingRegion, value);
        }

        public ICommand StartSelectionCommand { get; }
        public ICommand ConfirmSelectionCommand { get; }
        public ICommand CancelSelectionCommand { get; }

        public event EventHandler<Rectangle> RegionSelected;
        public event EventHandler SelectionCancelled;

        private void StartSelection()
        {
            try
            {
                // Store the current cursor position
                var cursorPos = System.Windows.Forms.Cursor.Position;
                var screen = System.Windows.Forms.Screen.FromPoint(cursorPos);
                
                // Initialize with a default region (e.g., 800x600 centered on cursor)
                var defaultWidth = Math.Min(800, screen.Bounds.Width - 100);
                var defaultHeight = Math.Min(600, screen.Bounds.Height - 100);
                
                var x = Math.Max(screen.Bounds.Left, cursorPos.X - defaultWidth / 2);
                var y = Math.Max(screen.Bounds.Top, cursorPos.Y - defaultHeight / 2);
                
                // Ensure the region stays within screen bounds
                x = Math.Min(x, screen.Bounds.Right - defaultWidth);
                y = Math.Min(y, screen.Bounds.Bottom - defaultHeight);
                
                SelectedRegion = new Rectangle(x, y, defaultWidth, defaultHeight);
                IsSelectingRegion = true;

                // Start the region selection process
                _regionSelectionService.StartRegionSelection(SelectedRegion, OnRegionUpdated);
                
                _logger.Debug("Started region selection");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to start region selection");
                // Show error to user
            }
        }

        private void OnRegionUpdated(Rectangle newRegion)
        {
            // Update the selected region when the user is dragging
            SelectedRegion = newRegion;
            ((RelayCommand)ConfirmSelectionCommand).RaiseCanExecuteChanged();
        }

        private bool CanConfirmSelection()
        {
            // Only allow confirming if we have a valid selection
            return SelectedRegion.Width > 10 && SelectedRegion.Height > 10;
        }

        private void ConfirmSelection()
        {
            try
            {
                if (SelectedRegion.Width <= 10 || SelectedRegion.Height <= 10)
                {
                    _logger.Warning("Selected region is too small");
                    return;
                }

                // Save the selected region to settings if needed
                _settings.LastSelectedRegion = SelectedRegion;
                
                _logger.Information("Region selected: {Region}", SelectedRegion);
                
                // Notify subscribers that a region was selected
                RegionSelected?.Invoke(this, SelectedRegion);
                
                // Clean up
                _regionSelectionService.StopRegionSelection();
                IsSelectingRegion = false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to confirm region selection");
                // Show error to user
            }
        }

        private void CancelSelection()
        {
            try
            {
                _logger.Debug("Region selection cancelled");
                
                // Clean up
                _regionSelectionService.StopRegionSelection();
                IsSelectingRegion = false;
                
                // Notify subscribers that selection was cancelled
                SelectionCancelled?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while cancelling region selection");
                // Show error to user
            }
        }
    }
}

using System;
using System.Drawing;

namespace ScreenRecorder.Infrastructure.Services
{
    /// <summary>
    /// Service for selecting screen regions for recording
    /// </summary>
    public class RegionSelectionService : IRegionSelectionService
    {
        private Rectangle _selectedRegion;
        private Action<Rectangle> _onRegionUpdated;
        private bool _isSelecting;

        public void StartRegionSelection(Rectangle initialRegion, Action<Rectangle> onRegionUpdated)
        {
            _selectedRegion = initialRegion;
            _onRegionUpdated = onRegionUpdated;
            _isSelecting = true;

            // TODO: Implement actual region selection UI
            // For now, just use the initial region
            _onRegionUpdated?.Invoke(_selectedRegion);
        }

        public void StopRegionSelection()
        {
            _isSelecting = false;
            _onRegionUpdated = null;
        }

        public Rectangle GetSelectedRegion()
        {
            return _selectedRegion;
        }
    }
}

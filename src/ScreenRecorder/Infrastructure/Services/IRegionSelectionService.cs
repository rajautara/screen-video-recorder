using System;
using System.Drawing;

namespace ScreenRecorder.Infrastructure.Services
{
    /// <summary>
    /// Service for selecting screen regions for recording
    /// </summary>
    public interface IRegionSelectionService
    {
        void StartRegionSelection(Rectangle initialRegion, Action<Rectangle> onRegionUpdated);
        void StopRegionSelection();
        Rectangle GetSelectedRegion();
    }
}

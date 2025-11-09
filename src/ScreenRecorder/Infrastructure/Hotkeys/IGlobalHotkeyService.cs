using ScreenRecorder.Domain.Models;
using System;
using System.Collections.Generic;

namespace ScreenRecorder.Infrastructure.Hotkeys
{
    public interface IGlobalHotkeyService : IDisposable
    {
        void Initialize(IntPtr windowHandle);
        void RegisterHotkey(Hotkey hotkey);
        void UnregisterHotkey(HotkeyAction action);
        void UnregisterAll();
        event EventHandler<HotkeyAction> HotkeyPressed;
        bool IsHotkeyAvailable(Hotkey hotkey);
    }
}

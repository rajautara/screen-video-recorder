using ScreenRecorder.Domain.Models;
using System;
using System.Collections.Generic;

namespace ScreenRecorder.Infrastructure.Hotkeys
{
    public interface IGlobalHotkeyService
    {
        void RegisterHotkey(Hotkey hotkey);
        void UnregisterHotkey(HotkeyAction action);
        void UnregisterAll();
        event EventHandler<HotkeyAction> HotkeyPressed;
        bool IsHotkeyAvailable(Hotkey hotkey);
    }
}

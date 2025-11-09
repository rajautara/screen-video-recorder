using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using ScreenRecorder.Domain.Models;
using Serilog;

namespace ScreenRecorder.Infrastructure.Hotkeys
{
    public class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly Dictionary<int, HotkeyAction> _registeredHotkeys;
        private IntPtr _windowHandle;
        private HwndSource _source;
        private int _currentId;

        public event EventHandler<HotkeyAction> HotkeyPressed;

        private const int WM_HOTKEY = 0x0312;

        public GlobalHotkeyService(ILogger logger)
        {
            _logger = logger;
            _registeredHotkeys = new Dictionary<int, HotkeyAction>();
            _currentId = 1;
        }

        public void Initialize(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(WndProc);
            _logger.Information("GlobalHotkeyService initialized");
        }

        public void RegisterHotkey(Hotkey hotkey)
        {
            if (!hotkey.IsEnabled)
                return;

            try
            {
                var modifiers = GetModifiers(hotkey.Modifiers);
                var key = KeyInterop.VirtualKeyFromKey(hotkey.Key);

                if (RegisterHotKey(_windowHandle, _currentId, modifiers, (uint)key))
                {
                    _registeredHotkeys[_currentId] = hotkey.Action;
                    _logger.Information("Registered hotkey: {Hotkey} for action {Action}",
                        hotkey.ToString(), hotkey.Action);
                    _currentId++;
                }
                else
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.Warning("Failed to register hotkey {Hotkey}: Error {Error}",
                        hotkey.ToString(), error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to register hotkey {Hotkey}", hotkey.ToString());
            }
        }

        public void UnregisterHotkey(HotkeyAction action)
        {
            try
            {
                var id = -1;
                foreach (var kvp in _registeredHotkeys)
                {
                    if (kvp.Value == action)
                    {
                        id = kvp.Key;
                        break;
                    }
                }

                if (id >= 0)
                {
                    UnregisterHotKey(_windowHandle, id);
                    _registeredHotkeys.Remove(id);
                    _logger.Information("Unregistered hotkey for action {Action}", action);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to unregister hotkey for action {Action}", action);
            }
        }

        public void UnregisterAll()
        {
            try
            {
                foreach (var id in _registeredHotkeys.Keys)
                {
                    UnregisterHotKey(_windowHandle, id);
                }

                _registeredHotkeys.Clear();
                _logger.Information("Unregistered all hotkeys");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to unregister all hotkeys");
            }
        }

        public bool IsHotkeyAvailable(Hotkey hotkey)
        {
            var modifiers = GetModifiers(hotkey.Modifiers);
            var key = KeyInterop.VirtualKeyFromKey(hotkey.Key);
            var testId = 9999;

            if (RegisterHotKey(_windowHandle, testId, modifiers, (uint)key))
            {
                UnregisterHotKey(_windowHandle, testId);
                return true;
            }

            return false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (_registeredHotkeys.TryGetValue(id, out var action))
                {
                    _logger.Debug("Hotkey pressed: {Action}", action);
                    HotkeyPressed?.Invoke(this, action);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private uint GetModifiers(ModifierKeys modifiers)
        {
            uint result = 0;

            if (modifiers.HasFlag(ModifierKeys.Alt))
                result |= MOD_ALT;
            if (modifiers.HasFlag(ModifierKeys.Control))
                result |= MOD_CONTROL;
            if (modifiers.HasFlag(ModifierKeys.Shift))
                result |= MOD_SHIFT;
            if (modifiers.HasFlag(ModifierKeys.Windows))
                result |= MOD_WIN;

            return result;
        }

        public void Dispose()
        {
            try
            {
                UnregisterAll();
                _source?.RemoveHook(WndProc);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error disposing GlobalHotkeyService");
            }
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
    }
}

using System;
using System.Windows.Input;

namespace ScreenRecorder.Domain.Models
{
    public class Hotkey : IEquatable<Hotkey>
    {
        public HotkeyAction Action { get; set; }
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public bool IsEnabled { get; set; }

        public Hotkey()
        {
            IsEnabled = true;
        }

        public Hotkey(HotkeyAction action, Key key, ModifierKeys modifiers)
        {
            Action = action;
            Key = key;
            Modifiers = modifiers;
            IsEnabled = true;
        }

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(Key.ToString());

            return string.Join("+", parts);
        }

        public bool Equals(Hotkey other)
        {
            if (other == null) return false;
            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Hotkey);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Modifiers);
        }
    }
}

using System.Windows.Input;

namespace Snake
{
    /// <summary>Turns WPF key names into short labels and back.</summary>
    public static class KeyNames
    {
        private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Up"] = "↑",
            ["Down"] = "↓",
            ["Left"] = "←",
            ["Right"] = "→",
            ["Space"] = "SPACE",
            ["Return"] = "ENTER",
            ["Back"] = "BACKSPACE",
            ["LeftShift"] = "L-SHIFT",
            ["RightShift"] = "R-SHIFT",
            ["LeftCtrl"] = "L-CTRL",
            ["RightCtrl"] = "R-CTRL",
            ["OemComma"] = ",",
            ["OemPeriod"] = ".",
            ["OemMinus"] = "-",
            ["OemPlus"] = "+",
        };

        public static string Display(string key)
        {
            if (Labels.TryGetValue(key, out string? label))
            {
                return label;
            }
            // D0..D9 are the number row, NumPad0..9 stay as they are.
            if (key.Length == 2 && key[0] == 'D' && char.IsDigit(key[1]))
            {
                return key[1].ToString();
            }
            return key.ToUpperInvariant();
        }

        public static bool TryParse(string name, out Key key) =>
            Enum.TryParse(name, ignoreCase: true, out key) && key != Key.None;

        /// <summary>The key that was really pressed, including Alt combinations that WPF reports as <see cref="Key.System"/>.</summary>
        public static Key RealKey(KeyEventArgs e) => e.Key == Key.System ? e.SystemKey : e.Key;
    }
}

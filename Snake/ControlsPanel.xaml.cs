using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Snake.Core;

namespace Snake
{
    /// <summary>
    /// Shows every key binding; click one and press a key to change it.
    /// </summary>
    public partial class ControlsPanel : UserControl
    {
        private const string IdleHint = "CLICK A KEY TO CHANGE IT · ESC GOES BACK";

        private static readonly (string Label, GameAction P1, GameAction P2)[] MovementRows =
        [
            ("UP", GameAction.P1Up, GameAction.P2Up),
            ("DOWN", GameAction.P1Down, GameAction.P2Down),
            ("LEFT", GameAction.P1Left, GameAction.P2Left),
            ("RIGHT", GameAction.P1Right, GameAction.P2Right),
        ];

        private static readonly (string Label, GameAction Action)[] GeneralActions =
        [
            ("PAUSE", GameAction.Pause),
            ("SCREENSHOT", GameAction.Screenshot),
            ("FULLSCREEN", GameAction.Fullscreen),
        ];

        private readonly Dictionary<GameAction, Button> buttons = new();
        private KeyBindings bindings = KeyBindings.Default;
        private GameAction? capturing;

        public ControlsPanel()
        {
            InitializeComponent();
            BuildRows();
        }

        public event EventHandler<KeyBindings>? Done;

        public bool IsCapturing => capturing is not null;

        public void Load(KeyBindings current)
        {
            bindings = current;
            capturing = null;
            HintText.Text = IdleHint;
            Refresh();
            buttons[GameAction.P1Up].Focus();
        }

        private void BuildRows()
        {
            MovementGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCell(MovementGrid, Caption("PLAYER 1", "AccentBrush"), 0, 1);
            AddCell(MovementGrid, Caption("PLAYER 2", "Accent2Brush"), 0, 2);

            for (int i = 0; i < MovementRows.Length; i++)
            {
                var (label, p1, p2) = MovementRows[i];
                MovementGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
                AddCell(MovementGrid, Caption(label, "MutedTextColor"), i + 1, 0);
                AddCell(MovementGrid, KeyButton(p1), i + 1, 1);
                AddCell(MovementGrid, KeyButton(p2), i + 1, 2);
            }

            GeneralGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            GeneralGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            for (int i = 0; i < GeneralActions.Length; i++)
            {
                AddCell(GeneralGrid, Caption(GeneralActions[i].Label, "MutedTextColor"), 0, i);
                AddCell(GeneralGrid, KeyButton(GeneralActions[i].Action), 1, i);
            }
        }

        private TextBlock Caption(string text, string brushKey) => new()
        {
            Text = text,
            Style = (Style)FindResource("Caption"),
            Foreground = (System.Windows.Media.Brush)FindResource(brushKey),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
        };

        private Button KeyButton(GameAction action)
        {
            var button = new Button
            {
                Style = (Style)FindResource("KeyButton"),
                Name = action + "Key",
                Tag = action,
            };
            button.Click += KeyButton_Click;
            buttons[action] = button;
            return button;
        }

        private static void AddCell(Grid grid, UIElement element, int row, int column)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }

        private void Refresh()
        {
            foreach ((GameAction action, Button button) in buttons)
            {
                button.Content = action == capturing ? "PRESS…" : KeyNames.Display(bindings[action]);
            }
        }

        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            capturing = (GameAction)((Button)sender).Tag;
            HintText.Text = "PRESS THE NEW KEY · ESC CANCELS";
            Refresh();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (capturing is not GameAction action)
            {
                return;
            }

            e.Handled = true;
            Key key = KeyNames.RealKey(e);
            capturing = null;
            if (key == Key.Escape)
            {
                HintText.Text = IdleHint;
            }
            else
            {
                KeyBindings updated = bindings.Rebind(action, key.ToString());
                HintText.Text = ReferenceEquals(updated, bindings) && bindings[action] != key.ToString()
                    ? $"{KeyNames.Display(key.ToString())} CAN'T BE USED THERE"
                    : IdleHint;
                bindings = updated;
            }
            Refresh();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            bindings = KeyBindings.Default;
            capturing = null;
            HintText.Text = IdleHint;
            Refresh();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e) => Done?.Invoke(this, bindings);
    }
}

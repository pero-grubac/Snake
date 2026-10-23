using System.Windows;
using System.Windows.Controls;
using Snake.Core;

namespace Snake
{
    /// <summary>
    /// Map size, speed, walls and sound.
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        private UserSettings original = UserSettings.Default;

        public SettingsPanel()
        {
            InitializeComponent();
            HeightSlider.Minimum = WidthSlider.Minimum = MapSize.Min;
            HeightSlider.Maximum = WidthSlider.Maximum = MapSize.Max;
        }

        public event EventHandler<UserSettings>? Saved;
        public event EventHandler? Cancelled;

        public void Load(UserSettings settings)
        {
            original = settings;
            HeightSlider.Value = settings.Map.Rows;
            WidthSlider.Value = settings.Map.Columns;
            SpeedSlow.IsChecked = settings.Speed == SpeedLevel.Slow;
            SpeedNormal.IsChecked = settings.Speed == SpeedLevel.Normal;
            SpeedFast.IsChecked = settings.Speed == SpeedLevel.Fast;
            SpeedProgressive.IsChecked = settings.Speed == SpeedLevel.Progressive;
            WallsSolid.IsChecked = settings.Walls == WallMode.Solid;
            WallsWrap.IsChecked = settings.Walls == WallMode.Wrap;
            SoundSwitch.IsChecked = settings.Sound;
            UpdateLabels();
            HeightSlider.Focus();
        }

        private SpeedLevel SelectedSpeed =>
            SpeedSlow.IsChecked == true ? SpeedLevel.Slow
            : SpeedFast.IsChecked == true ? SpeedLevel.Fast
            : SpeedProgressive.IsChecked == true ? SpeedLevel.Progressive
            : SpeedLevel.Normal;

        private void UpdateLabels()
        {
            if (HeightValue is null || WidthValue is null)
            {
                return;
            }
            HeightValue.Text = ((int)HeightSlider.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            WidthValue.Text = ((int)WidthSlider.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateLabels();

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Saved?.Invoke(this, original with
            {
                Map = new MapSize((int)HeightSlider.Value, (int)WidthSlider.Value),
                Speed = SelectedSpeed,
                Walls = WallsWrap.IsChecked == true ? WallMode.Wrap : WallMode.Solid,
                Sound = SoundSwitch.IsChecked == true,
            });
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => Cancelled?.Invoke(this, EventArgs.Empty);
    }
}

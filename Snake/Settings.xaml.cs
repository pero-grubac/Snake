using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Snake
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private MainWindow mainWindow;
        public Settings(MainWindow main)
        {
            InitializeComponent();
            mainWindow = main;
            HeightSliderValue = mainWindow.Rows;
            WidthSliderValue = mainWindow.Columns;
        }
        public double HeightSliderValue
        {
            get { return HeightSlider.Value; }
            set { HeightSlider.Value = value; }
        }

        public double WidthSliderValue
        {
            get { return WidthSlider.Value; }
            set { WidthSlider.Value = value; }
        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Rows = (int)HeightSlider.Value;
            mainWindow.Columns = (int)WidthSlider.Value;
            this.Close();
        }

    }
}

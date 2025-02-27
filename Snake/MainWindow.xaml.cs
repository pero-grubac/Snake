﻿


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Snake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private int rows = 15;
        private int columns = 15;
        private Dictionary<Tuple<int, int>, int> data = new Dictionary<Tuple<int, int>, int>();
        public int Rows
        {
            get { return rows; }
            set { rows = value; }
        }

        public int Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        private Image[,] gridImages;
        private int highestScore;
        private readonly Dictionary<GridValue, ImageSource> gridValueToImage = new Dictionary<GridValue, ImageSource>
        {
            {GridValue.Emplty,Images.Empty },
            {GridValue.Snake,Images.Body },
            {GridValue.Food,Images.Food }
        };
        private readonly Dictionary<Direction, int> directionToRotation = new Dictionary<Direction, int>
        {
            {Direction.Up,0 },
            {Direction.Right,90 },
            {Direction.Down,180 },
            {Direction.Left,270 }
        };

        private GameState gameState;
        private bool gameRunning;
        private bool isPaused = false;

        private static string directory = Directory.GetCurrentDirectory();
        private static string pathToScore = System.IO.Path.Combine(directory, "score.txt");
        private static string screenshotDirectory = System.IO.Path.Combine(directory, "Screenshots");
        public MainWindow()
        {
            InitializeComponent();
            gridImages = SetupGrid();
            gameState = new GameState(rows, columns);
            ReadData();
            ReadHighestScore();
            MapText.Text = $"{rows}X{columns}";
            ScoreText.Text = $" CURRENT SCORE 0 HIGHEST SCORE {highestScore}";
        }
        private void ReadHighestScore()
        {
            Tuple<int, int> map = new Tuple<int, int>(rows, columns);
            if (data.ContainsKey(map))
            {
                highestScore = data[map];
            }
            else
            {
                data[map] = 0;
                highestScore = 0;
            }
        }
        private void AddScore()
        {
            Tuple<int, int> map = new Tuple<int, int>(rows, columns);
            data[map] = highestScore;
        }
        private Image[,] SetupGrid()
        {
            Image[,] images = new Image[rows, columns];
            GameGrid.Rows = rows;
            GameGrid.Columns = columns;
            GameGrid.Width = GameGrid.Height * (columns / (double)rows);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Image image = new Image
                    {
                        Source = Images.Empty,
                        RenderTransformOrigin = new Point(0.5, 0.5),

                    };
                    images[r, c] = image;
                    GameGrid.Children.Add(image);
                }
            }
            return images;
        }
        private void DrawGrid()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    GridValue gridValue = gameState.GridValues[r, c];
                    gridImages[r, c].Source = gridValueToImage[gridValue];
                    gridImages[r, c].RenderTransform = Transform.Identity;
                }
            }
        }
        private void Draw()
        {
            DrawGrid();
            DrawSnakeHad();
            MapText.Text = $"{rows}X{columns}";
            ScoreText.Text = $"CURRENT SCORE {gameState.Score} HIGHEST SCORE {highestScore}";

        }
        private void ReadData()
        {
            try
            {
                if (File.Exists(pathToScore))
                {
                    string[] lines = File.ReadAllLines(pathToScore);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(';');
                        if (parts.Length == 3 && int.TryParse(parts[0], out int height) &&
                    int.TryParse(parts[1], out int width) &&
                    int.TryParse(parts[2], out int score))
                        {
                            Tuple<int, int> key = new Tuple<int, int>(height, width);
                            data[key] = score;
                        }
                        else
                        {
                            File.WriteAllText(pathToScore, "15;15;0");
                        }
                    }
                }
                else
                {
                    File.WriteAllText(pathToScore, "15;15;0");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private void WriteHighestScore()
        {
            using (StreamWriter writer = new StreamWriter(pathToScore))
            {
                foreach (var line in data)
                {
                    writer.WriteLine($"{line.Key.Item1};{line.Key.Item2};{line.Value}");
                }
            }
        }
        private void DrawSnakeHad()
        {
            Position headPosition = gameState.HeadPosition();
            Image image = gridImages[headPosition.Row, headPosition.Column];
            image.Source = Images.Head;

            int rotation = directionToRotation[gameState.Direction];
            image.RenderTransform = new RotateTransform(rotation);
        }
        private async Task DrawDeadSnake()
        {
            List<Position> positions = new List<Position>(gameState.SnakePositions());

            for (int i = 0; i < positions.Count; i++)
            {
                Position position = positions[i];
                ImageSource source = (i == 0) ? Images.DeadHead : Images.DeadBody;
                gridImages[position.Row, position.Column].Source = source;
                await Task.Delay(50);
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameState.GameOver)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                    gameState.ChangeDirection(Direction.Left);
                    break;
                case Key.Right:
                    gameState.ChangeDirection(Direction.Right);
                    break;
                case Key.Up:
                    gameState.ChangeDirection(Direction.Up);
                    break;
                case Key.Down:
                    gameState.ChangeDirection(Direction.Down);
                    break;
                case Key.P:
                    gameState.SwitchPause();
                    break;
                case Key.S:
                    CaptureScreenshot();
                    break;
                case Key.F:
                    ToggleFullScreen();
                    break;
            }
        }
        private void ToggleFullScreen()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CaptureScreenshot()
        {
            string fileName = "snake_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
            string filePath = Path.Combine(screenshotDirectory, fileName);
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;
            RenderTargetBitmap renderTarget = new RenderTargetBitmap(screenWidth, screenHeight, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(this);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));
            if (Directory.Exists(screenshotDirectory))
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }

            }
            else
            {
                Directory.CreateDirectory(screenshotDirectory);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
        }
        private async Task GameLoop()
        {
            while (!gameState.GameOver)
            {
                await Task.Delay(100);
                gameState.Move();
                Draw();

            }
        }
        private async Task RunGame()
        {
            Draw();
            await ShowCountDown();
            Overlay.Visibility = Visibility.Hidden;
            await GameLoop();
            await ShowGameOver();
            gameState = new GameState(rows, columns);
        }
        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Overlay.Visibility == Visibility.Visible)
            {
                e.Handled = true;
            }
            if (!gameRunning)
            {
                gameRunning = true;
                await RunGame();
                gameRunning = false;
            }
        }
        private async Task ShowCountDown()
        {
            for (int i = 3; i >= 1; i--)
            {
                OverlayText.Text = i.ToString();
                await Task.Delay(500);
            }
        }
        private async Task ShowGameOver()
        {
            await DrawDeadSnake();
            await Task.Delay(1000);
            Overlay.Visibility = Visibility.Visible;
            if (gameState.Score < highestScore)
            {
                OverlayText.Text = "GAME OVER \n PRESS ANY KEY TO START";
            }
            else
            {
                highestScore = gameState.Score;
                OverlayText.Text = $"NEW HIGHEST SCORE {highestScore}\nPRESS ANY KEY TO START";
                AddScore();
                ReadHighestScore();
                WriteHighestScore();
            }
            OverlayText.TextAlignment = TextAlignment.Center;
        }
        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Settings settingsWindow = new Settings(this);
            settingsWindow.ShowDialog();
            gameState = new GameState(rows, columns);
            ReadHighestScore();
            GameGrid.Children.Clear();
            gridImages = SetupGrid();

            await RunGame();
        }
    }
}

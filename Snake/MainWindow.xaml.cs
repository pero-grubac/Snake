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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int rows = 15;
        private readonly int columns = 15;
        private readonly Image[,] gridImages;

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
        public MainWindow()
        {
            InitializeComponent();
            gridImages = SetupGrid();
            gameState = new GameState(rows, columns);
        }
        private Image[,] SetupGrid()
        {
            Image[,] images = new Image[rows, columns];
            GameGrid.Rows = rows;
            GameGrid.Columns = columns;
            GameGrid.Width = GameGrid.Height*(columns/(double)rows);

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
            ScoreText.Text = $"SCORE {gameState.Score}";

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
            DrawDeadSnake(); 
            await Task.Delay(1000);
            Overlay.Visibility = Visibility.Visible;
            
            OverlayText.Text = "GAME OVER \n PRESS ANY KEY TO START";
            OverlayText.TextAlignment = TextAlignment.Center;
        }


    }
}

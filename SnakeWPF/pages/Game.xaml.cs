using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SnakeWPF.Pages
{
    public partial class Game : Page
    {
        // Кэшированные ресурсы
        private readonly SolidColorBrush _headBrush = new SolidColorBrush(Color.FromRgb(31, 71, 15));
        private readonly SolidColorBrush _bodyBrush = new SolidColorBrush(Color.FromRgb(57, 99, 41));
        private readonly ImageBrush _appleBrush;

        // Кэш элементов
        private readonly Dictionary<Snakes, List<Rectangle>> _snakeElementsCache = new Dictionary<Snakes, List<Rectangle>>();
        private Ellipse _appleElement;

        public Game()
        {
            InitializeComponent();

            _appleBrush = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri("pack://application:,,,/Image/apple.png"))
            };

            _headBrush.Freeze();
            _bodyBrush.Freeze();
            _appleBrush.Freeze();
        }

        public void CreateUI()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    canvas.Children.Clear();

                    // Просто отрисовываем текущее состояние без кэширования
                    if (MainWindow.mainWindow.ViewModelGames?.SnakesPlayers != null)
                    {
                        RenderSnake(MainWindow.mainWindow.ViewModelGames.SnakesPlayers);
                    }

                    RenderApple();

                    if (MainWindow.mainWindow.ViewModelGamesList != null)
                    {
                        foreach (var gameState in MainWindow.mainWindow.ViewModelGamesList)
                        {
                            if (gameState?.SnakesPlayers != null)
                            {
                                RenderSnake(gameState.SnakesPlayers);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateUI: {ex.Message}");
            }
        }

        private void RenderSnake(Snakes snake)
        {
            if (snake?.Points == null || snake.Points.Count == 0)
                return;

            for (int i = 0; i < snake.Points.Count; i++)
            {
                var point = snake.Points[i];
                var isHead = i == 0;

                var rectangle = new Rectangle()
                {
                    Width = 10,
                    Height = 10,
                    Margin = new Thickness(point.X, point.Y, 0, 0), // Убраны смещения -5
                    Fill = isHead ? _headBrush : _bodyBrush,
                    Stroke = Brushes.Black
                };

                canvas.Children.Add(rectangle);
            }
        }

        private void RenderApple()
        {
            if (MainWindow.mainWindow.ViewModelGames?.Points == null)
                return;

            var apple = new Ellipse()
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(
                    MainWindow.mainWindow.ViewModelGames.Points.X,
                    MainWindow.mainWindow.ViewModelGames.Points.Y, 0, 0), // Убраны смещения -15
                Fill = _appleBrush
            };

            canvas.Children.Add(apple);
        }
    }
}
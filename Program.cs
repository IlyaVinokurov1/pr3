using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Snake
{
    class Program
    {
        public static List<Leaders> Leaders = new List<Leaders>(); // Исправлено: Leadres -> Leaders
        public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        private static int localPort = 5001;
        public static int MaxSpeed = 15;

        static void Main(string[] args)
        {
            try
            {
                Thread tRec = new Thread(new ThreadStart(Receiver));
                tRec.Start();

                Thread tTime = new Thread(Timer);
                tTime.Start();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }

        private static void Send()
        {
            List<ViewModelUserSettings> _ips = new List<ViewModelUserSettings>(remoteIPAddress); // Копируем список для безопасной итерации

            foreach (ViewModelUserSettings User in _ips)
            {
                UdpClient sender = new UdpClient();
                IPEndPoint endPoint = new IPEndPoint(
                    IPAddress.Parse(User.IPAddress),
                    int.Parse(User.Port));
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelGames.Find(x => x.IdSnake == User.IdSnake)));
                    sender.Send(bytes, bytes.Length, endPoint);

                    // Отправляем также список других змей
                    bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelGames.FindAll(x => x.IdSnake != User.IdSnake)));
                    sender.Send(bytes, bytes.Length, endPoint);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Отправил данные пользователю: {User.IPAddress}:{User.Port}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Возникло исключение: " + ex.ToString() + "\n " + ex.Message);
                }
                finally
                {
                    sender.Close();
                }
            }
        }

        public static void Receiver()
        {
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;
            try
            {
                Console.WriteLine("Команды сервера:");
                while (true)
                {
                    byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Получил команду: " + returnData.ToString()); // Исправлено: Получи -> Получил

                    if (returnData.ToString().Contains("/start"))
                    {
                        string[] dataMessage = returnData.ToString().Split('|');
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Подключился пользователь: {viewModelUserSettings.IPAddress}:{viewModelUserSettings.Port}");
                        remoteIPAddress.Add(viewModelUserSettings);
                        viewModelUserSettings.IdSnake = AddSnake();
                        viewModelGames[viewModelUserSettings.IdSnake].IdSnake = viewModelUserSettings.IdSnake;
                    }
                    else
                    {
                        string[] dataMessage = returnData.ToString().Split('|');
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        int IdPlayer = -1;
                        IdPlayer = remoteIPAddress.FindIndex(x => x.IPAddress == viewModelUserSettings.IPAddress
                        && x.Port == viewModelUserSettings.Port);

                        if (IdPlayer != -1)
                        {
                            if (dataMessage[0] == "Up" &&
                               viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Down) // Исправлено: SnakesPlayers -> SnakesPlayer
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Up;
                            else if (dataMessage[0] == "Down" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Up)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Down;
                            else if (dataMessage[0] == "Left" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Right)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Left;
                            else if (dataMessage[0] == "Right" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Left)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Right;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n " + ex.Message);
            }
        }

        public static int AddSnake()
        {
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();
            viewModelGamesPlayer.SnakesPlayers = new Snakes() // Исправлено: SnakesPlayers -> SnakesPlayer
            {
                Points = new List<Snakes.Point>()
                {
                    new Snakes.Point() { X = 30, Y = 10 },
                    new Snakes.Point() { X = 20, Y = 10 },
                    new Snakes.Point() { X = 10, Y = 10 },
                },
                direction = Snakes.Direction.Start
            };
            viewModelGamesPlayer.Points = new Snakes.Point(new Random().Next(10, 783), new Random().Next(10, 410));
            viewModelGames.Add(viewModelGamesPlayer);
            return viewModelGames.FindIndex(x => x == viewModelGamesPlayer);
        }

        public static void Timer()
        {
            while (true)
            {
                Thread.Sleep(100);

                // Удаляем отключившихся пользователей
                List<ViewModelGames> RemoteSnakes = viewModelGames.FindAll(x => x.SnakesPlayers.GameOver); // Исправлено: SnakesPlayers -> SnakesPlayer
                if (RemoteSnakes.Count > 0)
                {
                    foreach (ViewModelGames DeadSnake in RemoteSnakes)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        var user = remoteIPAddress.Find(x => x.IdSnake == DeadSnake.IdSnake);
                        if (user != null)
                        {
                            Console.WriteLine($"Отключил пользователя: {user.IPAddress}:{user.Port}");
                            remoteIPAddress.RemoveAll(x => x.IdSnake == DeadSnake.IdSnake);
                        }
                    }
                    viewModelGames.RemoveAll(x => x.SnakesPlayers.GameOver); // Исправлено: вынесено из цикла
                }

                // Обновляем состояние всех змей
                foreach (ViewModelUserSettings User in remoteIPAddress.ToList()) // Копируем для безопасной итерации
                {
                    var gameState = viewModelGames.Find(x => x.IdSnake == User.IdSnake);
                    if (gameState == null) continue;

                    Snakes Snake = gameState.SnakesPlayers; // Исправлено: SnakesPlayers -> SnakesPlayer

                    // Двигаем змею
                    for (int i = Snake.Points.Count - 1; i >= 0; i--)
                    {
                        if (i != 0)
                        {
                            Snake.Points[i] = Snake.Points[i - 1];
                        }
                        else
                        {
                            int Speed = 10 + (int)Math.Round(Snake.Points.Count / 20f);
                            if (Speed > MaxSpeed) Speed = MaxSpeed;

                            if (Snake.direction == Snakes.Direction.Right)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X + Speed, Y = Snake.Points[i].Y };
                            }
                            else if (Snake.direction == Snakes.Direction.Down)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X, Y = Snake.Points[i].Y + Speed };
                            }
                            else if (Snake.direction == Snakes.Direction.Up)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X, Y = Snake.Points[i].Y - Speed };
                            }
                            else if (Snake.direction == Snakes.Direction.Left)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X - Speed, Y = Snake.Points[i].Y };
                            }
                        }
                    }

                    // Проверяем границы
                    if (Snake.Points[0].X <= 0 || Snake.Points[0].X >= 793 ||
                        Snake.Points[0].Y <= 0 || Snake.Points[0].Y >= 420)
                    {
                        Snake.GameOver = true;
                    }

                    // Проверяем столкновение с собой
                    if (Snake.direction != Snakes.Direction.Start && !Snake.GameOver)
                    {
                        for (int i = 1; i < Snake.Points.Count; i++)
                        {
                            if (Math.Abs(Snake.Points[0].X - Snake.Points[i].X) < 10 &&
                                Math.Abs(Snake.Points[0].Y - Snake.Points[i].Y) < 10)
                            {
                                Snake.GameOver = true;
                                break;
                            }
                        }
                    }

                    // Проверяем сбор яблока
                    if (!Snake.GameOver &&
                        Math.Abs(Snake.Points[0].X - gameState.Points.X) < 20 &&
                        Math.Abs(Snake.Points[0].Y - gameState.Points.Y) < 20)
                    {
                        gameState.Points = new Snakes.Point(
                            new Random().Next(10, 783),
                            new Random().Next(10, 410));

                        // Добавляем новый сегмент
                        Snake.Points.Add(new Snakes.Point()
                        {
                            X = Snake.Points[Snake.Points.Count - 1].X,
                            Y = Snake.Points[Snake.Points.Count - 1].Y
                        });

                        // Обновляем таблицу лидеров
                        LoadLeaders();

                        Leaders.Add(new Leaders()
                        {
                            Name = User.Name,
                            Points = Snake.Points.Count - 3
                        });

                        // Исправлено: явное указание типов для OrderByDescending
                        Leaders = Leaders.OrderByDescending(x => x.Points).ThenBy(x => x.Name).ToList();

                        // Исправлено: FindIndex для списка Leaders
                        int leaderIndex = Leaders.FindIndex(x => x.Points == (Snake.Points.Count - 3) && x.Name == User.Name);
                        gameState.Top = leaderIndex + 1;
                    }

                    // Сохраняем лидеров при завершении игры
                    if (Snake.GameOver)
                    {
                        LoadLeaders();
                        Leaders.Add(new Leaders()
                        {
                            Name = User.Name,
                            Points = Snake.Points.Count - 3
                        });
                        SaveLeaders(); // Исправлено: SaveLeader -> SaveLeaders
                    }
                }

                Send();
            }
        }

        public static void LoadLeaders()
        {
            if (File.Exists("./leaders.txt")) // Исправлено: leadres.txt -> leaders.txt
            {
                StreamReader SR = new StreamReader("./leaders.txt");
                string json = SR.ReadLine();
                SR.Close();
                if (!string.IsNullOrEmpty(json))
                    Leaders = JsonConvert.DeserializeObject<List<Leaders>>(json);
                else
                    Leaders = new List<Leaders>();
            }
            else
                Leaders = new List<Leaders>();
        }

        public static void SaveLeaders() // Исправлено: SaveLeader -> SaveLeaders
        {
            string json = JsonConvert.SerializeObject(Leaders);
            StreamWriter SW = new StreamWriter("./leaders.txt"); // Исправлено: leadres.txt -> leaders.txt
            SW.WriteLine(json);
            SW.Close();
        }
    }
}
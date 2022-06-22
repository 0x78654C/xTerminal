using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Commands.TerminalCommands.Games
{
    /*
     * Autor: mkbmain
     * Link: https://github.com/mkbmain/ConsoleSnake/
     */
    public class Snake : ITerminalCommand
    {
        private static readonly Random Random = new Random(Guid.NewGuid().GetHashCode());
        private const char Food = ' ';
        private const char SnakeChar = ' ';
        private static bool _run = true;
        private const char Empty = '█';
        private static DisplayElement[][] _display;
        private static int _score;
        private const int Speed = 15;
        private static List<Point> _snake;
        private static Direction _direction = Direction.East;
        private static Direction _lastDirection = Direction.East;
        public string Name => "snake";
        public void Execute(string args)
        {
            Main();
        }
        private static void Main()
        {
            _run = true;
            Console.CursorVisible = false;
            Console.Clear();
            _display = new DisplayElement[Console.WindowWidth - 2][];
            for (var item = 0; item < _display.Length; item++)
            {
                var widthCol = new DisplayElement[Console.WindowHeight - 2];
                for (var i = 0; i < widthCol.Length; i++)
                {
                    widthCol[i] = new DisplayElement { Value = Empty, Point = new Point(item, i) };
                    OutPutDisplayItem(widthCol[i]);
                }

                _display[item] = widthCol;
            }

            GenFood();
            _snake = new List<Point>
            {
                new Point(_display.Length / 2 - 3, (Console.WindowHeight - 2) / 2),
                new Point(_display.Length / 2 - 2, (Console.WindowHeight - 2) / 2),
                new Point(_display.Length / 2 - 1, (Console.WindowHeight - 2) / 2),
                new Point(_display.Length / 2, (Console.WindowHeight - 2) / 2),
            };
            foreach (var item in _snake)
            {
                UpdateDisplayElementFromPoint(item, SnakeChar);
            }

            var task = Task.Run(GameLoop);
            while (_run)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            if (_lastDirection != Direction.South)
                            {
                                _direction = Direction.North;
                            }

                            break;
                        case ConsoleKey.DownArrow:
                            if (_lastDirection != Direction.North)
                            {
                                _direction = Direction.South;
                            }

                            break;
                        case ConsoleKey.LeftArrow:
                            if (_lastDirection != Direction.East)
                            {
                                _direction = Direction.West;
                            }

                            break;
                        case ConsoleKey.RightArrow:
                            if (_lastDirection != Direction.West)
                            {
                                _direction = Direction.East;
                            }

                            break;
                    }
                }
            }
            Console.WriteLine($"Game Over Score: {_score}");
            return;
        }

        static DisplayElement GetElementByPoint(Point point)
        {
            return _display[point.X][point.Y];
        }

        static void UpdateDisplayElementFromPoint(Point point, char value)
        {
            var item = GetElementByPoint(point);
            item.Value = value;
            OutPutDisplayItem(item);
        }

        static void GameLoop()
        {
            while (_run)
            {
                var head = _snake.Last();
                var tail = _snake.First();

                var point = new Point(head.X, head.Y);
                _lastDirection = _direction;
                switch (_direction)
                {
                    case Direction.North:
                        point.Y -= 1;
                        break;
                    case Direction.South:
                        point.Y += 1;
                        break;
                    case Direction.East:
                        point.X += 1;
                        break;
                    case Direction.West:
                        point.X -= 1;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Check if out side bounds of map or we have hit our own tail\other part of snake
                if (point.X >= _display.Length || point.Y >= _display[0].Length || point.X < 0 || point.Y < 0 ||
                    _snake.Skip(1).Contains(point))
                {
                    Console.Clear();
                    _run = false;
                    return;
                }

                _snake.Add(point);

                var item = GetElementByPoint(point);
                if (item.Value != Food)
                {
                    _snake = _snake.Skip(1).ToList();
                }
                else
                {
                    _score += Speed;
                    GenFood();
                }

                item.Value = SnakeChar;
                OutPutDisplayItem(item);
                if (item.Point.X != head.X ||
                    item.Point.Y != head.Y) // If head is same space tail was we don't want to blank it
                {
                    UpdateDisplayElementFromPoint(tail, Empty);
                }

                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"Score = {_score}");
                System.Threading.Thread.Sleep(1000 / Speed);
            }
        }

        private static void GenFood()
        {
            var freeLabels = _display.SelectMany(t => t.Where(y => y.Value == Empty)).ToArray();
            var item = freeLabels[Random.Next(0, freeLabels.Length)];
            item.Value = Food;
            Console.BackgroundColor = ConsoleColor.Red;
            OutPutDisplayItem(item);
            Console.BackgroundColor = ConsoleColor.Black;
        }

        private static void OutPutDisplayItem(DisplayElement displayElement)
        {
            Console.SetCursorPosition(displayElement.Point.X, displayElement.Point.Y + 1);
            Console.Write(displayElement.Value);
        }
    }

    public class DisplayElement
    {
        public Point Point { get; set; }
        public char Value { get; set; }
    }

    public enum Direction
    {
        North,
        South,
        East,
        West
    }
}

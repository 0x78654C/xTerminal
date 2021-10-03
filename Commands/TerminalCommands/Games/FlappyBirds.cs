using System;
using System.IO;
using System.Threading;

namespace Commands.TerminalCommands.Games
{
    public class FlappyBirds : ITerminalCommand
    {
        public string Name => "flappy";
        public ConsoleKeyInfo keypress = new ConsoleKeyInfo();
        public Random rand = new Random();

        public StreamReader sr;
        public StreamWriter sw;


        public int score, highscore, pivotX, pivotY, height, width, falldelay, wingDelay, raiseSpeed, fallSpeed;

        public int[,] birdX = new int[5, 5];
        public int[,] birdY = new int[5, 5];
        public char[,] bird = new char[5, 5];
        private char wing;

        public int[,] pipeX = new int[30, 30];
        public int[,] pipeY = new int[30, 30];
        public char[,] pipe = new char[30, 30];
        public int splitStart, splitLength, pipePivotX, pipeWidth, r;

        public int[,] pipeX2 = new int[30, 30];
        public int[,] pipeY2 = new int[30, 30];
        public char[,] pipe2 = new char[30, 30];
        public int splitStart2, splitLength2, pipePivotX2;

        private bool gameOver, restart, isfly, isPrinted;
        private string dirFile = "D:\\FlappyBird";
        private string highscoreFile = "D:\\FlappyBird\\saved_game.txt";

        void ShowMainMenu()
        {
            int choiceID;

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("||========================================================||");
            Console.WriteLine("||--------------------------------------------------------||");

            Console.Write("||---------------------- ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("FLAPPY BIRD");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ---------------------||");

            Console.Write("||-------------------- ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Console version");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" -------------------||");
            Console.WriteLine("||========================================================||");

            pivotX = 30;
            pivotY = 10;
            SetBirdInfo('v', 'o');

            for (int i = 6; i < 14; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    isPrinted = false;
                    for (int m = 0; m < 3; m++)
                        for (int n = 0; n < 5; n++)
                        {
                            if (j == birdX[m, n] && i == birdY[m, n])
                            {
                                if (j == pivotX + 1 && i == pivotY)
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                    Console.Write(bird[m, n]);
                                }
                                else if (j == pivotX - 1 && i == pivotY)
                                {
                                    Console.ForegroundColor = ConsoleColor.Magenta;
                                    Console.Write(bird[m, n]);
                                }
                                else if (j == pivotX + 2 && i == pivotY)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write(bird[m, n]);
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write(bird[m, n]);
                                }
                                isPrinted = true;
                            }
                        }
                    if (!isPrinted)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" ");
                    }
                }
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine("                     PLAY GAME - press 1                  ");
            Console.WriteLine("                     HIGHSCORE - press 2                  ");
            Console.WriteLine("                     HELP      - press 3                  ");
            Console.WriteLine("                     CREDITS   - press 4                  ");
            Console.WriteLine("                     QUIT GAME - press 5                  ");
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("                        Have fun =))");

            while (true)
            {
                keypress = Console.ReadKey(true);
                if (keypress.Key == ConsoleKey.D1)
                {
                    choiceID = 1;
                    break;
                }
                else if (keypress.Key == ConsoleKey.D2)
                {
                    choiceID = 2;
                    break;
                }
                else if (keypress.Key == ConsoleKey.D3)
                {
                    choiceID = 3;
                    break;
                }
                else if (keypress.Key == ConsoleKey.D4)
                {
                    choiceID = 4;
                    break;
                }
                else if (keypress.Key == ConsoleKey.D5)
                {
                    choiceID = 5;
                    break;
                }

            }

            switch (choiceID)
            {
                case 1:
                    LoadScene();
                    break;

                case 2:
                    ViewHighScore();
                    break;

                case 3:
                    ViewHelp();
                    break;

                case 4:
                    ViewCredits();
                    break;

                case 5:
                    AreUsure("quit");
                    break;
            }
        }

        void ViewHighScore()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("||---------------------- FLAPPY BIRD ---------------------||");
            Console.WriteLine("                    ** Console verion **");
            Console.WriteLine("                        <<< HELP >>>");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("                        High score: " + highscore);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("           -- Press ESC to return to Main Menu --");
            while (true)
            {
                keypress = Console.ReadKey(true);
                if (keypress.Key == ConsoleKey.Escape)
                    break;
            }
            ShowMainMenu();
        }

        void ViewCredits()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("||---------------------- FLAPPY BIRD ---------------------||");
            Console.WriteLine("                    ** Console verion **");
            Console.WriteLine("                      <<< CREDITS >>>");
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("         Written in C Sharp language on 10/6/2017");
            Console.WriteLine("                   Author: Phan Phu Hao");
            Console.WriteLine("                    fb.com/ph77894456");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("           -- Press ESC to return to Main Menu --");

            while (true)
            {
                keypress = Console.ReadKey(true);
                if (keypress.Key == ConsoleKey.Escape)
                    break;
            }
            ShowMainMenu();
        }

        void ViewHelp()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("||---------------------- FLAPPY BIRD ---------------------||");
            Console.WriteLine("                    ** Console verion **");
            Console.WriteLine("                        <<< HELPS >>>");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("         Keep the bird flying and avoiding the pipes");
            Console.WriteLine("     Don't let him touch the ground or the top of window");
            Console.WriteLine();
            Console.WriteLine("    Keyboard buttons: - Press Spacebar to raise the bird");
            Console.WriteLine("                      - Press R to restart game");
            Console.WriteLine("                      - Press ESC to pause game");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("           -- Press ESC to return to Main Menu --");
            while (true)
            {
                keypress = Console.ReadKey(true);
                if (keypress.Key == ConsoleKey.Escape)
                    break;
            }
            ShowMainMenu();
        }

        void AreUsure(string Case)
        {
            while (true)
            {
                if (Case == "quit")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.CursorTop = 15;
                    Console.WriteLine();
                    Console.WriteLine(" |--------------------------------------------------------|");
                    Console.WriteLine(" |                DO YOU WANT TO QUIT GAME?               |");
                    Console.WriteLine(" |                      Yes - Press 1                     |");
                    Console.WriteLine(" |                      No  - Press 2                     |");
                    Console.Write(" |--------------------------------------------------------|");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    keypress = Console.ReadKey(true);
                    if (keypress.Key == ConsoleKey.D1)
                        Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Environment.Exit(0);
                    if (keypress.Key == ConsoleKey.D2)
                        ShowMainMenu();
                }
                /*else if (Case == "return")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.CursorTop = 10;
                    Console.WriteLine();
                    Console.WriteLine("        |------------------------------------------|        ");
                    Console.WriteLine("        |          DO YOU WANT TO STOP GAME        |        ");
                    Console.WriteLine("        |        AND RETURN TO THE MAIN MENU?      |        ");
                    Console.WriteLine("        |               Yes - Press 1              |        ");
                    Console.WriteLine("        |               No  - Press 2              |        ");
                    Console.Write    ("        |------------------------------------------|        ");

                    keypress = Console.ReadKey(true);
                    if (keypress.Key == ConsoleKey.D1)
                        ShowMainMenu();
                    if (keypress.Key == ConsoleKey.D2)
                    {
                        break;
                    }
                }*/
            }
        }

        void CountDown()
        {
            // Console.CursorTop = 20;
            Console.ForegroundColor = ConsoleColor.Cyan;
            for (int i = 3; i >= 1; i--)
            {
                Console.SetCursorPosition(width / 2 - 8, height / 2 + 5);
                Console.Write("Ready to play: " + i);
                Thread.Sleep(1000);
            }
            Console.ForegroundColor = ConsoleColor.Green;
        }

        void Pause()
        {
            Console.CursorTop = 10;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("        ============================================        ");
            Console.WriteLine("                         GAME PAUSED                        ");
            Console.WriteLine("              Resume game             - press 1             ");
            Console.WriteLine("              Restart game            - press 2             ");
            Console.WriteLine("              Return to the Main Menu - press 3             ");
            Console.WriteLine("        ============================================        ");
            Console.ForegroundColor = ConsoleColor.Green;

            while (true)
            {
                keypress = Console.ReadKey(true);
                if (keypress.Key == ConsoleKey.D1)
                {
                    Render();
                    goto through1;
                }
                else if (keypress.Key == ConsoleKey.D2)
                {
                    restart = true;
                    goto through2;
                }
                else if (keypress.Key == ConsoleKey.D3)
                    break;
            }
            ShowMainMenu();
        through1:;
            CountDown();
        through2:;
        }

        void Lose()
        {

            Console.CursorTop = 10;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("        ============================================        ");
            Console.WriteLine("                          GAME OVER                         ");
            Console.WriteLine("                        Your score: {0}                     ", score);
            Console.WriteLine("                        High score: {0}                     ", highscore);
            Console.WriteLine("                Return to Main Menu - press 1               ");
            Console.WriteLine("                Restart game        - press 2               ");
            Console.Write("        ============================================        ");
            Console.ForegroundColor = ConsoleColor.Green;
            while (true)
            {
                keypress = Console.ReadKey(true);
                if (keypress.Key == ConsoleKey.D1)
                    goto through1;
                if (keypress.Key == ConsoleKey.D2)
                {
                    goto through2;
                }
            }
        through1:;
            ShowMainMenu();
        through2:;
            restart = true;
        }

        void SetBirdInfo(char wch, char ech)
        {
            bird[0, 0] = ' '; birdX[0, 0] = pivotX - 2; birdY[0, 0] = pivotY - 1;
            bird[0, 1] = '='; birdX[0, 1] = pivotX - 1; birdY[0, 1] = pivotY - 1;
            bird[0, 2] = '='; birdX[0, 2] = pivotX; birdY[0, 2] = pivotY - 1;
            bird[0, 3] = '='; birdX[0, 3] = pivotX + 1; birdY[0, 3] = pivotY - 1;
            bird[0, 4] = ' '; birdX[0, 4] = pivotX + 2; birdY[0, 4] = pivotY - 1;

            bird[1, 0] = '='; birdX[1, 0] = pivotX - 2; birdY[1, 0] = pivotY;
            bird[1, 1] = wch; birdX[1, 1] = pivotX - 1; birdY[1, 1] = pivotY; // wing
            bird[1, 2] = '='; birdX[1, 2] = pivotX; birdY[1, 2] = pivotY; // pivot
            bird[1, 3] = ech; birdX[1, 3] = pivotX + 1; birdY[1, 3] = pivotY; // eye
            bird[1, 4] = '>'; birdX[1, 4] = pivotX + 2; birdY[1, 4] = pivotY; // neb

            bird[2, 0] = ' '; birdX[2, 0] = pivotX - 2; birdY[2, 0] = pivotY + 1;
            bird[2, 1] = '='; birdX[2, 1] = pivotX - 1; birdY[2, 1] = pivotY + 1;
            bird[2, 2] = '='; birdX[2, 2] = pivotX; birdY[2, 2] = pivotY + 1;
            bird[2, 3] = '='; birdX[2, 3] = pivotX + 1; birdY[2, 3] = pivotY + 1;
            bird[2, 4] = ' '; birdX[2, 4] = pivotX + 2; birdY[2, 4] = pivotY + 1;
        }

        void SetPipeInfo()
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < pipeWidth; j++)
                {
                    if (j < r)
                        pipeX[i, j] = pipePivotX - (r - j);
                    else if (j > r)
                        pipeX[i, j] = pipePivotX + (j - r);
                    else if (j == r)
                        pipeX[i, j] = pipePivotX;

                    pipeY[i, j] = i;
                    pipe[i, j] = '=';
                }
            }
            for (int k = splitStart; k < splitLength + splitStart; k++)
            {
                for (int l = 0; l < pipeWidth; l++)
                {
                    pipe[k, l] = ' ';
                }
            }
        }

        void SetPipe2Info()
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < pipeWidth; j++)
                {
                    if (j < r)
                        pipeX2[i, j] = pipePivotX2 - (r - j);
                    else if (j > r)
                        pipeX2[i, j] = pipePivotX2 + (j - r);
                    else if (j == r)
                        pipeX2[i, j] = pipePivotX2;

                    pipeY2[i, j] = i;
                    pipe2[i, j] = '=';
                }
            }
            for (int k = splitStart2; k < splitLength2 + splitStart2; k++)
            {
                for (int l = 0; l < pipeWidth; l++)
                {
                    pipe2[k, l] = ' ';
                }
            }
        }

        void DeadCheck()
        {
            if (pivotY + 1 <= 2 || pivotY + 1 >= height - 1)
            {
                SetBirdInfo(wing, 'x');
                Render();
                gameOver = true;
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (birdY[i, j] <= pipeY[splitStart, 0] - 1 || birdY[i, j] >= pipeY[splitStart + splitLength, 0])
                    {
                        if (birdX[i, j] >= pipePivotX - r && birdX[i, j] <= pipePivotX + r - 1)
                        {
                            SetBirdInfo(wing, 'x');
                            Render();
                            gameOver = true;
                        }
                    }
                    if (birdY[i, j] <= pipeY2[splitStart2, 0] - 1 || birdY[i, j] >= pipeY2[splitStart2 + splitLength2, 0])
                    {
                        if (birdX[i, j] >= pipePivotX2 - r && birdX[i, j] <= pipePivotX2 + r + 1)
                        {
                            SetBirdInfo(wing, 'x');
                            Render();
                            gameOver = true;
                        }
                    }
                }
            }
        }

        void ReadHighScoreFromFile()
        {
            try
            {
                string num;
                sr = new StreamReader(highscoreFile);
                while ((num = sr.ReadLine()) != null)
                {
                    highscore = int.Parse(num);
                }
                sr.Close();
            }
            catch
            {
                Directory.CreateDirectory(dirFile);
                File.Create(highscoreFile);
                highscore = 0;
                //WriteHighScore();
            }
        }

        void Setup()
        {
            height = 30;
            width = 60;

            Console.SetWindowSize(width, height + 5);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.CursorVisible = false;

            score = 0;
            falldelay = 0;
            raiseSpeed = 3;
            fallSpeed = 1;
            wing = 'v';

            gameOver = false;
            restart = false;
            isfly = false;

            pivotX = 20;
            pivotY = height / 2;

            splitStart = rand.Next(5, height - 10);
            splitStart2 = rand.Next(3, height - 13);
            splitLength = splitLength2 = 9;
            pipePivotX = 60;
            pipePivotX2 = pipePivotX + pipeWidth + 21;
            pipeWidth = 17;
            r = pipeWidth / 2;
        }

        void GameCheckInput()
        {
            while (Console.KeyAvailable)
            {
                if (!gameOver)
                    keypress = Console.ReadKey(true);
                if (keypress.Key == ConsoleKey.Spacebar)
                {
                    isfly = true;
                }
                if (keypress.Key == ConsoleKey.Escape)
                    Pause();
            }
        }

        void Logic()
        {
            pipePivotX--;
            pipePivotX2--;
            falldelay++;
            wingDelay++;

            if (wingDelay == 1)
                wing = '^';
            if (falldelay == 1)
            {
                pivotY += fallSpeed;
                falldelay = 0;
            }
            if (isfly)
            {
                //Console.Beep();
                pivotY -= raiseSpeed;
                wing = 'v';
                falldelay = -1;
                wingDelay = -1;
                isfly = false;
            }

            if (pipePivotX == pivotX - r || pipePivotX2 == pivotX - r)
            {
                score++;
                if (score > highscore)
                {
                    highscore = score;
                }
            }

            if (pipePivotX == -r)
            {
                pipePivotX = width + r;
                splitStart = rand.Next(3, height - splitLength - 3);
            }

            if (pipePivotX2 == -r)
            {
                pipePivotX2 = width + r;
                splitStart2 = rand.Next(3, height - 13);
            }

            SetPipeInfo();
            SetPipe2Info();

            SetBirdInfo(wing, 'o');
        }

        void Render()
        {
            if (!gameOver)
            {
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.Green;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        isPrinted = false;
                        for (int m = 0; m < 3; m++)
                        {
                            for (int n = 0; n < 5; n++)
                            {
                                if (j == birdX[m, n] && i == birdY[m, n])
                                {
                                    if (j == pivotX + 1 && i == pivotY)
                                    {
                                        if (bird[m, n] == 'o')
                                            Console.ForegroundColor = ConsoleColor.White;
                                        else
                                            Console.ForegroundColor = ConsoleColor.Red;
                                    }
                                    else if (j == pivotX - 1 && i == pivotY)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Magenta;
                                    }
                                    else if (j == pivotX + 2 && i == pivotY)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                    }
                                    Console.Write(bird[m, n]);
                                    isPrinted = true;
                                }
                            }
                        }
                        if (!isPrinted)
                        {
                            for (int a = 0; a < height; a++)
                            {
                                for (int b = 0; b < pipeWidth; b++)
                                {
                                    if (j == pipeX[a, b] && i == pipeY[a, b])
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.Write(pipe[a, b]);
                                        isPrinted = true;
                                    }
                                }
                            }
                        }
                        if (!isPrinted)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < pipeWidth; x++)
                                {
                                    if (j == pipeX2[y, x] && i == pipeY2[y, x])
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.Write(pipe2[y, x]);
                                        isPrinted = true;
                                    }
                                }
                            }
                        }
                        if (!isPrinted)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write(' ');
                        }
                    }
                    Console.WriteLine();
                }

                Console.SetCursorPosition(0, height);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("-----------------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Your score: " + score);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("FLAPPY BIRD console version");
                Console.WriteLine("Created by Phan Phu Hao");
            }
        }

        void WriteHighScore()
        {
            sw = new StreamWriter(highscoreFile);
            sw.WriteLine(highscore);
            sw.Close();
        }

        void Update()
        {
            Console.Clear();
            while (true)
            {
                GameCheckInput();
                Logic();
                Render();
                DeadCheck();
                if (gameOver || restart)
                    break;
                //Thread.Sleep(10);
            }
            if (gameOver)
            {
                try
                {
                    WriteHighScore();
                }
                catch
                { }
                Thread.Sleep(500);
                Lose();
            }
        }

        void LoadScene()
        {
            CountDown();
            Setup();
            Update();
        }

        public void Execute(string arg) 
        {
            FlappyBirds Fb = new FlappyBirds();
            Fb.ReadHighScoreFromFile();
            Fb.Setup();
            Fb.ShowMainMenu();
            while (true)
            {
                Fb.LoadScene();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SnakeLite
{
    class Program
    {
        //This is all values and external code used for disabling the maximize button
        const int MF_BYCOMMAND = 0x00000000;
        const int SC_MAXIMIZE = 0xF030;

        [DllImport("user32.dll")]
        static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);
        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        static extern IntPtr GetConsoleWindow();

        //These are important game values like whether the game is idle or active, the time of the last game tick, and the random number generator
        static int gameState = 0;
        static DateTime lastTick;
        static Random rng = new Random();

        //This is just for comparing with the current window size to determine if the grid needs to be adjusted or the window size reverted
        static (int width, int height) screenSize = (0, 0);

        //This is the grid which hold the color value of each "pixel" as an int which can be used to reference a color from the list
        static int[][] grid;
        static ConsoleColor[] colors = new ConsoleColor[3] { ConsoleColor.Black, ConsoleColor.Gray, ConsoleColor.Red };

        //These values all have to do with the snake's position and direction with the list holding the coordinates to each piece of the snake and the direction refering to the list of vectors
        static List<(int row, int col)> snake = new List<(int row, int col)>();
        static (int row, int col)[] directionVectors = new (int row, int col)[4] { (-1, 0), (0, 1), (1, 0), (0, -1) };
        static int direction;

        //This is simply the position of the food
        static (int row, int col) foodPos = (-1, -1);

        static void Main()
        {
            //This is the external code for disabling the maximize button in action
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_MAXIMIZE, MF_BYCOMMAND);

            //Sets the title of the console window
            Console.Title = "SnakeLite";

            //This is the main game loop, the only way out of this is to exit the game either with the escape key or closing it out
            while (gameState != -1)
            {
                //This loop is the screen update loop
                //The property "KeyAvailable" means that a key is currently being input to the console so by performing this loop while KeyAvailable is false the loop will continue until a key is pressed and, due to its placement inside the main game loop, after the key is released this loop will start right back up again
                while (Console.KeyAvailable == false)
                {
                    //This is where the comparison between the stored screen size and actual screen size takes place
                    if ((Console.WindowWidth, Console.WindowHeight) != screenSize)
                    {
                        //Regardless of what happens to the screen size from here the cursor needs to be hiden again
                        Console.CursorVisible = false;

                        //This try-catch is set up because despite disabling the maximize button you can still end up maximizing it because you can snap the window to the top of the screen and there is no way to disable moving the window
                        try
                        {
                            //If the game is not in progress it will update the grid to the new size of the console, otherwise it will attempt to force the window back to the size it was when the game started
                            if (gameState == 0) { screenSize = (Console.WindowWidth, Console.WindowHeight); Console.SetBufferSize(screenSize.width, screenSize.height); }
                            else { Console.BackgroundColor = colors[0]; Console.SetWindowSize(screenSize.width, screenSize.height); Console.SetBufferSize(screenSize.width, screenSize.height); }
                        }
                        catch
                        {
                            //If the window was snapped to the edge of the screen in any way and then shrunk or just shrunk too small while in a game the progress is lost and unfortunately there is no way around this that I have found so all I can do is reset the game and set the window to the default size
                            ResetGame();
                            Console.SetWindowSize(80, 20);
                        }

                        ResizeGrid();
                    }

                    //This is where everything that happens once per tick takes place
                    if (gameState == 1 && DateTime.Now >= lastTick.AddSeconds(0.15))
                    {
                        //Never forget to update the time of the last tick otherwise there is no way to track how much time has passed
                        lastTick = DateTime.Now;

                        //This is what the location of the head will be after this tick and is used to check whether it will die, eat food, or just take a step
                        (int row, int col) newHeadPos = (snake[0].row + directionVectors[direction].row, snake[0].col + directionVectors[direction].col);

                        //In order this is checking if the snake will die either from hitting a wall or itself, eat food, or take a normal step and it is carrying out the resulting actions whether its reseting the game, growing the snake and replacing the food, or just moving the snake along
                        if ((newHeadPos.row < 0 || newHeadPos.row >= grid.Length || newHeadPos.col < 0 || newHeadPos.col >= grid[0].Length) || grid[newHeadPos.row][newHeadPos.col] == 1) { ResetGame(); }
                        else if (grid[newHeadPos.row][newHeadPos.col] == 2) { MoveSnake(newHeadPos, true); PlaceFood(); }
                        else { MoveSnake(newHeadPos, false); }
                    }
                }

                //This is where the input is accepted and stored to be processed, the "true" overload given to the ReadKey method ensures the key you pressed isn't displayed to the console
                //Anything beyond here will only happen when a key is pressed
                ConsoleKey input = Console.ReadKey(true).Key;

                //This checks if the game is idle and if it is it sets it to active and resets all the important game values
                if (gameState == 0)
                {
                    gameState = 1;
                    lastTick = DateTime.Now;
                    direction = -1;
                    MoveSnake((grid.Length / 2, grid[0].Length / 2), false);
                    PlaceFood();
                }

                //This is where the input is processed, only allowing you to turn 90 degrees at a time and if you start the game with something other than an arrow key you start with a random direction
                if ((input == ConsoleKey.UpArrow || input == ConsoleKey.W) && (direction == -1 || snake[0].row - 1 != snake[1].row)) { direction = 0; }
                else if ((input == ConsoleKey.RightArrow || input == ConsoleKey.D) && (direction == -1 || snake[0].col + 1 != snake[1].col)) { direction = 1; }
                else if ((input == ConsoleKey.DownArrow || input == ConsoleKey.S) && (direction == -1 || snake[0].row + 1 != snake[1].row)) { direction = 2; }
                else if ((input == ConsoleKey.LeftArrow || input == ConsoleKey.A) && (direction == -1 || snake[0].col - 1 != snake[1].col)) { direction = 3; }
                else if (input == ConsoleKey.Escape) { gameState = -1; }
                else if (direction == -1) { direction = rng.Next(0, 4); }
            }
        }

        //This resizes the grid based on the stored screen size and if a game was in progress it restores all of the elements to the screen
        static void ResizeGrid()
        {
            Console.CursorVisible = false;
            grid = new int[screenSize.height][];
            for(int r = 0; r < grid.Length; r++) { grid[r] = new int[screenSize.width / 2]; }
            if (snake.Count() > 0) { foreach ((int row, int col) piece in snake) { ScreenUpdate(piece.row, piece.col, 1); } }
            if (foodPos != (-1, -1)) { ScreenUpdate(foodPos.row, foodPos.col, 2); }
        }

        //This is the only place the console is writen to and is only called when a change happens, the method updates the grid value and the screen with the correct color
        static void ScreenUpdate(int row, int col, int newValue)
        {
            //This check ensures that you can't crash the console by try to draw to a part of the screen that isn't there anymore because the window is part way through being resized
            if (screenSize == (Console.WindowWidth, Console.WindowHeight))
            {
                grid[row][col] = newValue;
                Console.BackgroundColor = colors[newValue];
                Console.SetCursorPosition(col * 2, row);
                Console.Write("  ");
            }
        }

        //This is what contols the movement of the snake, it's given the new head position and a bool for whether the snake ate food this turn
        static void MoveSnake((int row, int col) newPos, bool ate)
        {
            //If the snake is empty that means it's a new game so the starting snake parts are added to the list and since they all have the same coordinates only one needs to be drawn
            if (snake.Count() == 0)
            {
                for (int s = 0; s < 4; s++) { snake.Add(newPos); }
                ScreenUpdate(newPos.row, newPos.col, 1);
            }
            //If the snake isn't empty then the list of snake pieces is remade with the new head position added to the front of the list and the tail removed from the end unless food was eaten
            else
            {
                //Temporarily storing the old snake in a new list
                List<(int row, int col)> oldSnake = new List<(int row, int col)>(0);
                oldSnake.AddRange(snake);

                //Clearing the snake, adding the new head location to the list, and updating the screen for that position
                snake.Clear();
                snake.Add(newPos);
                ScreenUpdate(newPos.row, newPos.col, 1);

                //Checking if food was consumed on this tick
                if (!ate)
                {
                    //If no food was eaten then tail is removed from the end of the snake list but the screen is only updated if all of the overlapping snake pieces from the start of the game are moved
                    if (!oldSnake.Exists(x => x == oldSnake.Last() && oldSnake.IndexOf(x) != oldSnake.Count() - 1)) { ScreenUpdate(oldSnake.Last().row, oldSnake.Last().col, 0); }
                    oldSnake.RemoveAt(oldSnake.Count() - 1);
                }

                //Readding the rest of the snake either with or without the end of the tail
                snake.AddRange(oldSnake);
            }
        }

        //This places the food on the grid assuming it doesn't fall on any part of the snake
        static void PlaceFood()
        {
            //Inital placement
            foodPos = (rng.Next(0, grid.Length), rng.Next(0, grid[0].Length));

            //If the inial placement is shared with a snake piece this loop repeats the placement process until that isn't true
            while (grid[foodPos.row][foodPos.col] == 1) { foodPos = (rng.Next(0, grid.Length), rng.Next(0, grid[0].Length)); }
            ScreenUpdate(foodPos.row, foodPos.col, 2);
        }

        //This method resets the game to idle and clears the console window
        static void ResetGame()
        {
            gameState = 0;
            snake.Clear();
            foodPos = (-1, -1);
            Console.BackgroundColor = colors[0];
            Console.Clear();
            screenSize = (0, 0);
        }
    }
}

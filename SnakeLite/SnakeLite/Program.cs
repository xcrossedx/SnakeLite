using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakeLite
{
    //I removed the overly complex shit that I didn't understand and also stretched everything out rather than trying to make it as compact as possible so now it should be easier to read
    class Program
    {
        //The game state determines if the game is idle (0) or active (1). It is also used to give the exit code (-1).
        static int gameState = 0;

        //The last tick is a DateTime object which is built into .net that holds a date and time and has useful functions like getting the current time and adding or subtracting time in seconds or minutes or whatever.
        static DateTime lastTick;

        //This is just instantiating the Random object, another thing built into .net, that allows random number generation within a specified range with or without a seed.
        static Random rng = new Random();

        //This is a tuple which is a variable with sub-variables, in this case two ints, which can be referred to by either screenSize.width or screenSize.height. This screenSize variable is holding the last window size when the game was last idle and is used to compare to the current screen size to revert the screen back to this size if the game is active.
        static (int width, int height) screenSize = (0, 0);

        //The grid is where numerical values, each representing a color, are stored in a jagged 2D array.
        static int[][] grid;

        //This is the list of colors that each pixel of the grid could be. By default ConsoleColors have numbers that can be referred to using an enumerator but if you don't need them all it can be easier to just make your own array of colors.
        static ConsoleColor[] colors = new ConsoleColor[3] { ConsoleColor.Black, ConsoleColor.Gray, ConsoleColor.Red };

        //This list stores the coordinates of every piece of the snakes body. Lists are different from arrays because you can add and remove elements and the size of the list changes dynamically.
        static List<(int row, int col)> snake = new List<(int row, int col)>();

        //This is an array of tuples used to list all possible directions that can be moved as vectors. This allows for the direction variable to be a single value despite representing movment in 2D space.
        static (int row, int col)[] directionVectors = new (int row, int col)[4] { (-1, 0), (0, 1), (1, 0), (0, -1) };

        //This is the direction variable which will really only refer to the index of the direction vector from the array above.
        static int direction;

        //This is just the coordinates of the food, again as a tuple.
        static (int row, int col) foodPos;

        //Typically I would try and keep as little code in the Main method as possible but considering the simplicity of the program I didn't think it would be necessary. Normally I would only call an Initialization function which would do all the set up and then a Play function which would hold the main game loop.
        static void Main()
        {
            //All this does is set the name of the application at the top of the window.
            Console.Title = "SnakeLite";

            //This is the main game loop. It only exits when the exit code is given, or the window is closed but thats a given.
            while (gameState != -1)
            {
                //This is the update loop, it handles all the screen updates as well snake movement and food placement. The Console.KeyAvailable property is true when a input is being accepted by the console so by running the loop while it is false the loop continues until a key is pressed.
                while (Console.KeyAvailable == false)
                {
                    //This is where the screen size comparison takes place.
                    if ((Console.WindowWidth, Console.WindowHeight) != screenSize)
                    {
                        //No matter what, when the screen is resized by anything other than code, the cursor is unhidden and therefore must be hidden again.
                        Console.CursorVisible = false;

                        //Due to the windows allowing you to snap windows to sides of the screen and stuff like that and some other weird quirks of resizing a console window all of this code is in a try-catch set up mean if the try code fails, the catch code will be executed.
                        try
                        {
                            //If the game is idle changing the size of the screen will update the grid size to make the game board equal to the new size of the window
                            if (gameState == 0)
                            {
                                //If you don't update the stored screen size the comparison will never match.
                                screenSize = (Console.WindowWidth, Console.WindowHeight);

                                //The buffer is the amount of space beyond the edge of the console which prompts the scroll bar, so by setting the buffer to the size of the screen the scroll bars are removed.
                                Console.SetBufferSize(screenSize.width, screenSize.height);
                            }
                            //If the game is active it will attempt to revert the window size to the stored value.
                            else
                            {
                                //When setting the window size with code it scans over the whole screen and applies whatever the current background color is so to keep it from being either snake or food colored it's reset to black.
                                Console.BackgroundColor = colors[0];

                                //This is setting both the window and buffer size to the stored value, when doing both always do the window size first because if the stored value is smaller than the current value and you try to set the buffer first you will get an out of range exception.
                                Console.SetWindowSize(screenSize.width, screenSize.height);
                                Console.SetBufferSize(screenSize.width, screenSize.height);
                            }
                        }
                        //This code only executes if the try code throws an error.
                        catch
                        {
                            //The window size is reset to the default values to prevent back to back exceptions
                            Console.SetWindowSize(80, 20);

                            //Because the screen could now easily be smaller than it was when the game was active there is no telling if the snake or food will be in bounds given the new grid size so the game needs to be reset.
                            gameState = 0;
                        }

                        //In any event if the screen size changes from the stored value the grid needs to be remade. even if the screen size ends up the same the resize method also contains code to redraw the game elements if they haven't been reset.
                        ResizeGrid();
                    }

                    //This is the real update work happens but only if the game is active and if a full tick as has passed, in this case 0.15 seconds.
                    if (gameState == 1 && DateTime.Now >= lastTick.AddSeconds(0.15))
                    {
                        //Just like the screen size when you are doing a comparison like this you need to update the stored value for the next time you want to compare it.
                        lastTick = DateTime.Now;

                        //This is a temporary tuple for the position the snake's head will be if it moves a space in its current direction.
                        (int row, int col) newHeadPos = (snake[0].row + directionVectors[direction].row, snake[0].col + directionVectors[direction].col);

                        //This checks if the snake will die by taking a step either by the wall or itself.
                        if ((newHeadPos.row < 0 || newHeadPos.row >= grid.Length || newHeadPos.col < 0 || newHeadPos.col >= grid[0].Length) || grid[newHeadPos.row][newHeadPos.col] == 1)
                        {
                            //If the snake dies the game is returned to idle to play again.
                            gameState = 0;
                        }
                        //This checks if the snake will eat food by taking a step.
                        else if (grid[newHeadPos.row][newHeadPos.col] == 2)
                        {
                            //This method moves the snake forward by giving it the new head position and a boolean telling it whether or not food was eaten in the same movement.
                            MoveSnake(newHeadPos, true);

                            //This is pretty self explanitory.
                            PlaceFood();
                        }
                        //This takes place if nothing interesting happens.
                        else
                        {
                            //Again this moves the snake but this time no food was eaten so it is passed a false boolean.
                            MoveSnake(newHeadPos, false);
                        }
                    }
                }

                //This is where input in accepted and stored. By pressing a key not only are you storing it here to be processed but you're breaking out of the while(Console.KeyAvailable == false) loop until after the input in processed and the main game loop returns to the top.
                ConsoleKey input = Console.ReadKey(true).Key;

                //This initializes the game if it is idle and a key is pressed.
                if (gameState == 0)
                {
                    //The color needs to be set to black before clearing the console or else, like resizing, the whole console will be colored whatever this is set to.
                    Console.BackgroundColor = colors[0];

                    //This wipes the console clean.
                    Console.Clear();

                    //Sets the last tick, only really necessary the first time but I like to keep it for added security.
                    lastTick = DateTime.Now;

                    //Creates a grid if one does not exists otherwise resizes it.
                    ResizeGrid();

                    //Clearing the list of snake pieces from the previous game.
                    snake.Clear();

                    //This method has another trick up its sleeve because if the list o snake pieces is empty it will initialize it automatically.
                    MoveSnake((grid.Length / 2, grid[0].Length / 2), false);

                    //Setting the direction to -1 is how I get around rotation protection when starting a new game, because you can only rotate 90 degrees at a time if the direction is left as whatever the last game ended with you could only start the new game in one of 2 directions.
                    direction = -1;

                    //Again this can probably speak for itself for now.
                    PlaceFood();

                    //Sets the state to active.
                    gameState = 1;
                }

                //This block of if and else if statements are all checking for direction inputs and setting the direction accordingly. It also checks if either the new game value is in place for the direction or if its not a 180 degree turn.
                if ((input == ConsoleKey.UpArrow || input == ConsoleKey.W) && (direction == -1 || snake[0].row - 1 != snake[1].row))
                {
                    direction = 0;
                }
                else if ((input == ConsoleKey.RightArrow || input == ConsoleKey.D) && (direction == -1 || snake[0].col + 1 != snake[1].col))
                {
                    direction = 1;
                }
                else if ((input == ConsoleKey.DownArrow || input == ConsoleKey.S) && (direction == -1 || snake[0].row + 1 != snake[1].row))
                {
                    direction = 2;
                }
                else if ((input == ConsoleKey.LeftArrow || input == ConsoleKey.A) && (direction == -1 || snake[0].col - 1 != snake[1].col))
                {
                    direction = 3;
                }
                //This is checking for the escape key to give the exit code.
                else if (input == ConsoleKey.Escape)
                {
                    //Setting the exit code, because this is at the end of the game loop this will be the last line to execute before the loop attempts to continue, making it the fastest possible way to exit the loop other than closing the window.
                    gameState = -1;
                }
                //This code only executes if the game just started and you press something that isn't a directional input.
                else if (direction == -1)
                {
                    //Because no direction was chosen a random one is applied before the game starts.
                    direction = rng.Next(0, 4);
                }
            }
        }

        //This is the method that resizes the grid and, if possible, redraws the game elements.
        static void ResizeGrid()
        {
            //Because the grid is a jagged 2D array you must first initialize the size of the first dimension.
            grid = new int[screenSize.height][];

            //This for loop iterates through each row of the newly created grid.
            for(int r = 0; r < grid.Length; r++)
            {
                //Since this is a 2D grid every row needs a colomn initialized with a size as well.
                grid[r] = new int[screenSize.width / 2];
            }

            //This is where the game elements are restored if the game hasn't been reset that is.
            if (gameState == 1)
            {
                //This iterates through each piece of the snake.
                foreach ((int row, int col) piece in snake)
                {
                    //For each piece it updates the screen with the correct color.
                    ScreenUpdate(piece.row, piece.col, 1);
                }

                //This checks if the food position was saved.
                if (foodPos.row < grid.Length && foodPos.col < grid[0].Length)
                {
                    //If it was then the screen can be updated with its position and color.
                    ScreenUpdate(foodPos.row, foodPos.col, 2);
                }
                //This is if the food's position wasn't saved.
                else
                {
                    //The food is placed fresh as if it was eaten.
                    PlaceFood();
                }
            }
        }

        //This method is responsible for updating both the grid and the screen with the appropreiate values.
        static void ScreenUpdate(int row, int col, int newValue)
        {
            //This check ensures that nothing will attempt to draw while the screen is being resized to avoid something being drawn out of bounds.
            if (screenSize == (Console.WindowWidth, Console.WindowHeight))
            {
                //This is where the grid is updated.
                grid[row][col] = newValue;

                //This sets the color that the updated pixel will be drawn in.
                Console.BackgroundColor = colors[newValue];

                //This sets the position of the cursor in the console window, the col is multiplied by 2 because a square "pixel" on a console application is 2 spaces next to each other making each pixel colomn equal to 2 characters of width which is how console size is determined.
                Console.SetCursorPosition(col * 2, row);

                //This simply writes the two spaces which make up the pixel. having the spaces is what allows for using background color to color the whole pixel.
                Console.Write("  ");
            }
        }

        //This is the method that controls the movement of the snake and more specifically the placement and removal of snake pieces.
        static void MoveSnake((int row, int col) newPos, bool ate)
        {
            //This is checking if the snake is empty, this would only be the case when this method is called after a key is pressed and the game is idle.
            if (snake.Count() == 0)
            {
                //This is just a basic iterator for the 4 pieces of the starting snake.
                for (int s = 0; s < 4; s++)
                {
                    //When the snake is first created all the pieces have the same position, this simplifies things for how the snake "moves" also is safe because lose detection only checks the space in front of where the head currently is.
                    snake.Add(newPos);
                }

                //Since all the snake pieces are in the same position right now the grid and screen only need to be updated once.
                ScreenUpdate(newPos.row, newPos.col, 1);
            }
            //If the snake is not empty then it needs to move and rather than inching each piece along one at a time the same affect can be achieved by adding the new head to the front of the list and removing the piece from the end of the list, unless food was consumed.
            else
            {
                //This temporary list will hold the current list of snake pieces. These will be appended to the new list containing the new head position either with or without the last piece depending on if food was eaten.
                List<(int row, int col)> oldSnake = new List<(int row, int col)>(0);

                //The AddRange method with clone the list over whereas setting them equal to one another creates two references to the same list, in other words, when you make changes to the original list you are changing the new list as well.
                oldSnake.AddRange(snake);

                //This is pretty clear what it does, this is a function built into the List<> data type.
                snake.Clear();

                //This adds the new head position to the newly cleared list.
                snake.Add(newPos);

                //Updating the grid and screen.
                ScreenUpdate(newPos.row, newPos.col, 1);

                //Here is where, if the snake did not eat this space, the tail would be removed from the end of the list.
                if (!ate)
                {
                    //This checks to see if there are any overlapping pieces from when the snake was first created. If there is still overlap the position of the last piece shouldn't be cleared from the grid or the screen because it is shared with other pieces.
                    //This .Exists() function returns a boolean and uses whats known as a lambda expression. It reads like this, "does x exist wherein x equals the tail value and the index of x does not equal the index of the tail" (Count() - 1 is used because Count() numbers the elements starting at 1 while the index starts at 0.). These come up quite a lot and are really powerful query expressions so they might be worth looking into on their own.
                    if (!oldSnake.Exists(x => x == oldSnake.Last() && oldSnake.IndexOf(x) != oldSnake.Count() - 1))
                    {
                        //This updates the screen with a color of black where the last piece of tail is.
                        ScreenUpdate(oldSnake.Last().row, oldSnake.Last().col, 0);
                    }

                    //Regardless of if the screen was updated the tails entry in the list must be removed, thats why its easy to have them all in the same spot to start with.
                    oldSnake.RemoveAt(oldSnake.Count() - 1);
                }

                //This is where the rest of the snake, with or without the tail, is appended to the list containing the new head position.
                snake.AddRange(oldSnake);
            }
        }

        //This is the method for placing food randomly on the grid while avoiding the snake.
        static void PlaceFood()
        {
            //The food is given a random position initially, as long as it didn't land on the snake first try this will be the final position.
            foodPos = (rng.Next(0, grid.Length), rng.Next(0, grid[0].Length));

            //If the food did land on a snake piece right away this is loop will run until a suitable location has been found.
            while (grid[foodPos.row][foodPos.col] == 1)
            {
                //Again this just randomly sets the position somewhere in the grid.
                foodPos = (rng.Next(0, grid.Length), rng.Next(0, grid[0].Length));
            }

            //Once it passes the test of not being inside the snake, the food can be placed on the grid and screen with its personal color
            ScreenUpdate(foodPos.row, foodPos.col, 2);
        }
    }
}

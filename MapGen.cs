using System;
using System.Collections.Generic;
using System.Threading;

namespace MapGen
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            // Turn off the cursor
            Console.CursorVisible = false;
            // Run the game
            Game game = new();
            game.Start();
        }
    }

    /// <summary>
    /// Segment Type    
    /// </summary>
    public enum SegmentType
    {
        Ground,
        Cavity,
        SlopeUp,
        SlopeDown,
        Spike
    }

    /// <summary>
    /// Game
    /// </summary>
    public class Game
    {
        private readonly LevelGenerator levelGenerator = new();
        private bool isPaused = false;

        public void Start()
        {
            levelGenerator.GenerateInitialGround();

            while (true)
            {
                // Check for key press
                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;

                    if (key == ConsoleKey.Spacebar) // Pause the game
                    {
                        isPaused = !isPaused;
                        if (isPaused)
                        {
                            Console.SetCursorPosition(0, 0);
                            Console.WriteLine("Game Paused. Press 'SPACE' to resume...");
                        }
                    }

                    if (key == ConsoleKey.Escape) // Exit the program
                    {
                        Console.SetCursorPosition(0, levelGenerator.ScreenBottom + 2);
                        Console.WriteLine("Exiting game... Goodbye!");
                        break;
                    }
                }

                if (!isPaused)
                {
                    levelGenerator.DrawMap();
                    levelGenerator.GenerateNextSegment();
                    Thread.Sleep(100); // Adjust game speed
                }
            }
        }
    }

    /// <summary>
    /// Level Generator
    /// </summary>
    public class LevelGenerator
    {
        public int ScreenWidth { get; set; } = 40;
        public int ScreenTop { get; set; } = 1;
        public int ScreenBottom { get; set; } = 5;
        public int GroundStart { get; set; } = 5;

        private readonly List<SegmentType> levelSegments = new();
        private readonly List<int> segmentHeights = new();
        
        private readonly Random random = new();

        private SegmentType lastSegment = SegmentType.Ground;
        private int lastHeight = 0;
        private int slopeLength = 0;

        // Weights for each segment type
        // Ground, Cavity, SlopeUp, SlopeDown, Spike
        private readonly double[] weights = [0.35, 0.10, 0.20, 0.20, 0.15];

        /// <summary>
        /// 
        /// </summary>
        public void GenerateInitialGround()
        {
            for (int i = 0; i < ScreenWidth; i++)
            {
                levelSegments.Add(SegmentType.Ground);
                segmentHeights.Add(GroundStart);
            }
            lastSegment = SegmentType.Ground;
            lastHeight = GroundStart;
        }

        // Generate Next Segment
        public void GenerateNextSegment()
        {
            // Set defaults
            SegmentType segment = SegmentType.Ground;
            int height = lastHeight;

            if (slopeLength > 0)
            {
                segment = lastSegment;
                slopeLength--;
                height = (segment == SegmentType.SlopeUp) ? height - 1 : height + 1;
            }
            else if (lastSegment == SegmentType.Ground)
            {
                SegmentType segmentType = GetWeightedRandomSegment(random);

                switch (segmentType)
                {
                    case SegmentType.Cavity:
                        segment = SegmentType.Cavity;
                        break;

                    case SegmentType.SlopeUp when height > ScreenTop:
                        segment = SegmentType.SlopeUp;
                        height = Math.Max(ScreenTop, lastHeight - 1);
                        slopeLength = height > ScreenTop ? random.Next(1, 3) : 0;
                        break;

                    case SegmentType.SlopeDown when height < ScreenBottom:
                        segment = SegmentType.SlopeDown;
                        height = Math.Min(lastHeight + 1, ScreenBottom);
                        slopeLength = height < ScreenBottom ? random.Next(1, 3) : 0;
                        break;

                    case SegmentType.Spike:
                        segment = SegmentType.Spike;
                        break;
                }
            }

            // Shift all values left by one
            for (int i = 0; i < ScreenWidth - 1; i++)
            {
                levelSegments[i] = levelSegments[i + 1];
                segmentHeights[i] = segmentHeights[i + 1];
            }

            lastSegment = levelSegments[ScreenWidth - 1] = segment;
            lastHeight = segmentHeights[ScreenWidth - 1] = height;
        }

        /// <summary>
        /// Get Weighted Random Segment
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public SegmentType GetWeightedRandomSegment(Random random)
        {
            // Calculate the cumulative weights
            var cumulativeWeights = new double[weights.Length];
            cumulativeWeights[0] = weights[0];
            for (int i = 1; i < weights.Length; i++)
            {
                cumulativeWeights[i] = cumulativeWeights[i - 1] + weights[i];
            }

            // Generate a random number between 0 and 1
            double randomValue = random.NextDouble();

            // Determine which value to return based on the random number
            for (int i = 0; i < cumulativeWeights.Length; i++)
            {
                if (randomValue < cumulativeWeights[i])
                {
                    return (SegmentType)i;
                }
            }

            // Default
            return SegmentType.Ground;
        }

        /// <summary>
        /// Draw Map with Colours
        /// Green for ground and slopes, Red and Dark Gray for cavity and Yellow for spikes
        /// </summary>
        public void DrawMap()
        {
            Console.Clear();

            for (int i = 0; i < levelSegments.Count; i++)
            {
                int segmentHeight = segmentHeights[i];
                Console.SetCursorPosition(i, segmentHeight + 1);

                // Set color based on segment type
                switch (levelSegments[i])
                {
                    case SegmentType.Ground:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("_");
                        break;

                    case SegmentType.Cavity:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.SetCursorPosition(i, segmentHeight + 2);
                        Console.Write("_");
                        Console.ResetColor();
                        break;

                    case SegmentType.SlopeUp:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.SetCursorPosition(i, segmentHeight + 2);
                        Console.Write("/");
                        break;

                    case SegmentType.SlopeDown:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("\\");
                        break;

                    case SegmentType.Spike:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(".");
                        break;

                }
                Console.ResetColor(); // Reset to default after each segment
            }

            Console.SetCursorPosition(0, segmentHeights[^1]);
        }
    }
}

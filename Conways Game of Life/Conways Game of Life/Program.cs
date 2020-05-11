using System;
using System.Threading.Tasks;

namespace Life
{
    public class LifeSimulation
    {
        // These are the main variables for the program
        private bool[,] thisWorld;
        private bool[,] nextGen;
        private Task processTasks;

        // Checks if the system is the right size
        public LifeSimulation(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException("Size must be greater than zero");
            this.Size = size;
            thisWorld = new bool[size, size];
            nextGen = new bool[size, size];
        }

        public int Size { get; private set; }
        public int Generation { get; private set; }

        public Action<bool[,]> nextGenCompleted;

        public bool this[int x, int y]
        {
            get { return this.thisWorld[x, y]; }
            set { this.thisWorld[x, y] = value; }
        }

        public bool ToggleCell(int x, int y)
        {
            bool currentValue = this.thisWorld[x, y];
            return this.thisWorld[x, y] = !currentValue;
        }

        public void Update()
        {
            if (this.processTasks != null && this.processTasks.IsCompleted)
            {
                // When a generation has completed then it will start 
                // Now flip the back buffer so we can start processing on the next generation
                var flip = this.nextGen;
                this.nextGen = this.thisWorld;
                this.thisWorld = flip;
                Generation++;

                // begin the next generation's processing asynchronously
                this.processTasks = this.ProcessGeneration();

                if (nextGenCompleted != null) nextGenCompleted(this.thisWorld);
            }
        }

        public void BeginGeneration()
        {
            if (this.processTasks == null || (this.processTasks != null && this.processTasks.IsCompleted))
            {
                // only begin the generation if the previous process was completed
                this.processTasks = this.ProcessGeneration();
            }
        }

        public void Wait()
        {
            if (this.processTasks != null)
            {
                this.processTasks.Wait();
            }
        }

        private Task ProcessGeneration()
        {
            return Task.Factory.StartNew(() =>
            {
                Parallel.For(0, Size, x =>
                {
                    Parallel.For(0, Size, y =>
                    {
                        int numberOfNeighbors = IsNeighborAlive(thisWorld, Size, x, y, -1, 0)

                            + IsNeighborAlive(thisWorld, Size, x, y, -1, 1) + IsNeighborAlive(thisWorld, Size, x, y, 0, 1)

                            + IsNeighborAlive(thisWorld, Size, x, y, 1, 1) + IsNeighborAlive(thisWorld, Size, x, y, 1, 0)

                            + IsNeighborAlive(thisWorld, Size, x, y, 1, -1) + IsNeighborAlive(thisWorld, Size, x, y, 0, -1)

                            + IsNeighborAlive(thisWorld, Size, x, y, -1, -1);

                        bool shouldLive = false;
                        bool isAlive = thisWorld[x, y];

                        if (isAlive && (numberOfNeighbors == 2 || numberOfNeighbors == 3))
                        {
                            shouldLive = true;
                        }
                        else if (!isAlive && numberOfNeighbors == 3) // zombification
                        {
                            shouldLive = true;
                        }

                        nextGen[x, y] = shouldLive;

                    });
                });
            });
        }

        private static int IsNeighborAlive(bool[,] thisWorld, int size, int x, int y, int offsetx, int offsety)
        {
            int result = 0;

            int proposedOffsetX = x + offsetx;
            int proposedOffsetY = y + offsety;
            bool outOfBounds = proposedOffsetX < 0 || proposedOffsetX >= size | proposedOffsetY < 0 || proposedOffsetY >= size;
            if (!outOfBounds)
            {
                result = thisWorld[x + offsetx, y + offsety] ? 1 : 0;
            }
            return result;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            LifeSimulation sim = new LifeSimulation(10);

            // Initialize with a blinker 
            sim.ToggleCell(5, 5);
            sim.ToggleCell(5, 6);
            sim.ToggleCell(5, 7);

            // Starts a generation
            sim.BeginGeneration();
            sim.Wait();
            OutputBoard(sim);

            // Updates and waits 
            sim.Update();
            sim.Wait();
            OutputBoard(sim);

            // Updates and waits 
            sim.Update();
            sim.Wait();
            OutputBoard(sim);

            Console.ReadKey();
        }

        private static void OutputBoard(LifeSimulation sim)
        {
            var line = new String('-', sim.Size);
            Console.WriteLine(line);

            for (int y = 0; y < sim.Size; y++)
            {
                for (int x = 0; x < sim.Size; x++)
                {
                    Console.Write(sim[x, y] ? "1" : "0");
                }

                Console.WriteLine();
            }
        }
    }
}
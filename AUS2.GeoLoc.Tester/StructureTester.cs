using AUS2.GeoLoc.Structures;
using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.Tester
{
    public class StructureTester
    {
        private ExtendibleHashingDirectory<Property> hashing;
        private Dictionary<int, Property> helpStructure;
        private bool exit;
        private int input;
        private int idSequence = 0;
        private Random rnd;

        public void Start()
        {
            Console.WriteLine("--- Extended Hashing Tester ---");

            UtilityOperations.GetDiskFreeSpace("C:\\", out var SectorsPerCluster, out var BytesPerSector, out var NumberOfFreeClusters, out var TotalNumberOfClusters);
            var ClusterSize = SectorsPerCluster * BytesPerSector;
            var emptyProp = new Property();
            Console.WriteLine($"Record size: {emptyProp.GetSize()}");
            Console.WriteLine($"Cluster size: {ClusterSize}");
            var bFactor = (ClusterSize - 8) / emptyProp.GetSize();
            Console.WriteLine($"Recomended BFactor: {bFactor}");

            Console.Write($"Blocking factor (default {bFactor}): ");
            var inp = Console.ReadLine();
            bFactor = string.IsNullOrWhiteSpace(inp) ? bFactor : int.Parse(inp);

            Console.Write("File path (default file.dat): ");
            inp = Console.ReadLine();
            inp = String.IsNullOrWhiteSpace(inp) ? "file.dat" : inp;
            hashing = new ExtendibleHashingDirectory<Property>(inp, bFactor);

            Console.Write("Seed (default is random): ");
            inp = Console.ReadLine();
            var seed = string.IsNullOrWhiteSpace(inp) ? Guid.NewGuid().GetHashCode() : int.Parse(inp);
            Console.WriteLine($"Seed = {seed}");

            rnd = new Random(seed);
            helpStructure = new Dictionary<int, Property>();

            do {
                Console.WriteLine();
                PrintStats();
                Console.WriteLine();
                PrintMenu();
                GetInput();
                Console.WriteLine();
                HandleInput();
            } while (!exit);
        }

        private void PrintStats()
        {
            Console.WriteLine($"Current Id Sequence: {idSequence}");
            Console.WriteLine($"Records: {helpStructure.Count}");
        }

        private void HandleInput()
        {
            switch (input) {
                case 0: // Insert
                    Console.Write("Number of records (default 1): ");
                    var count = int.Parse(Console.ReadLine() ?? "1");
                    DoInsert(count);
                    break;
                case 1: // Find
                    Console.Write("Property Id: ");
                    var id = int.Parse(Console.ReadLine() ?? "1");

                    Console.WriteLine(DoFind(id)
                        ? "Record was found."
                        : "Record with given keys doesn't exist.");
                    break;
                //case 2: // Delete
                //    break;
                case 3: // Random
                    Console.Write("Number of operations: ");
                    var operationsCount = int.Parse(Console.ReadLine()!);
                    DoRandomOperations(operationsCount);
                    break;
                case 4: // Exit
                    exit = true;
                    break;
                default:
                    exit = true;
                    break;
            }
        }

        private bool DoFind(int id = -1)
        {
            if (helpStructure.Count < 1) return false;
            
            if (id >= 0) {
                var prop = new Property() { Id = id };
                return hashing.Find(prop).CustomEquals(helpStructure[id]);
            } else {
                var prop = helpStructure[rnd.Next() % idSequence];
                return hashing.Find(prop).CustomEquals(prop);
            }
        }

        private void DoInsert(int count)
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++) {
                var prop = new Property() { Id = idSequence, RegisterNumber = idSequence, Description = "Jojo" };
                helpStructure.Add(idSequence, prop);
                hashing.Add(prop);
                ++idSequence;
            }
            //watch.Stop();
            //Console.WriteLine($"Duration: {watch.ElapsedMilliseconds}ms");
        }

        private void DoRandomOperations(int operationsCount)
        {
            var failures = 0;
            for (int i = 0; i < operationsCount; i++) {
                var probability = rnd.NextDouble();
                if (probability < 0.5) // insert
                    DoInsert(1);
                else if (helpStructure.Count > 0)
                    if (!DoFind())
                        ++failures;
            }
            Console.WriteLine($"Failures: {failures}");
        }

        private void GetInput()
        {
            Console.Write("Input:~ $ ");
            input = int.Parse(Console.ReadLine() ?? "4");
        }

        private void PrintMenu()
        {
            Console.WriteLine("[0] Insert");
            Console.WriteLine("[1] Find");
            Console.WriteLine("[WIP] Delete");
            Console.WriteLine("[3] Random Operations");
            Console.WriteLine("[4] Exit");
        }
    }
}

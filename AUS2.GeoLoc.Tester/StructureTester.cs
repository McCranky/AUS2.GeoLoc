using AUS2.GeoLoc.Structures.Hashing;
using AUS2.GeoLoc.Structures.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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
            hashing = new ExtendibleHashingDirectory<Property>(inp, bFactor, 9);

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
                    var findId = int.Parse(Console.ReadLine() ?? "1");

                    Console.WriteLine(DoFind(findId)
                        ? "Record was found."
                        : "Record with given id doesn't exist.");
                    break;
                case 2: // Delete
                    Console.Write("Property Id: ");
                    var deleteId = int.Parse(Console.ReadLine() ?? "-1");

                    Console.WriteLine(DoDelete(deleteId)
                        ? "Record was deleted."
                        : "Record with given id doesn't exist.");
                    break;
                case 3: // Random
                    Console.Write("Number of operations: ");
                    var operationsCount = int.Parse(Console.ReadLine()!);
                    DoRandomOperations(operationsCount);
                    break;
                case 4: // Save
                    hashing.Save();
                    break;
                case 5: // Clear
                    Clear();
                    break;
                case 6: // Exit
                    exit = true;
                    break;
                default:
                    exit = true;
                    break;
            }
        }

        private void Clear()
        {
            var prop = new Property();
            var counter = 0;
            foreach (var item in helpStructure) {
                ++counter;
                prop.Id = item.Key;
                if (!hashing.Delete(prop)) {
                    Console.WriteLine("Cannot delete: " + prop.Id);
                }

            }
        }

        private bool DoDelete(int deleteId = -1)
        {
            if (helpStructure.Count < 1) return false;

            var key = rnd.Next() % helpStructure.Count;
            if (deleteId >= 0) {
                if (!helpStructure.TryGetValue(key, out var prop)) return false;

                helpStructure.Remove(key);
                return hashing.Delete(prop);
            } else {
                var pair = helpStructure.ElementAt(key);
                helpStructure.Remove(pair.Key);
                return hashing.Delete(pair.Value);
            }
        }

        private bool DoFind(int id = -1)
        {
            if (id >= 0) {
                var prop = new Property() { Id = id };

                if (helpStructure.TryGetValue(id, out prop)) {
                    return hashing.Find(prop).CustomEquals(prop);
                }
                return false;
            } else {
                var key = rnd.Next() % helpStructure.Count;
                var pair = helpStructure.ElementAt(key);
                return hashing.Find(pair.Value).CustomEquals(pair.Value);
            }
        }

        private void DoInsert(int count)
        {
            for (int i = 0; i < count; i++) {
                var prop = new Property() { Id = idSequence, RegisterNumber = idSequence, Description = "Jojo" };
                helpStructure.Add(idSequence, prop);
                hashing.Add(prop);
                ++idSequence;
            }
        }

        private void DoRandomOperations(int operationsCount)
        {
            var findFailures = 0;
            var deleteFailures = 0;
            for (int i = 0; i < operationsCount; i++) {
                var probability = rnd.NextDouble();
                if (probability < 0.5) // insert
                    DoInsert(1);
                else if (helpStructure.Count > 0)
                    if (probability < 0.75) {
                        if (!DoFind())
                            ++findFailures;
                    } else {
                        if (!DoDelete())
                            ++deleteFailures;
                    }
            }
            Console.WriteLine($"Find Failures: {findFailures}");
            Console.WriteLine($"Delete Failures: {deleteFailures}");
        }

        private void GetInput()
        {
            Console.Write("Input:~ $ ");
            var inp = Console.ReadLine();
            input = string.IsNullOrWhiteSpace(inp) ? 6 : int.Parse(inp);
        }

        private void PrintMenu()
        {
            Console.WriteLine("[0] Insert");
            Console.WriteLine("[1] Find");
            Console.WriteLine("[2] Delete");
            Console.WriteLine("[3] Random Operations");
            //Console.WriteLine("[4] Save");
            Console.WriteLine("[5] Clear");
            Console.WriteLine("[6] Exit");
        }
    }
}

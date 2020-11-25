using AUS2.GeoLoc.Structures.Hashing;
using AUS2.GeoLoc.Structures.Tables;
using AUS2.GeoLoc.Structures.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AUS2.GeoLoc.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            //SortedTable<int, int> table = new SortedTable<int, int>();
            //for (int i = 100; i >= 0; i--) {
            //    table.Add(i, i);
            //}
            //table.RemoveRange(3, 15);
            //foreach (var item in table.Items) {
            //    Console.WriteLine(item.Value);
            //}

            var testing = new StructureTester();
            testing.Start();

            //int a = 6;
            //int b = a ^ (1 << 1);
            //Console.WriteLine(b);

            //var bil = new List<BlockInformations>();
            //bil.Add(new BlockInformations { Address = 1, Depth = 1, Records = 1 });
            //var i = bil.FirstOrDefault(x => x.Address == 1);
            //i.Address = 2;
            //Console.WriteLine(bil[0].Address);
            //i = new BlockInformations { Address = 9 };
            //Console.WriteLine(bil[0].Address);

            //StructureTest();
            //Console.WriteLine(BitsOperations.GetIntFromBitArray(new BitArray(new bool[] { false, true, true })));
        }

        private static void StructureTest()
        {
            var directory = new ExtendibleHashingDirectory<Property>("file.dat", 3);
            var iterations = 5000;
            for (int i = 0; i < iterations; i++) {
                directory.Add(new Property { Id = i, Description = "Hoho", RegisterNumber = i });
            }

            var pFind = new Property { Id = 1};
            //directory.Delete(pFind);
            //pFind.Id = 2;
            //directory.Delete(pFind);

            for (int i = 0; i < iterations; i++) {
                pFind.Id = i;
                Console.WriteLine("Delete: " + directory.Delete(pFind));
                //if (i % 2 == 0)
                //    Console.WriteLine("Delete: " + directory.Delete(pFind));
                //else
                //    Console.WriteLine("Found: " + directory.Find(pFind));
            }
            //Console.WriteLine("---");
            //for (int i = 0; i < iterations; i++) {
            //    directory.Add(new Property { Id = i, Description = "Hoho", RegisterNumber = i });
            //}

            //for (int i = 0; i < iterations; i++) {
            //    pFind.Id = i;
            //    Console.WriteLine(directory.Delete(pFind));
            //}
        }

        private static void BitsConversionTest()
        {
            var tmp = new BitArray(new bool[] { true, false, false});
            BitsOperations.ReverseBits(ref tmp);
            var enu = tmp.GetEnumerator();
            while (enu.MoveNext()) {
                Console.WriteLine(enu.Current);
            }


            int a = 0;
            byte b = 16;
            a += b * 3;
            Console.WriteLine(a);
            var num = 3;
            var arr = new BitArray(BitConverter.GetBytes(num));
            //var arr = new BitArray(new bool[] { true, true});
            var ret = new byte[(arr.Length - 1) / 8 + 1];
            arr.CopyTo(ret, 0);

            arr.LeftShift(arr.Length - 2);
            var en = arr.GetEnumerator();
            while (en.MoveNext()) {
                Console.WriteLine(en.Current);
            }
            Console.WriteLine(string.Join("", arr));
            Console.WriteLine(BitsOperations.GetIntFromBitArray(arr));

            Console.WriteLine(
                BitsOperations.GetIntFromBitArray(
                    BitsOperations.GetFirstBits(new BitArray(new bool[] { true, true, true }), 3)
                    ));
        }

        private static void RunManualTest()
        {
            var p1 = new Property { Id = 69, Description = "Hoho", RegisterNumber = 9 };
            Console.WriteLine(p1);
            var tmp = p1.ToByteArray();
            var p2 = new Property();
            p2.FromByteArray(tmp);
            Console.WriteLine("(De)serialisation...");
            Console.WriteLine(p2);
            Console.WriteLine(tmp.Length + " " + p1.GetSize());
            Console.WriteLine("\n---------------------------------------\n");



            var rnd = new Random();
            Block<Property> block = new Block<Property>(3, p1.GetEmptyClass());
            foreach (var record in block.Records) {
                record.Id = Guid.NewGuid().GetHashCode();
                record.RegisterNumber = rnd.Next();
                record.Description = "Jojo";
                Console.WriteLine(record);
            }
            block.ValidCount = 3;

            var tmp2 = block.ToByteArray();
            var block2 = new Block<Property>(3, p1.GetEmptyClass());
            block2.FromByteArray(tmp2);

            Console.WriteLine("(De)serialisation...");
            foreach (var record in block2.Records) {
                Console.WriteLine(record);
            }
            Console.WriteLine($"{block.GetSize()} {tmp2.Length}");
        }
    }
}

using AUS2.GeoLoc.Structures;
using System;
using System.Collections;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var directory = new ExtendibleHashingDirectory<Property>("file.dat", 1);
            directory.Add(new Property { Id = 9, Description = "Hoho", RegisterNumber = 9 });
            directory.Add(new Property { Id = 69, Description = "Hoho", RegisterNumber = 9 });
            directory.Add(new Property { Id = 6, Description = "Hoho", RegisterNumber = 9 });
            var pFind = new Property { Id = 69 };
            Console.WriteLine(directory.Find(pFind));
            //BitsConversionTest();
            //RunManualTest();
        }

        private static void BitsConversionTest()
        {
            var num = 2812;
            var arr = new BitArray(BitConverter.GetBytes(num));
            var ret = new byte[(arr.Length - 1) / 8 + 1];
            arr.CopyTo(ret, 0);
            Console.WriteLine(BitConverter.ToInt32(ret));
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
            block._validCount = 3;

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

using System;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var id = Guid.NewGuid();
            var number = 123;
            var desc = "Desc...";
            var max_desc = 20;

            Console.WriteLine("---Data---");
            Console.WriteLine(id);
            Console.WriteLine(number);
            Console.WriteLine(desc);
            Console.WriteLine("----------\n");

            using (var ms = new MemoryStream()) {
                ms.Write(id.ToByteArray());
                ms.Write(BitConverter.GetBytes(number));
                ms.Write(BitConverter.GetBytes(desc.Length));
                ms.Write(Encoding.UTF8.GetBytes(desc));
                for (int i = 0; i < max_desc - desc.Length; i++) {
                    ms.WriteByte(BitConverter.GetBytes('x')[0]);
                }

                Console.WriteLine(
                "Capacity = {0}, Length = {1}, Position = {2}\n",
                ms.Capacity.ToString(),
                ms.Length.ToString(),
                ms.Position.ToString());

                
                
                Console.WriteLine("------Seek to 0------");
                ms.Seek(0, SeekOrigin.Begin);

                var buffer = new byte[16];
                ms.Read(buffer, 0, buffer.Length);
                Console.WriteLine(new Guid(buffer));

                buffer = new byte[sizeof(int)];
                ms.Read(buffer, 0, buffer.Length);
                Console.WriteLine(BitConverter.ToInt32(buffer));

                ms.Read(buffer, 0, buffer.Length);
                var descLength = BitConverter.ToInt32(buffer);
                Console.WriteLine(descLength);

                buffer = new byte[max_desc];
                ms.Read(buffer, 0, buffer.Length);
                Console.WriteLine(Encoding.UTF8.GetString(buffer).Substring(0, descLength));

                Console.WriteLine(string.Join(' ', ms.ToArray()));
            }
        }
    }
}

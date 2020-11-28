using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Structures.Hashing
{
    public class BlockInfo : IRecord
    {
        public int Address { get; set; }
        public int Records { get; set; }
        public int Depth { get; set; }
        public int OverflowAddress { get; set; } = int.MinValue;

        public void FromByteArray(byte[] array)
        {
            using (var ms = new MemoryStream(array)) {
                var buffer = new byte[sizeof(int)];
                ms.Read(buffer);
                Address = BitConverter.ToInt32(buffer);

                ms.Read(buffer);
                Records = BitConverter.ToInt32(buffer);

                ms.Read(buffer);
                Depth = BitConverter.ToInt32(buffer);

                ms.Read(buffer);
                OverflowAddress = BitConverter.ToInt32(buffer);
            }
        }

        public int GetSize()
        {
            return sizeof(int) * 4;
        }

        public byte[] ToByteArray()
        {
            byte[] result;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(Address));
                ms.Write(BitConverter.GetBytes(Records));
                ms.Write(BitConverter.GetBytes(Depth));
                ms.Write(BitConverter.GetBytes(OverflowAddress));
                result = ms.ToArray();
            }
            return result;
        }
    }
}

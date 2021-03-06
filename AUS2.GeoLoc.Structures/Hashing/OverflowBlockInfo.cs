﻿using System;
using System.IO;

namespace AUS2.GeoLoc.Structures.Hashing
{
    /// <summary>
    /// Represents informations about overflow blocks
    /// </summary>
    public class OverflowBlockInfo : IRecord
    {
        public int Records { get; set; } = 1;
        public int NextOwerflowAddress { get; set; } = int.MinValue;

        public void FromByteArray(byte[] array)
        {
            using (var ms = new MemoryStream(array)) {
                var buffer = new byte[sizeof(int)];
                ms.Read(buffer);
                Records = BitConverter.ToInt32(buffer);

                ms.Read(buffer);
                NextOwerflowAddress = BitConverter.ToInt32(buffer);
            }
        }

        public int GetSize()
        {
            return sizeof(int) * 2;
        }

        public byte[] ToByteArray()
        {
            byte[] result;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(Records));
                ms.Write(BitConverter.GetBytes(NextOwerflowAddress));
                result = ms.ToArray();
            }
            return result;
        }
    }
}

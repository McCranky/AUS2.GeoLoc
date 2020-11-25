using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AUS2.GeoLoc.Structures.Utilities
{
    public static class BitsOperations
    {
        public static BitArray GetFirstBits(BitArray from, int count)
        {
            if (from.Count < count) return null;

            var enumerator = from.GetEnumerator();
            var result = new BitArray(count);
            var index = 0;

            while (enumerator.MoveNext() && index < count) {
                result.Set(index++, (bool)enumerator.Current);
            }

            return result;
        }

        public static int GetIntFromBitArray(BitArray bitArray)
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }

        public static void ReverseBits(ref BitArray array)
        {
            var length = array.Length;
            var mid = length / 2;

            for (var i = 0; i < mid; i++) {
                var bit = array[i];
                array[i] = array[length - i - 1];
                array[length - i - 1] = bit;
            }
        }

        public static void PrintBits(BitArray array)
        {
            var en = array.GetEnumerator();
            while (en.MoveNext()) {
                Console.Write(en.Current + " ");
            }
            Console.WriteLine(".");
        }
    }
}

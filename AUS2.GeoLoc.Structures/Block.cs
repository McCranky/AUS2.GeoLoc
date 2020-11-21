using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public class Block<T> : IRecord
    {
        private int bFactor;
        private T t;

        public Block(int bFactor, T t)
        {
            this.bFactor = bFactor;
            this.t = t;
        }

        public void FromByteArray(byte[] array)
        {
            throw new NotImplementedException();
        }

        public int GetSize()
        {
            throw new NotImplementedException();
        }

        public byte[] ToByteArray()
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<T> GetRecords()
        {
            throw new NotImplementedException();
        }
    }
}

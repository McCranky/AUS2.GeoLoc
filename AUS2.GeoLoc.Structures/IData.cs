using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public interface IData<T> : IRecord
    {
        public BitArray GetHash();
        public bool CustomEquals(T data);
        public T GetEmptyClass();
    }
}

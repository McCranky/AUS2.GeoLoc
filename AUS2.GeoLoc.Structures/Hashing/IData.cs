using System.Collections;

namespace AUS2.GeoLoc.Structures.Hashing
{
    /// <summary>
    /// Represents data that can be stored in extendable hashing directory
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IData<T> : IRecord
    {
        public BitArray GetHash();
        public bool CustomEquals(T data);
        public T GetEmptyClass();
    }
}

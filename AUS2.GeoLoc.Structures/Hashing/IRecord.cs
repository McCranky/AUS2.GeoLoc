namespace AUS2.GeoLoc.Structures.Hashing
{
    /// <summary>
    /// Contains all methods that are necesary for classes which can be written to binary file
    /// </summary>
    public interface IRecord
    {
        public byte[] ToByteArray();
        public void FromByteArray(byte[] array);
        public int GetSize();
    }
}

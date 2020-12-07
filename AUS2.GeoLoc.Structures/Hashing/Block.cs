using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AUS2.GeoLoc.Structures.Hashing
{
    /// <summary>
    /// A container that stores records. Optimal size is size of cluster on disk on which is stored on.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Block<T> : IRecord where T : IData<T>
    {
        public int BFactor { get; private set; }
        private readonly T _Class;
        private readonly List<T> _Records;
        public int ValidCount { get; set; } = 0;
        public int BlockDepth { get; set; } = 1;

        public Block(int bFactor, T t)
        {
            BFactor = bFactor;
            _Class = t;
            _Records = new List<T>(BFactor);
            for (int i = 0; i < BFactor; i++) {
                _Records.Add(_Class.GetEmptyClass());
            }
        }

        public List<T> Records => _Records.Take(ValidCount).ToList();

        public T FindRecord(T record)
        {
            foreach (var rc in Records) {
                if (rc.CustomEquals(record)) {
                    return rc;
                }
            }
            return default;
        }

        public bool AddRecord(T record)
        {
            if (ValidCount == _Records.Count) return false; // There is no space for another record
            _Records[ValidCount++] = record;
            return true;
        }

        public bool UpdateRecord(T record)
        {
            for (int i = 0; i < ValidCount; i++) {
                if (_Records[i].CustomEquals(record)) {
                    _Records[i] = record;
                    return true;
                }
            }
            return false;
        }

        public T DeleteRecord(T record)
        {
            var recordToDelete = -1;
            for (int i = 0; i < BFactor; i++) {
                if (recordToDelete == -1 && _Records[i].CustomEquals(record)) {
                    recordToDelete = i;
                }
                if (recordToDelete != -1 && i == ValidCount - 1) {
                    if (i != recordToDelete)
                        _Records[recordToDelete] = _Records[i];

                    --ValidCount;
                    return _Records[i];
                }
            }

            return default;
        }

        public byte[] ToByteArray()
        {
            byte[] result;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(ValidCount));
                ms.Write(BitConverter.GetBytes(BlockDepth));

                foreach (var record in _Records) {
                    ms.Write(record.ToByteArray());
                }

                result = ms.ToArray();
            }
            return result;
        }

        public void FromByteArray(byte[] array)
        {
            using (var ms = new MemoryStream(array)) {
                var buffer = new byte[sizeof(int)];
                ms.Read(buffer);
                ValidCount = BitConverter.ToInt32(buffer);

                ms.Read(buffer);
                BlockDepth = BitConverter.ToInt32(buffer);

                buffer = new byte[_Class.GetSize()];
                for (int i = 0; i < ValidCount; i++) {
                    ms.Read(buffer);
                    _Records[i].FromByteArray(buffer);
                }
            }
        }

        public int GetSize()
        {
            return sizeof(int) * 2 + _Records.Count * _Class.GetSize();
        }
    }
}

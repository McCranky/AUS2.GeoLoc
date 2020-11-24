using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public class Block<T> : IRecord where T : IData<T>
    {
        private readonly int _BFactor;
        private readonly T _Class;
        private readonly List<T> _Records;
        public int ValidCount { get; set; } = 0;
        public int BlockDepth { get; set; } = 1;

        public Block(int bFactor, T t)
        {
            _BFactor = bFactor;
            _Class = t;
            _Records = new List<T>(_BFactor);
            for (int i = 0; i < _BFactor; i++) {
                _Records.Add(_Class.GetEmptyClass());
            }
        }

        public List<T> Records => _Records.Take(ValidCount).ToList();

        public bool AddRecord(T record)
        {
            if (ValidCount == _Records.Count) return false; // There is no space for another record
            _Records[ValidCount++] = record;
            return true;
        }

        public T DeleteRecord(T record)
        {
            var recordToDelete = -1;
            for (int i = 0; i < _BFactor; i++) {
                if (_Records[i].CustomEquals(record)) {
                    recordToDelete = i;
                }
                if (recordToDelete != - 1 && i != -1 && i == ValidCount - 1) {
                    if (i != recordToDelete)
                        _Records[recordToDelete] = _Records[i];

                    --ValidCount;
                    return _Records[i];
                }
            }

            return default(T);
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

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
        public int _validCount = 0;
        private int _hashDepth = 1;

        public Block(int bFactor, T t)
        {
            _BFactor = bFactor;
            _Class = t;
            _Records = new List<T>(_BFactor);
            for (int i = 0; i < _BFactor; i++) {
                _Records.Add(_Class.GetEmptyClass());
            }
        }

        public List<T> Records => _Records;//.Take(_ValidCount).ToList();

        public bool AddRecord(T record)
        {
            if (_validCount == _Records.Count) return false; // There is no space for another record
            _Records[_validCount++] = record;
            return true;
        }

        public byte[] ToByteArray()
        {
            byte[] result;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(_validCount));

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
                _validCount = BitConverter.ToInt32(buffer);

                buffer = new byte[_Class.GetSize()];
                for (int i = 0; i < _validCount; i++) {
                    ms.Read(buffer);
                    _Records[i].FromByteArray(buffer);
                }
            }
        }

        public int GetSize()
        {
            return sizeof(int) + _Records.Count * _Class.GetSize();
        }
    }
}

using AUS2.GeoLoc.Structures.Hashing;
using AUS2.GeoLoc.Structures.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Structures.FileManagers
{
    public class FileManager : IDisposable, IRecord
    {
        private FileStream _fileStream;
        private SortedTable<int, int> _freeAddresses;
        private readonly int _blockSize;

        private int LastAddress => (int)_fileStream?.Length;

        public FileManager(string filePath, int blockSize)
        {
            _fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
            _freeAddresses = new SortedTable<int, int>();
            _blockSize = blockSize;
        }


        public int GetFreeAddress()
        {
            if (_freeAddresses.Count > 0) {
                var address = _freeAddresses.Items[0].Value;
                _freeAddresses.Items.RemoveAt(0);
                return address;
            }
            return LastAddress;
        }

        public void FreeAddress(int address)
        {
            // ak to bolo adresa na konci tak zmazať všetky volne adresy od konca až po platne data
            if (address == LastAddress - _blockSize) { // LastAddress - _blockSize -> posledna pridelena adresa.. na LastAddress ešte nič nie je
                var blocksToErase = 1;

                for (int i = _freeAddresses.Count - 1; i >= 0; i--) {
                    var key = _freeAddresses.Items[i].Key;
                    if (key == address - _blockSize) {
                        address = key;
                        ++blocksToErase;
                    } else {
                        break;
                    }
                }

                _fileStream.Seek(0, SeekOrigin.Begin);
                _fileStream.SetLength(LastAddress - blocksToErase * _blockSize);
                if (blocksToErase > 1) { // lebo ak je 1, tak sa tam ani nedostala
                    _freeAddresses.RemoveRange(_freeAddresses.Count - blocksToErase - 1, blocksToErase - 1);
                }
            } else {
                _freeAddresses.Add(address, address);
            }
        }

        public void WriteBytes(int address, byte[] byts)
        {
            _fileStream.Seek(address, SeekOrigin.Begin);
            _fileStream.Write(byts);
        }

        public void ReadBytes(int address, ref byte[] buffer)
        {
            _fileStream.Seek(address, SeekOrigin.Begin);
            _fileStream.Read(buffer);
        }

        public void Clear()
        {
            _freeAddresses.Clear();
            _fileStream.Seek(0, SeekOrigin.Begin);
            _fileStream.SetLength(0);
        }

        public void Dispose()
        {
            if (_fileStream != null) {
                _fileStream.Close();
            }
        }

        public virtual byte[] ToByteArray()
        {
            byte[] result;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(_freeAddresses.Count));

                for (int i = 0; i < _freeAddresses.Count; i++) {
                    ms.Write(BitConverter.GetBytes(_freeAddresses.Items[i].Key));
                }
                result = ms.ToArray();
            }
            return result;
        }

        public virtual void FromByteArray(byte[] array)
        {
            using (var ms = new MemoryStream(array)) {
                var buffer = new byte[sizeof(int)];
                while (ms.Position < ms.Length) {
                    ms.Read(buffer);
                    var address = BitConverter.ToInt32(buffer);
                    _freeAddresses.Add(address, address);
                }
            }
        }

        public virtual int GetSize()
        {
            return sizeof(int) * (_freeAddresses.Count + 1); // 1 - počet prvkov
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public class ExtendibleHashingDirectory<T> : IDisposable where T : IData<T> 
    {
        public int BFactor { get; private set; }
        public string FilePath { get; set; }
        private FileStream fileStream;
        private Dictionary<int, int> _directory;
        private int _lastAddress = 0;
        //dynamicke pole celych čisel
        // hlbka hashovania(adresaru)

        public ExtendibleHashingDirectory(string filePath, int bFactor)
        {
            _directory = new Dictionary<int, int>();
            FilePath = filePath;
            BFactor = bFactor; //TODO?? ak bude čas tak dorobyť automaticke vypočitanie na zaklade velkosti clustera a T
            fileStream = new FileStream(FilePath, FileMode.OpenOrCreate);
        }

        public T Find(T data)
        {
            var hash = data.GetHash();

            //1. ziskaj prvých D bitov z hashu
            //2. preved z bin na dec a pristup na dany index do pola adries blokov a spristupni dany blok
            //3. v bloku najdi zaznam
            var blockAddress = HashToBlockAddress(hash);

            // inicializacia bloku
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            var blockBytes = new byte[block.GetSize()];
            fileStream.Seek(blockAddress, SeekOrigin.Begin);
            fileStream.Read(blockBytes); 
            block.FromByteArray(blockBytes);
            //hladanie zaznamu v bloku
            foreach (var record in block.Records) {
                if (data.CustomEquals(record))
                    return record;
            }

            return default(T);
        }

        public bool Add(T data)
        {
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            block.AddRecord(data);

            var blockIndex = BiteArrayToInt32(data.GetHash());
            var blockAddress = _lastAddress + block.GetSize();
            _directory.Add(blockIndex, blockAddress);

            fileStream.Seek(blockAddress, SeekOrigin.Begin);
            fileStream.Write(block.ToByteArray());

            _lastAddress = blockAddress;
            return true;
        }

        private int HashToBlockAddress(BitArray hash)
        {
            var blockIndex = BiteArrayToInt32(hash);
            return _directory[blockIndex];
        }

        private int BiteArrayToInt32(BitArray bits)
        {
            var bytes = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(bytes, 0);
            return BitConverter.ToInt32(bytes);
        }

        public void Dispose()
        {
            fileStream.Close();
        }
    }
}

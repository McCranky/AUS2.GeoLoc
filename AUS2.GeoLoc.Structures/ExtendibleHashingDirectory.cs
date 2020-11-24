using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public class ExtendibleHashingDirectory<T> : IDisposable where T : IData<T> 
    {
        public int BFactor { get; private set; }
        private FileStream fileStream;
        private List<int> _blockAddresses;
        private int _hashDepth = 1;
        private Dictionary<int, int> _overflowFile;
        private int _lastAddress = 0;

        public ExtendibleHashingDirectory(string filePath, int bFactor)
        {
            fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
            BFactor = bFactor; //TODO?? ak bude čas tak dorobyť automaticke vypočitanie na zaklade velkosti clustera a T
            _overflowFile = new Dictionary<int, int>();
            _blockAddresses = new List<int>((int)Math.Pow(2, _hashDepth));

            Init();
        }

        
        public T Find(T data)
        {
            //1. ziskaj prvých D bitov z hashu
            //2. preved z bin na dec a pristup na dany index do pola adries blokov a spristupni dany blok
            var block = GetBlock(data, out _);
            //3. v bloku najdi zaznam
            foreach (var record in block?.Records) {
                if (data.CustomEquals(record))
                    return record;
            }

            return default(T);
        }

        public bool Add(T data)
        {
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            while (true) {
                block = GetBlock(data, out var blockAddress);
                if (block.ValidCount == BFactor) { // blok je plný
                    if (block.BlockDepth == _hashDepth) { // ak su rovnake hlbky
                        // zdvojnasob subor
                        ++_hashDepth;
                        var newAddresses = new List<int>((int)Math.Pow(2, _hashDepth));
                        for (int i = 0; i < (int)Math.Pow(2, _hashDepth - 1); i++) {
                            newAddresses.Add(_blockAddresses[i]);
                            newAddresses.Add(_blockAddresses[i]);
                        }
                        _blockAddresses = newAddresses;
                    }
                    // rozdelenie bloku (split - vytvorenie noveho bloku)


                    // musim vybrať zaznamy z bloku a rozdeliť ich podla novej hlbky (velkosti hashu)
                    var block0 = new Block<T>(BFactor, data.GetEmptyClass()); // blok s bin 0 na konci (parne čislo)
                    var block1 = new Block<T>(BFactor, data.GetEmptyClass()); // blok s bin 1 na konci (neparne čislo)
                    block0.BlockDepth = block.BlockDepth + 1;
                    block1.BlockDepth = block.BlockDepth + 1;
                    foreach (var record in block.Records) {
                        var bitsFromHash = BitsOperations.GetFirstBits(record.GetHash(), block0.BlockDepth);

                        if (bitsFromHash.Get(block0.BlockDepth - 1) == false)
                            block0.AddRecord(record);
                        else
                            block1.AddRecord(record);
                    }

                    //aktualizovanie adries
                    _lastAddress = _lastAddress + block.GetSize();
                    var firstIndexOfAddress = _blockAddresses.IndexOf(blockAddress);
                    var lastIndexOfAddress = _blockAddresses.LastIndexOf(blockAddress);
                    var splitIndex = (lastIndexOfAddress - firstIndexOfAddress + 1) / 2;
                    for (int i = firstIndexOfAddress + splitIndex; i <= lastIndexOfAddress; i++) {
                        _blockAddresses[i] = _lastAddress;
                    }

                    //zapisanie novych blokov do suboru
                        // parny blok (menšie čislo) na pôvodnu adresu
                    fileStream.Seek(blockAddress, SeekOrigin.Begin);
                    fileStream.Write(block0.ToByteArray());
                        // neparny blok (väčšie čislo) na novu adresu
                    fileStream.Seek(_lastAddress, SeekOrigin.Begin);
                    fileStream.Write(block1.ToByteArray());

                } else {
                    block.AddRecord(data);
                    fileStream.Seek(blockAddress, SeekOrigin.Begin);
                    fileStream.Write(block.ToByteArray());
                    break;
                }
            }

            return true;
        }

        private void Init()
        {
            var classInstance = (T)Activator.CreateInstance(typeof(T));
            var block = new Block<T>(BFactor, classInstance.GetEmptyClass());

            _blockAddresses.Add(_lastAddress);
            fileStream.Seek(_lastAddress, SeekOrigin.Begin);
            fileStream.Write(block.ToByteArray());

            _lastAddress += block.GetSize();

            _blockAddresses.Add(_lastAddress);
            fileStream.Seek(_lastAddress, SeekOrigin.Begin);
            fileStream.Write(block.ToByteArray());
        }

        private Block<T> GetBlock(T data, out int address)
        {
            var hash = data.GetHash();
            //1. ziskaj prvých D bitov z hashu
            //2. preved z bin na dec
            var bits = BitsOperations.GetFirstBits(hash, _hashDepth);
            BitsOperations.ReverseBits(ref bits);
            var index = BitsOperations.GetIntFromBitArray(
                bits
                );
            //2.2 pristup na dany index do pola adries blokov
            address = _blockAddresses[index];
            //2.3 spristupni dany blok
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            var blockBytes = new byte[block.GetSize()];
            fileStream.Seek(address, SeekOrigin.Begin);
            fileStream.Read(blockBytes);
            block.FromByteArray(blockBytes);
            return block;
        }
        
        public void Dispose()
        {
            fileStream.Close();
        }
    }
}

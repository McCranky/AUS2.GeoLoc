using System;
using System.Linq;
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
        private int _lastAddress = 0;

        //private List<Tuple<int, int>> _deepestBlocksAddresses;
        private List<BlockInformations> _blockInformations;

        public ExtendibleHashingDirectory(string filePath, int bFactor)
        {
            fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
            BFactor = bFactor; //TODO?? ak bude čas tak dorobyť automaticke vypočitanie na zaklade velkosti clustera a T
            _blockAddresses = new List<int>((int)Math.Pow(2, _hashDepth));
            //_deepestBlocksAddresses = new List<Tuple<int, int>>();
            _blockInformations = new List<BlockInformations>();

            Init();
        }

        
        public T Find(T data)
        {
            //1. ziskaj prvých D bitov z hashu
            //2. preved z bin na dec a pristup na dany index do pola adries blokov a spristupni dany blok
            var block = GetBlock(data, out _, out _, false);
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
                block = GetBlock(data, out var blockAddress, out _);
                if (block.ValidCount == BFactor) { // block is full
                    if (block.BlockDepth == _hashDepth) {
                        DoubleAddressSize();
                    }

                    SplitOperation(ref data, ref block, blockAddress);
                    
                } else {
                    block.AddRecord(data);
                    WriteBlock(blockAddress, block);
                    var blockInfo = _blockInformations.FirstOrDefault(info => info.Address == blockAddress);
                    blockInfo.Records += 1;
                    break;
                }
            }

            return true;
        }

        public bool Delete(T data)
        {
            // operacia prebieha cyklicky ako vkladanie
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            var z = 1;
            while (z-- > 0) {
                block = GetBlock(data, out var blockAddress, out var blockIndex);
                if (block == null) return false;
                // 1 najdi blok a vymaž prvok v ňom
                block.DeleteRecord(data);
                WriteBlock(blockAddress, block);
                var currentBlockInfo = GetBlockInformations(blockAddress);
                --currentBlockInfo.Records;



                if (_hashDepth == 1 || block.BlockDepth == 1) break;
                // 2 pozri susedné bloky, či sa dajú spojiť
                var neighborInfo = GetNeighborInformations(blockIndex, block.BlockDepth, out var neighborIndex);
                if (neighborInfo.Address == currentBlockInfo.Address || // TODO ako to je možne?
                    neighborInfo.Depth == currentBlockInfo.Depth ||
                    neighborInfo.Records + currentBlockInfo.Records > BFactor) break;

                // ak ano, tak sa presunu do jedneho 
                var neighborBlock = ReadBlock(neighborInfo.Address);
                var fromBlock = neighborInfo.Address > currentBlockInfo.Address ? neighborBlock : block;
                var fromInfo = neighborInfo.Address > currentBlockInfo.Address ? neighborInfo : currentBlockInfo;
                var toBlock = neighborInfo.Address < currentBlockInfo.Address ? neighborBlock : block;
                var toInfo = neighborInfo.Address < currentBlockInfo.Address ? neighborInfo : currentBlockInfo;
                foreach (var record in fromBlock.Records) {
                    toBlock.AddRecord(record);
                }
                var depthOfInteress = toBlock.BlockDepth;
                --toBlock.BlockDepth;
                --toInfo.Depth;
                toInfo.Records = toBlock.ValidCount;
                WriteBlock(toInfo.Address, toBlock);
                // a volny sa dealokuje
                UpdateAddresses(fromInfo.Address, toInfo.Address); // adresy bloku from sa nahradia adrousou z bloku to

                // a tomu čo ostal Depth -= 1
                // ak to bol posledny blok s touto hlbkou, znižuje sa hlbka adresara !!! ak to bola hlbka rovna hashDepth !!!
                // a adresar sa zmenši na polovicu
                var countOfEqualDepth = _blockInformations
                                            .Where(i => i.Depth == depthOfInteress)
                                            .Count();
                _blockInformations.Remove(fromInfo);
                if (_hashDepth != depthOfInteress || countOfEqualDepth != 1) break;
                while (HalveAddressSize()) {

                }
            }
            return true;
        }

        private bool HalveAddressSize()
        {
            --_hashDepth;
            var step = FindSmallestSequene();
            if (step <= 1) return false;
            var newAddresses = new List<int>((int)Math.Pow(2, _hashDepth));
            for (int i = 0; i < (int)Math.Pow(2, _hashDepth); i++) {
                newAddresses.Add(_blockAddresses[i * step]);
            }
            _blockAddresses = newAddresses;
            return true;
        }

        private int FindSmallestSequene()
        {
            var lastAddress = _blockAddresses[0];
            var count = 0;
            var min = int.MaxValue;
            foreach (var address in _blockAddresses) {
                if (address != lastAddress) {
                    if (count < min)
                        min = count;
                    lastAddress = address;
                    count = 0;
                }                    
                ++count;
            }
            return min;
        }

        private void UpdateAddresses(int fromAddress, int toAddress)
        {
            var index = _blockAddresses.IndexOf(fromAddress);
            while (index <= _blockAddresses.Count - 1 && _blockAddresses[index] == fromAddress) {
                _blockAddresses[index++] = toAddress;
            }
        }

        private void Init()
        {
            var classInstance = (T)Activator.CreateInstance(typeof(T));
            var block = new Block<T>(BFactor, classInstance.GetEmptyClass());

            _blockAddresses.Add(_lastAddress);
            _blockInformations.Add(new BlockInformations { Address = _lastAddress, Depth = 1, Records = 0 });
            fileStream.Seek(_lastAddress, SeekOrigin.Begin);
            fileStream.Write(block.ToByteArray());

            _lastAddress += block.GetSize();

            _blockAddresses.Add(_lastAddress);
            _blockInformations.Add(new BlockInformations { Address = _lastAddress, Depth = 1, Records = 0 });
            fileStream.Seek(_lastAddress, SeekOrigin.Begin);
            fileStream.Write(block.ToByteArray());
        }

        private void DoubleAddressSize()
        {
            ++_hashDepth;
            var newAddresses = new List<int>((int)Math.Pow(2, _hashDepth));
            for (int i = 0; i < (int)Math.Pow(2, _hashDepth - 1); i++) {
                newAddresses.Add(_blockAddresses[i]);
                newAddresses.Add(_blockAddresses[i]);
            }
            _blockAddresses = newAddresses;
        }

        private void UpdateNewAddresses(int blockSize, int forBlockAddress)
        {
            _lastAddress = _lastAddress + blockSize; //TODO novu adresu ziskat od managera volnych miest
            var firstIndexOfAddress = _blockAddresses.IndexOf(forBlockAddress);
            var lastIndexOfAddress = _blockAddresses.LastIndexOf(forBlockAddress);
            var splitIndex = (lastIndexOfAddress - firstIndexOfAddress + 1) / 2;
            for (int i = firstIndexOfAddress + splitIndex; i <= lastIndexOfAddress; i++) {
                _blockAddresses[i] = _lastAddress;
            }
        }

        private void SplitOperation(ref T data, ref Block<T> block, int blockAddress)
        {
            var block0 = new Block<T>(BFactor, data.GetEmptyClass()); // blok s bin 0 na konci (parne čislo)
            var block1 = new Block<T>(BFactor, data.GetEmptyClass()); // blok s bin 1 na konci (neparne čislo)
            var oldDepth = block.BlockDepth;
            var newDepth = oldDepth + 1;
            block0.BlockDepth = newDepth;
            block1.BlockDepth = newDepth;
            foreach (var record in block.Records) {
                var bitsFromHash = BitsOperations.GetFirstBits(record.GetHash(), block0.BlockDepth);

                if (bitsFromHash.Get(oldDepth) == false)
                    block0.AddRecord(record);
                else
                    block1.AddRecord(record);
            }

            //aktualizovanie adries
            UpdateNewAddresses(block.GetSize(), blockAddress);

            //zapisanie novych blokov do suboru a aktualizovanie informacii o blokoch
            // parny blok (menšie čislo) na pôvodnu adresu
            // bloku na povodnej adrese nasatvime novu hlbku a zaznamy
            WriteBlock(blockAddress, block0);
            var blockInfo = _blockInformations.FirstOrDefault(info => info.Address == blockAddress);
            blockInfo.Records = block0.ValidCount;
            blockInfo.Depth = newDepth;

            // neparny blok (väčšie čislo) na novu adresu
            // informacie o novom bloku musime pridať do zaznamu
            WriteBlock(_lastAddress, block1);
            _blockInformations.Add(new BlockInformations { Address = _lastAddress, Depth = newDepth, Records = block1.ValidCount });
        }

        private int GetIndexFromHash(BitArray hash, bool forFind = false)
        {
            //1. ziskaj prvých D bitov z hashu
            var bits = BitsOperations.GetFirstBits(hash, _hashDepth);
            if(!forFind)
                BitsOperations.ReverseBits(ref bits);
            //2. preved z bin na dec
            return BitsOperations.GetIntFromBitArray(
                bits
                );
        }

        private Block<T> GetBlock(T data, out int address, out int index, bool forFind = false)
        {
            address = -1;
            //1. ziskaj index
            index = GetIndexFromHash(data.GetHash(), forFind);
            if (index >= _blockAddresses.Count) return null;
            //2.2 pristup na dany index do pola adries blokov
            address = _blockAddresses[index];
            //2.3 spristupni dany blok
                                                    //var block = new Block<T>(BFactor, data.GetEmptyClass());
                                                    //var blockBytes = new byte[block.GetSize()];
                                                    //fileStream.Seek(address, SeekOrigin.Begin);
                                                    //fileStream.Read(blockBytes);
                                                    //block.FromByteArray(blockBytes);
                                                    //return block;
            return ReadBlock(address);
        }

        private BlockInformations GetNeighborInformations(int blockIndex, int blockDepth, out int neighborIndex)
        {
            neighborIndex = blockIndex ^ (1 << _hashDepth - blockDepth);
            var neighborAddress = _blockAddresses[neighborIndex];
            return GetBlockInformations(neighborAddress);
        }

        private BlockInformations GetBlockInformations(int blockAddress)
        {
            return _blockInformations.FirstOrDefault(info => info.Address == blockAddress);
        }

        private void WriteBlock(int address, Block<T> block)
        {
            fileStream.Seek(address, SeekOrigin.Begin);
            fileStream.Write(block.ToByteArray());
        }

        private Block<T> ReadBlock(int address)
        {
            var block = new Block<T>(BFactor, (T)Activator.CreateInstance(typeof(T)));
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

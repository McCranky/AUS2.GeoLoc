using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AUS2.GeoLoc.Structures.Utilities;
using AUS2.GeoLoc.Structures.FileManagers;

namespace AUS2.GeoLoc.Structures.Hashing
{
    /// <summary>
    /// A main class for extendable hashing which stores main logic and algorithms.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ExtendibleHashingDirectory<T> : IDisposable where T : IData<T>
    {
        public int _BFactor { get; private set; }
        public int _OverflowBFactor { get; private set; }
        private FileManager _fileManager;
        private OverflowFileManager<T> _overflowManager;
        private List<BlockInfo> _blocksInformations;
        private string _filePath;
        private int _hashDepth = 1;
        private T _emptyClass;

        public ExtendibleHashingDirectory(string filePath, int bFactor = -1, int overflowBFactor = -1, string mainFileDiskLetter = "C:\\", string overflowFileDiskLetter = "C:\\")
        {
            _emptyClass = (T)Activator.CreateInstance(typeof(T));
            _filePath = filePath;

            if (!TryLoad()) {
                Init(bFactor, overflowBFactor, mainFileDiskLetter, overflowFileDiskLetter);
            }
        }

        /// <summary>
        /// Files and containers initialisation
        /// </summary>
        private void Init(int bFactor, int overflowBFactor, string mainFileDiskLetter, string overflowFileDiskLetter)
        {
            UtilityOperations.GetDiskFreeSpace(mainFileDiskLetter, out var SectorsPerCluster, out var BytesPerSector, out _, out _);
            var ClusterSize = SectorsPerCluster * BytesPerSector;

            _BFactor = bFactor == -1 ? (ClusterSize - 8) / _emptyClass.GetSize() : bFactor;
            _OverflowBFactor = overflowBFactor == -1 ? (ClusterSize - 8) / _emptyClass.GetSize() : overflowBFactor;


            _blocksInformations = new List<BlockInfo>((int)Math.Pow(2, _hashDepth));

            var block = new Block<T>(_BFactor, _emptyClass.GetEmptyClass());
            var overflowBlock = new Block<T>(_OverflowBFactor, _emptyClass.GetEmptyClass());

            _fileManager = new FileManager(_filePath, block.GetSize());
            _overflowManager = new OverflowFileManager<T>("overflow." + _filePath, overflowBlock.GetSize(), _emptyClass, _OverflowBFactor);

            var address = _fileManager.GetFreeAddress();
            _blocksInformations.Add(new BlockInfo { Address = address, Depth = 1, Records = 0 });
            WriteBlock(address, block);

            address = _fileManager.GetFreeAddress();
            _blocksInformations.Add(new BlockInfo { Address = address, Depth = 1, Records = 0 });
            WriteBlock(address, block);
        }

        public void Save()
        {
            using (var stream = new FileStream("config." + _filePath, FileMode.OpenOrCreate)) {
                stream.Write(BitConverter.GetBytes(_BFactor));
                stream.Write(BitConverter.GetBytes(_OverflowBFactor));
                stream.Write(BitConverter.GetBytes(_hashDepth));
                stream.Write(BitConverter.GetBytes(_blocksInformations.Count));


                if (_blocksInformations.Count > 0) {
                    var distinctCount = _blocksInformations.Distinct().Count();
                    stream.Write(BitConverter.GetBytes(distinctCount));

                    var currentInfo = _blocksInformations[0];
                    var count = 0;

                    for (int i = 0; i < _blocksInformations.Count; i++) {
                        if (_blocksInformations[i].Address == currentInfo.Address) {
                            ++count;
                            if (i == _blocksInformations.Count - 1) {
                                stream.Write(BitConverter.GetBytes(count));
                                stream.Write(currentInfo.ToByteArray());
                                break;
                            }
                        } else {
                            stream.Write(BitConverter.GetBytes(count));
                            stream.Write(currentInfo.ToByteArray());

                            currentInfo = _blocksInformations[i];
                            count = 1;
                        }
                    }
                    stream.Write(BitConverter.GetBytes(count));
                    stream.Write(currentInfo.ToByteArray());
                }
                stream.Write(_fileManager.ToByteArray());
                stream.Write(_overflowManager.ToByteArray());
            }
        }

        public bool TryLoad()
        {
            try {
                var stream = new FileStream("config." + _filePath, FileMode.Open);
                var buffer = new byte[sizeof(int)];

                // citanie suboru
                stream.Read(buffer);
                _BFactor = BitConverter.ToInt32(buffer);
                var block = new Block<T>(_BFactor, _emptyClass.GetEmptyClass());
                _fileManager = new FileManager(_filePath, block.GetSize(), false);

                stream.Read(buffer);
                _OverflowBFactor = BitConverter.ToInt32(buffer);
                var overflowBlock = new Block<T>(_OverflowBFactor, _emptyClass.GetEmptyClass());
                _overflowManager = new OverflowFileManager<T>("overflow." + _filePath, overflowBlock.GetSize(), _emptyClass, _OverflowBFactor, false);

                stream.Read(buffer);
                _hashDepth = BitConverter.ToInt32(buffer);
                _blocksInformations = new List<BlockInfo>((int)Math.Pow(2, _hashDepth));

                stream.Read(buffer);
                var blocksInfoCount = BitConverter.ToInt32(buffer);

                if (blocksInfoCount > 0) {
                    stream.Read(buffer);
                    var distinctCount = BitConverter.ToInt32(buffer);

                    BlockInfo blockInfo;

                    for (int i = 0; i < distinctCount; i++) {
                        // zistom kolko opakovani
                        stream.Read(buffer);
                        var repetition = BitConverter.ToInt32(buffer);
                        // ziskam blockInfo
                        blockInfo = new BlockInfo();
                        buffer = new byte[blockInfo.GetSize()];
                        stream.Read(buffer);
                        blockInfo.FromByteArray(buffer);
                        // vlozim info potrebny pocet krat
                        while (repetition-- > 0) {
                            _blocksInformations.Add(blockInfo);
                        }

                        buffer = new byte[sizeof(int)];
                    }
                }

                stream.Read(buffer);
                var fileManagerAddresses = BitConverter.ToInt32(buffer);
                if (fileManagerAddresses > 0) {
                    buffer = new byte[sizeof(int) * fileManagerAddresses];
                    _fileManager.FromByteArray(buffer);
                }

                var overflowManagerSize = stream.Length - stream.Position;
                buffer = new byte[overflowManagerSize];
                stream.Read(buffer);
                _overflowManager.FromByteArray(buffer);

                stream.Close();

                return true;
            } catch (FileNotFoundException ex) {
                Console.WriteLine("File " + ex.FileName + " does not exist.");
                return false;
            }
        }

        public List<int> GetBlockAddresses()
        {
            return _blocksInformations.Select(info => info.Address).ToList();
        }

        public Tuple<BlockInfo, Block<T>> GetAllFromAddress(int address)
        {
            var info = _blocksInformations.First(info => info.Address == address);
            var block = ReadBlock(address);

            return new Tuple<BlockInfo, Block<T>>(info, block);
        }

        public List<Tuple<OverflowBlockInfo, Block<T>>> GetAllFromOverflow(int forAddress)
        {
            var chainData = new List<Tuple<OverflowBlockInfo, Block<T>>>();
            var overflowAddress = _blocksInformations.First(info => info.Address == forAddress).OverflowAddress;

            while (overflowAddress != int.MinValue) {
                _overflowManager.GetBlockAndInfo(overflowAddress, out var overflowBlock, out var info);
                overflowAddress = info.NextOwerflowAddress;

                chainData.Add(new Tuple<OverflowBlockInfo, Block<T>>(info, overflowBlock));
            }

            return chainData;
        }

        /// <summary>
        /// Get all free addreses in main file
        /// </summary>
        /// <returns></returns>
        public List<int> GetFreeAddressesMain()
        {
            return _fileManager.ShowFreeAddresses();
        }
        /// <summary>
        /// Get all free addresses in overflow file
        /// </summary>
        /// <returns></returns>
        public List<int> GetFreeAddressesOverflow()
        {
            return _overflowManager.ShowFreeAddresses();
        }

        public T Find(T data)
        {
            var block = GetBlock(data, out var blockInfo, out _);
            if (block == null) return default;

            var overflowAddress = blockInfo.OverflowAddress;

            while (true) {
                foreach (var record in block.Records) {
                    if (data.CustomEquals(record))
                        return record;
                }

                if (overflowAddress != int.MinValue) {
                    _overflowManager.GetBlockAndInfo(overflowAddress, out block, out var overflowInfo);
                    overflowAddress = overflowInfo.NextOwerflowAddress;
                } else {
                    break;
                }
            }
            
            return default;
        }

        public bool Update(T data)
        {
            var block = GetBlock(data, out var blockInfo, out _);
            if (block == null) return false;

            if (block.UpdateRecord(data)) {
                WriteBlock(blockInfo.Address, block);
                return true;
            }

            if (blockInfo.OverflowAddress != int.MinValue) {
                return _overflowManager.Update(ref data, blockInfo.OverflowAddress);
            }

            return false;
        }

        public bool Add(T data)
        {
            var block = new Block<T>(_BFactor, data.GetEmptyClass());
            while (true) {
                block = GetBlock(data, out var blockInfo, out _);
                if (block.FindRecord(data) != null) {
                    return false;
                }

                if (block.ValidCount == _BFactor) { // block is full
                    if (block.BlockDepth == _hashDepth) {
                        if (!DoubleAddressSize(data.GetHash().Count)) {
                            // adresar sa už viac nemohol rozšíriť
                            return _overflowManager.Add(ref data, ref blockInfo);
                        }
                    }

                    SplitOperation(ref data, ref block, blockInfo);

                } else {
                    block.AddRecord(data);
                    WriteBlock(blockInfo.Address, block);
                    blockInfo.Records += 1;
                    break;
                }
            }

            return true;
        }

        public bool Delete(T data)
        {
            // operacia prebieha cyklicky ako vkladanie
            var canMerge = true;
            var block = new Block<T>(_BFactor, data.GetEmptyClass());
            block = GetBlock(data, out var blockInfo, out var blockIndex);

            if (block == null) return false;
            // 1 najdi blok a vymaž prvok v ňom
            var result = block.DeleteRecord(data);
            if (result == null ) {
                if (blockInfo.OverflowAddress != int.MinValue) {
                    // zmaž v preplnovacom subore
                    _overflowManager.Remove(ref data, ref blockInfo);
                    if (blockInfo.OverflowAddress != int.MinValue) {
                        return true;
                    }
                } else {
                    return false;
                }                
            } else {
                if (blockInfo.OverflowAddress != int.MinValue) {
                    // presun zaznam z preplnovaceho suboru a teda pocet zaznamov nemusime menit
                    _overflowManager.MoveRecordToBlock(ref block, ref blockInfo);

                    if (blockInfo.OverflowAddress != int.MinValue) {
                        // este mam preplnovacie data tak sa nemozem spojit
                        canMerge = false;
                    }
                } else {
                    --blockInfo.Records;
                }

                WriteBlock(blockInfo.Address, block);
            }
            
            while (canMerge) {
                if (TryMergeOperation(ref block, ref blockIndex, ref blockInfo, out var depth)) {

                    if (_hashDepth == depth) {
                        var countOfEqualDepth = _blocksInformations
                                            .Where(i => i.Depth == depth)
                                            .Count();
                        if (countOfEqualDepth == 0) {
                            if (HalveAddressSize()) {
                                SetBlockInfoAndIndex(data, ref blockInfo, ref blockIndex);
                            }
                        }
                    }
                } else {
                    break;
                }
            }
            return true;
        }

        private bool TryMergeOperation(ref Block<T> block, ref int blockIndex, ref BlockInfo blockInfo, out int depth)
        {
            depth = -1;
            if (_hashDepth == 1 ) return false;
            var neighborInfo = GetNeighborInformations(blockIndex, block.BlockDepth, out _);
            if (neighborInfo.Depth != blockInfo.Depth ||
                neighborInfo.OverflowAddress != int.MinValue && blockInfo.Records != 0 ||
                blockInfo.OverflowAddress != int.MinValue && neighborInfo.Records != 0 ||
                neighborInfo.Records + blockInfo.Records > _BFactor) return false;

            // ak ano, tak sa presunu do jedneho 
            var neighborBlock = ReadBlock(neighborInfo.Address);
            foreach (var record in neighborBlock.Records) {
                block.AddRecord(record);
            }

            depth = block.BlockDepth;
            --block.BlockDepth;
            --blockInfo.Depth;
            blockInfo.Records = block.ValidCount;
            
            var higherAddress = blockInfo.Address > neighborInfo.Address ? blockInfo.Address : neighborInfo.Address;
            var lowerAddress = blockInfo.Address < neighborInfo.Address ? blockInfo.Address : neighborInfo.Address;
            blockInfo.Address = lowerAddress;
            _fileManager.FreeAddress(higherAddress);

            WriteBlock(blockInfo.Address, block);
            UpdateAddresses(neighborInfo, blockInfo);

            return true;
        }
        /// <summary>
        /// Slice dictionary size to half
        /// </summary>
        /// <returns></returns>
        private bool HalveAddressSize()
        {
            --_hashDepth;
            var step = FindSmallestSequene();
            if (step <= 1) return false;
            var newAddresses = new List<BlockInfo>((int)Math.Pow(2, _hashDepth));
            for (int i = 0; i < (int)Math.Pow(2, _hashDepth); i++) {
                newAddresses.Add(_blocksInformations[i * step]);
            }
            _blocksInformations = newAddresses;
            return true;
        }
        /// <summary>
        /// Finds smallest sequence.. result for 0 0 1 1 1 1 is 2
        /// </summary>
        /// <returns></returns>
        private int FindSmallestSequene()
        {
            var lastInfo = _blocksInformations[0];
            var count = 0;
            var min = int.MaxValue;
            foreach (var info in _blocksInformations) {
                if (info != lastInfo) {
                    if (count < min)
                        min = count;
                    lastInfo = info;
                    count = 0;
                }
                ++count;
            }
            return min;
        }
        /// <summary>
        /// Rewrites addresses
        /// </summary>
        /// <param name="fromInfo"></param>
        /// <param name="toInfo"></param>
        private void UpdateAddresses(BlockInfo fromInfo, BlockInfo toInfo)
        {
            var index = _blocksInformations.IndexOf(fromInfo);
            while (index <= _blocksInformations.Count - 1 && _blocksInformations[index] == fromInfo) {
                _blocksInformations[index++] = toInfo;
            }
        }
        /// <summary>
        /// Enlarges dictionary by 100%
        /// </summary>
        /// <param name="hashSize"></param>
        /// <returns></returns>
        private bool DoubleAddressSize(int hashSize)
        {
            if (_hashDepth == hashSize) return false;

            ++_hashDepth;
            var newInformations = new List<BlockInfo>((int)Math.Pow(2, _hashDepth));
            for (int i = 0; i < (int)Math.Pow(2, _hashDepth - 1); i++) {
                newInformations.Add(_blocksInformations[i]);
                newInformations.Add(_blocksInformations[i]);
            }
            _blocksInformations = newInformations;
            return true;
        }
        /// <summary>
        /// Used in split opeation when higher half of addresses are pointing to new block
        /// </summary>
        /// <param name="fromBlockInfo"></param>
        /// <param name="toBlockInfo"></param>
        private void UpdateNewAddresses(BlockInfo fromBlockInfo, BlockInfo toBlockInfo)
        {
            var firstIndexOfAddress = _blocksInformations.IndexOf(fromBlockInfo);
            var lastIndexOfAddress = _blocksInformations.LastIndexOf(fromBlockInfo);
            var splitIndex = (lastIndexOfAddress - firstIndexOfAddress + 1) / 2;
            for (int i = firstIndexOfAddress + splitIndex; i <= lastIndexOfAddress; i++) {
                _blocksInformations[i] = toBlockInfo;
            }
        }
        /// <summary>
        /// Splits one block to to with +1 depth
        /// </summary>
        /// <param name="data"></param>
        /// <param name="block"></param>
        /// <param name="blockInfo"></param>
        private void SplitOperation(ref T data, ref Block<T> block, BlockInfo blockInfo)
        {
            var block0 = new Block<T>(_BFactor, data.GetEmptyClass()); // blok s bin 0 na konci (parne čislo)
            var block1 = new Block<T>(_BFactor, data.GetEmptyClass()); // blok s bin 1 na konci (neparne čislo)
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

            //aktualizovanie
            var address = _fileManager.GetFreeAddress();

            blockInfo.Records = block0.ValidCount;
            blockInfo.Depth = newDepth;
            var newBlockInfo = new BlockInfo { Address = address, Depth = newDepth, Records = block1.ValidCount };

            UpdateNewAddresses(blockInfo, newBlockInfo);

            //zapisanie novych blokov do suboru a aktualizovanie informacii o blokoch
            // parny blok (menšie čislo) na pôvodnu adresu
            WriteBlock(blockInfo.Address, block0);

            // neparny blok (väčšie čislo) na novu adresu
            WriteBlock(newBlockInfo.Address, block1);
        }

        private int GetIndexFromHash(BitArray hash)
        {
            //1. ziskaj prvých D bitov z hashu
            var bits = BitsOperations.GetFirstBits(hash, _hashDepth);
            BitsOperations.ReverseBits(ref bits);
            //2. preved z bin na dec
            return BitsOperations.GetIntFromBitArray(
                bits
                );
        }

        private Block<T> GetBlock(T data, out BlockInfo info, out int index)
        {
            info = new BlockInfo();
            //1. ziskaj index
            index = GetIndexFromHash(data.GetHash());
            if (index >= _blocksInformations.Count) return null;
            //2.2 pristup na dany index do pola adries blokov
            info = _blocksInformations[index];
            //2.3 spristupni dany blok
            return ReadBlock(info.Address);
        }

        private void SetBlockInfoAndIndex(T data, ref BlockInfo info, ref int index)
        {
            index = GetIndexFromHash(data.GetHash());
            info = _blocksInformations[index];
        }

        private BlockInfo GetNeighborInformations(int blockIndex, int blockDepth, out int neighborIndex)
        {
            neighborIndex = blockIndex ^ 1 << _hashDepth - blockDepth;
            return GetBlockInformations(neighborIndex);
        }

        private BlockInfo GetBlockInformations(int blockIndex)
        {
            return _blocksInformations[blockIndex];
        }

        private void WriteBlock(int address, Block<T> block)
        {
            _fileManager.WriteBytes(address, block.ToByteArray());
        }

        private Block<T> ReadBlock(int address)
        {
            var block = new Block<T>(_BFactor, (T)Activator.CreateInstance(typeof(T)));
            var blockBytes = new byte[block.GetSize()];
            _fileManager.ReadBytes(address, ref blockBytes);
            block.FromByteArray(blockBytes);
            return block;
        }

        public void Dispose()
        {
            Save();
            _fileManager.Dispose();
        }
    }
}

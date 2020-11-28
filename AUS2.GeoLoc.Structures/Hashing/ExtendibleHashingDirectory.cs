using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AUS2.GeoLoc.Structures.Utilities;
using AUS2.GeoLoc.Structures.FileManagers;

namespace AUS2.GeoLoc.Structures.Hashing
{
    public class ExtendibleHashingDirectory<T> : IDisposable where T : IData<T>
    {
        public int BFactor { get; private set; }
        private FileManager fileManager;
        private OverflowFileManager<T> overflowManager;
        private List<BlockInfo> _blocksInformations;
        private string _filePath;
        private int _hashDepth = 1;

        public ExtendibleHashingDirectory(string filePath, int bFactor = -1)
        {
            if (bFactor == -1) {
                UtilityOperations.GetDiskFreeSpace("C:\\", out var SectorsPerCluster, out var BytesPerSector, out var NumberOfFreeClusters, out var TotalNumberOfClusters);
                var ClusterSize = SectorsPerCluster * BytesPerSector;
                var emptyObject = (T)Activator.CreateInstance(typeof(T));
                BFactor = (ClusterSize - 8) / emptyObject.GetSize();
            } else {
                BFactor = bFactor;
            }
            
            _filePath = filePath;
            if (!TryLoad()) {
                Init();
            }
        }

        private void Init()
        {
            _blocksInformations = new List<BlockInfo>((int)Math.Pow(2, _hashDepth));

            var classInstance = (T)Activator.CreateInstance(typeof(T));
            var block = new Block<T>(BFactor, classInstance.GetEmptyClass());

            fileManager = new FileManager(_filePath, block.GetSize());
            overflowManager = new OverflowFileManager<T>("overflow." + _filePath, block.GetSize());

            var address = fileManager.GetFreeAddress();
            _blocksInformations.Add(new BlockInfo { Address = address, Depth = 1, Records = 0 });
            WriteBlock(address, block);

            address = fileManager.GetFreeAddress();
            _blocksInformations.Add(new BlockInfo { Address = address, Depth = 1, Records = 0 });
            WriteBlock(address, block);
        }

        public void Save()
        {
            var stream = new FileStream("config." + _filePath, FileMode.OpenOrCreate);
            //stream.Seek(stream.Length, SeekOrigin.Begin);

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
            stream.Write(fileManager.ToByteArray());
            stream.Write(overflowManager.ToByteArray());

            stream.Close();
        }

        public bool TryLoad()
        {
            try {
                var stream = new FileStream("config." + _filePath, FileMode.Open);
                var buffer = new byte[sizeof(int)];

                // inicializacia managerov
                var classInstance = (T)Activator.CreateInstance(typeof(T));
                var block = new Block<T>(BFactor, classInstance.GetEmptyClass());

                fileManager = new FileManager(_filePath, block.GetSize());
                overflowManager = new OverflowFileManager<T>("overflow." + _filePath, block.GetSize());
                _blocksInformations = new List<BlockInfo>((int)Math.Pow(2, _hashDepth));


                // citanie suboru
                stream.Read(buffer);
                _hashDepth = BitConverter.ToInt32(buffer);

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
                    fileManager.FromByteArray(buffer);
                }

                var overflowManagerSize = stream.Length - stream.Position;
                buffer = new byte[overflowManagerSize];
                stream.Read(buffer);
                overflowManager.FromByteArray(buffer);

                return true;
            } catch (FileNotFoundException ex) {
                Console.WriteLine("File " + ex.FileName + " does not exist.");
                return false;
            }
        }

        public List<int> GetBlockAddresses()
        {
            return _blocksInformations.Distinct().Select(info => info.Address).ToList();
        }

        public T Find(T data)
        {
            var block = GetBlock(data, out var blockInfo, out _);
            if (block == null) return default;

            while (true) {
                foreach (var record in block.Records) {
                    if (data.CustomEquals(record))
                        return record;
                }

                if (blockInfo.OverflowAddress != int.MinValue) {
                    overflowManager.GetBlockAndInfo(blockInfo.OverflowAddress, ref block, ref blockInfo);
                } else {
                    break;
                }
            }
            
            return default;
        }

        public bool Add(T data)
        {
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            while (true) {
                block = GetBlock(data, out var blockInfo, out _);
                if (block.ValidCount == BFactor) { // block is full
                    if (block.BlockDepth == _hashDepth) {
                        if (!DoubleAddressSize(data.GetHash().Count)) {
                            // adresar sa už viac nemohol rozšíriť
                            overflowManager.Add(ref data, ref block, ref blockInfo);
                            return true;
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
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            var helpBlock = new Block<T>(BFactor, data.GetEmptyClass());
            block = GetBlock(data, out var blockInfo, out var blockIndex);

            if (block == null) return false;
            // 1 najdi blok a vymaž prvok v ňom
            var result = block.DeleteRecord(data);
            if (result == null ) {
                if (blockInfo.OverflowAddress != int.MinValue) {
                    // zmaž v preplnovacom subore
                    overflowManager.Remove(ref data, ref block, ref blockInfo, ref helpBlock);
                    if (blockInfo.OverflowAddress != int.MinValue) {
                        return true;
                    }
                } else {
                    return false;
                }                
            } else {
                if (blockInfo.OverflowAddress != int.MinValue) {
                    // presun zaznam z preplnovaceho suboru a teda pocet zaznamov nemusime menit
                    
                    overflowManager.MoveRecordToBlock(ref block, ref blockInfo, ref helpBlock);
                    if (blockInfo.OverflowAddress != int.MinValue) {
                        // este mam preplnovacie data tak sa nemozem spojit
                        canMerge = false; // TODO ale mozem iba s prazdnym?
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
                        if (countOfEqualDepth == 1) {
                            if (HalveAddressSize()) {
                                blockInfo = _blocksInformations[GetIndexFromHash(data.GetHash())];
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
            if (_hashDepth == 1) return false;
            var neighborInfo = GetNeighborInformations(blockIndex, block.BlockDepth, out var neighborIndex);
            if (//neighborInfo.Address == blockInfo.Address || // TODO ako to je možne?
                neighborInfo.Depth == blockInfo.Depth ||
                neighborInfo.OverflowAddress != int.MinValue && blockInfo.Records != 0 ||
                blockInfo.OverflowAddress != int.MinValue && neighborInfo.Records != 0 ||
                neighborInfo.Records + blockInfo.Records > BFactor) return false;

            // ak ano, tak sa presunu do jedneho 
            var neighborBlock = ReadBlock(neighborInfo.Address);
            foreach (var record in neighborBlock.Records) {
                block.AddRecord(record);
            }
            depth = block.BlockDepth;
            --block.BlockDepth;
            --blockInfo.Depth;
            blockInfo.Records = block.ValidCount;
            blockInfo.Address = blockInfo.Address < neighborInfo.Address ? blockInfo.Address : neighborInfo.Address;
            WriteBlock(blockInfo.Address, block);

            UpdateAddresses(neighborInfo, blockInfo);

            return true;
        }

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

        private void UpdateAddresses(BlockInfo fromInfo, BlockInfo toInfo)
        {
            var index = _blocksInformations.IndexOf(fromInfo);
            while (index <= _blocksInformations.Count - 1 && _blocksInformations[index] == fromInfo) {
                _blocksInformations[index++] = toInfo;
            }
        }

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

        private void UpdateNewAddresses(BlockInfo fromBlockInfo, BlockInfo toBlockInfo)
        {
            var firstIndexOfAddress = _blocksInformations.IndexOf(fromBlockInfo);
            var lastIndexOfAddress = _blocksInformations.LastIndexOf(fromBlockInfo);
            var splitIndex = (lastIndexOfAddress - firstIndexOfAddress + 1) / 2;
            for (int i = firstIndexOfAddress + splitIndex; i <= lastIndexOfAddress; i++) {
                _blocksInformations[i] = toBlockInfo;
            }
        }

        private void SplitOperation(ref T data, ref Block<T> block, BlockInfo blockInfo)
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

            //aktualizovanie
            var address = fileManager.GetFreeAddress();

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

        private BlockInfo GetNeighborInformations(int blockIndex, int blockDepth, out int neighborIndex)
        {
            neighborIndex = blockIndex ^ 1 << _hashDepth - blockDepth;
            return GetBlockInformations(neighborIndex);
        }

        private BlockInfo GetBlockInformations(int blockIndex)
        {
            return _blocksInformations[blockIndex];//.FirstOrDefault(info => info.Address == blockAddress);
        }

        private void WriteBlock(int address, Block<T> block)
        {
            fileManager.WriteBytes(address, block.ToByteArray());
        }

        private Block<T> ReadBlock(int address)
        {
            var block = new Block<T>(BFactor, (T)Activator.CreateInstance(typeof(T)));
            var blockBytes = new byte[block.GetSize()];
            fileManager.ReadBytes(address, ref blockBytes);
            block.FromByteArray(blockBytes);
            return block;
        }

        public void Dispose()
        {
            fileManager.Dispose();
        }
    }
}

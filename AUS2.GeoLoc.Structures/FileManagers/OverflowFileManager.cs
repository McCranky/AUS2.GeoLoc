using AUS2.GeoLoc.Structures.Hashing;
using AUS2.GeoLoc.Structures.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AUS2.GeoLoc.Structures.FileManagers
{
    public class OverflowFileManager<T> : FileManager, IRecord where T : IData<T>
    {
        private SortedTable<int, OverflowBlockInfo> _blocksInfoTable;

        public OverflowFileManager(string filePath, int blockSize) : base(filePath, blockSize)
        {
            _blocksInfoTable = new SortedTable<int, OverflowBlockInfo>();
        }

        public void GetBlockAndInfo(int address, ref Block<T> block, out OverflowBlockInfo blockInfo)
        {
            ReadBlock(address, ref block);
            blockInfo = _blocksInfoTable[address].Value;
        }

        public OverflowBlockInfo GetOverflowInfo(int address)
        {
            return _blocksInfoTable[address].Value;
        }

        public void Add(ref T data, ref Block<T> block, ref BlockInfo blockInfo)
        {
            if (blockInfo.OverflowAddress != int.MinValue) {
                AddToExistingBlock(ref data, ref block, ref blockInfo);
            } else {
                AddNewBlock(ref data, ref block, ref blockInfo);
            }
        }

        private void AddToExistingBlock(ref T data, ref Block<T> block, ref BlockInfo blockInfo)
        {
            var foundPlace = false;
            var nextAddress = blockInfo.OverflowAddress;
            while (!foundPlace) {
                var info = _blocksInfoTable[nextAddress].Value;
                if (info.Records < block.BFactor) {
                    // zapis ho sem
                    ReadBlock(nextAddress, ref block);
                    block.AddRecord(data);
                    ++info.Records;
                    WriteBytes(nextAddress, block.ToByteArray());
                    foundPlace = true;
                } else {
                    if (info.NextOwerflowAddress != int.MinValue) {
                        nextAddress = info.NextOwerflowAddress;
                    } else {
                        AddNewOverflowBlock(ref data, ref block, ref info);
                        foundPlace = true;
                    }
                }
            }
        }

        private void AddNewOverflowBlock(ref T data, ref Block<T> block, ref OverflowBlockInfo blockInfo)
        {
            var address = GetFreeAddress();
            block.ValidCount = 0;
            block.AddRecord(data);
            WriteBytes(address, block.ToByteArray());
            blockInfo.NextOwerflowAddress = address;

            _blocksInfoTable.Add(address, new OverflowBlockInfo());
        }

        private void AddNewBlock(ref T data, ref Block<T> block, ref BlockInfo blockInfo)
        {
            var address = GetFreeAddress();
            block.ValidCount = 0;
            block.AddRecord(data);
            WriteBytes(address, block.ToByteArray());
            blockInfo.OverflowAddress = address;

            _blocksInfoTable.Add(address, new OverflowBlockInfo());
        }

        public bool Update(int overflowAddress, ref Block<T> block, ref T data)
        {
            while (overflowAddress != int.MinValue) {
                ReadBlock(overflowAddress, ref block);
                if (block.UpdateRecord(data)) {
                    WriteBytes(overflowAddress, block.ToByteArray());
                    return true;
                }

                overflowAddress = _blocksInfoTable[overflowAddress].Value.NextOwerflowAddress;
            }
            return false;
        }

        private void ReadBlock(int address, ref Block<T> block)
        {
            var blockBytes = new byte[block.GetSize()];
            ReadBytes(address, ref blockBytes);
            block.FromByteArray(blockBytes);
        }

        internal void Remove(ref T data, ref Block<T> block, ref BlockInfo blockInfo, ref Block<T> helpBlock)
        {
            // aktualizovat blockInfo ak sa vyprazdnil blok na ktory ukazuje
            var address = blockInfo.OverflowAddress;
            OverflowBlockInfo beforeInfo = null;

            var canContinue = true;
            while (canContinue) {
                ReadBlock(address, ref helpBlock);
                var info = _blocksInfoTable[address].Value;

                if (helpBlock.DeleteRecord(data) == null) {
                    if (info.NextOwerflowAddress != int.MinValue) {
                        beforeInfo = info;
                        address = info.NextOwerflowAddress;
                    } else {
                        // zaznam nebol najdeny
                        canContinue = false;
                    }
                } else {
                    --info.Records;
                    if (info.Records == 0) {
                        // blok ostal prazdny tak prenastavime povodnemu bloku novu adresu zretazenia
                        // odstranime už nepotrebnu informaciu
                        // uvolnime adresu
                        if (beforeInfo != null) {
                            beforeInfo.NextOwerflowAddress = info.NextOwerflowAddress;
                        } else {
                            blockInfo.OverflowAddress = info.NextOwerflowAddress;
                        }
                        _blocksInfoTable.Remove(address);
                        FreeAddress(address);
                    } else {
                        if (!TryShake(blockInfo.OverflowAddress, address, ref helpBlock, out var newOverflowAddress)) {
                            WriteBytes(address, helpBlock.ToByteArray());
                        } else {
                            // TODO uvolnenie adresy a prepojenie blokov
                            if (newOverflowAddress >= 0) {
                                blockInfo.OverflowAddress = newOverflowAddress;
                            }
                        }
                    }
                    canContinue = false;
                }
            }
        }

        internal void MoveRecordToBlock(ref Block<T> block, ref BlockInfo blockInfo, ref Block<T> helpBlock)
        {
            // najdeme najväčšiu adresu a z nej zobereme, aby sme uvolnovali miesto z konca
            var address = blockInfo.OverflowAddress;
            var beforeAddress = -1;
            var maxAddress = -1;

            while (true) {
                var key = _blocksInfoTable[address].Value.NextOwerflowAddress;
                if (key == int.MinValue) {
                    break;
                }
                if (key > maxAddress) {
                    maxAddress = key;
                    beforeAddress = address;
                }

                address = key;
            }
            if (maxAddress == -1) {
                maxAddress = address;
            }

            var helpBlockInfo = _blocksInfoTable[maxAddress].Value;
            ReadBlock(maxAddress, ref helpBlock);
            var record = helpBlock.Records[0];
            helpBlock.DeleteRecord(record);
            --helpBlockInfo.Records;

            if (helpBlockInfo.Records == 0) {
                if (beforeAddress != -1) {
                    // nejaka adresa ukazuje na tuto tka ju musime aktualizovat ale povodneho bloku informaciu nemusime aktualizovat
                    _blocksInfoTable[beforeAddress].Value.NextOwerflowAddress = helpBlockInfo.NextOwerflowAddress;
                } else {
                    blockInfo.OverflowAddress = helpBlockInfo.NextOwerflowAddress;
                }
                _blocksInfoTable.Remove(maxAddress);
                FreeAddress(maxAddress);
            } else {
                if (!TryShake(blockInfo.OverflowAddress, maxAddress, ref helpBlock, out var newOverflowAddress)) {
                    WriteBytes(maxAddress, helpBlock.ToByteArray());
                } else {
                    // TODO uvolnenie adresy a prepojenie blokov
                    if (newOverflowAddress >= 0) {
                        blockInfo.OverflowAddress = newOverflowAddress;
                    }
                }
            }

            block.AddRecord(record);
        }

        private bool TryShake(int firstAddressOfSequence, int blockToShakeAddress, ref Block<T> blockToShake, out int newOverflowAddress)
        {
            newOverflowAddress = -1;
            var address = firstAddressOfSequence;
            var shakeToAddress = -1;
            OverflowBlockInfo info;

            while (address != int.MinValue) {
                info = _blocksInfoTable[address].Value;

                if (address != blockToShakeAddress &&
                    info.Records + blockToShake.ValidCount <= blockToShake.BFactor) 
                {
                    shakeToAddress = address;
                    break;
                }

                address = info.NextOwerflowAddress;
            }

            if (shakeToAddress != -1) {
                var shakeBlock = new Block<T>(blockToShake.BFactor, blockToShake.Records[0].GetEmptyClass());
                var shakeBlockInfo = _blocksInfoTable[shakeToAddress].Value;
                ReadBlock(shakeToAddress, ref shakeBlock);

                foreach (var record in blockToShake.Records) {
                    shakeBlock.AddRecord(record);
                }
                shakeBlockInfo.Records = shakeBlock.ValidCount;

                WriteBytes(shakeToAddress, shakeBlock.ToByteArray());

                if (shakeBlockInfo.NextOwerflowAddress == blockToShakeAddress) {
                    // ak blok z ktoreho sa striasalo je nasledovnikom bloku do ktoreho sa striasa, tak mu priradime jeho nasledovnika
                    shakeBlockInfo.NextOwerflowAddress = _blocksInfoTable[blockToShakeAddress].Value.NextOwerflowAddress;
                } else if (blockToShakeAddress == firstAddressOfSequence) { 
                    // ak sa ztriasal prvy blok zo sekvencie tak povodnemu bloku musime priradiť jeho nasledovnika
                    newOverflowAddress = _blocksInfoTable[blockToShakeAddress].Value.NextOwerflowAddress;
                } else {
                    // inak prepojime zreťazenie
                    // musime najsť blok, ktory ukazuje na utriasany a nasatviť mu spravneho nasledovnika
                    var blockBeforeToShakeBlockInfo = _blocksInfoTable.Items.First(tableItem => tableItem.Value.NextOwerflowAddress == blockToShakeAddress);
                    blockBeforeToShakeBlockInfo.Value.NextOwerflowAddress = _blocksInfoTable[blockToShakeAddress].Value.NextOwerflowAddress;
                }

                _blocksInfoTable.Remove(blockToShakeAddress);
                FreeAddress(blockToShakeAddress);

                return true;
            } else {
                return false;
            }
        }

        public override int GetSize()
        {
            var result = sizeof(int) * (1 + _blocksInfoTable.Count);
            result += ((OverflowBlockInfo)Activator.CreateInstance(typeof(OverflowBlockInfo))).GetSize() * _blocksInfoTable.Count;
            return result + base.GetSize();
        }

        public override byte[] ToByteArray()
        {
            byte[] myArray;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(_blocksInfoTable.Count));

                for (int i = 0; i < _blocksInfoTable.Count; i++) {
                    ms.Write(BitConverter.GetBytes(_blocksInfoTable.Items[i].Key));
                    ms.Write(_blocksInfoTable.Items[i].Value.ToByteArray());
                }
                myArray = ms.ToArray();
            }
            var baseArray = base.ToByteArray();

            var result = new byte[myArray.Length + baseArray.Length];
            Buffer.BlockCopy(myArray, 0, result, 0, myArray.Length);
            Buffer.BlockCopy(baseArray, 0, result, myArray.Length, baseArray.Length);

            return result;
        }

        public override void FromByteArray(byte[] array)
        {
            using (var ms = new MemoryStream(array)) {
                var buffer = new byte[sizeof(int)];
                ms.Read(buffer);
                var infoCount = BitConverter.ToInt32(buffer);

                if (infoCount > 0) {
                    OverflowBlockInfo info;

                    for (int i = 0; i < infoCount; i++) {
                        //kluc
                        ms.Read(buffer);
                        var key = BitConverter.ToInt32(buffer);
                        //info
                        info = new OverflowBlockInfo();
                        buffer = new byte[info.GetSize()];
                        ms.Read(buffer);
                        info.FromByteArray(buffer);

                        _blocksInfoTable.Add(key, info);

                        buffer = new byte[sizeof(int)];
                    }
                }

                ms.Read(buffer);
                var fileManagerAddresses = BitConverter.ToInt32(buffer);
                if (fileManagerAddresses > 0) {
                    buffer = new byte[sizeof(int) * fileManagerAddresses];
                    base.FromByteArray(buffer);
                }
            }
        }
    }
}

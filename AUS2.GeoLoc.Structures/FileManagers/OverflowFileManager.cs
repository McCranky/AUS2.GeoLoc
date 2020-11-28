using AUS2.GeoLoc.Structures.Hashing;
using AUS2.GeoLoc.Structures.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Structures.FileManagers
{
    public class OverflowFileManager<T> : FileManager, IRecord where T : IData<T>
    {
        private SortedTable<int, BlockInfo> _blocksInfoTable;

        public OverflowFileManager(string filePath, int blockSize) : base(filePath, blockSize)
        {
            _blocksInfoTable = new SortedTable<int, BlockInfo>();
        }

        public void GetBlockAndInfo(int address, ref Block<T> block, ref BlockInfo blockInfo)
        {
            ReadBlock(address, ref block);
            blockInfo = _blocksInfoTable[address].Value;
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
                    if (info.OverflowAddress != int.MinValue) {
                        nextAddress = info.OverflowAddress;
                    } else {
                        AddNewBlock(ref data, ref block, ref info);
                    }
                }
            }
        }

        private void AddNewBlock(ref T data, ref Block<T> block, ref BlockInfo blockInfo)
        {
            var address = GetFreeAddress();
            block.ValidCount = 0;
            block.AddRecord(data);
            WriteBytes(address, block.ToByteArray());
            blockInfo.OverflowAddress = address;

            _blocksInfoTable.Add(address, new BlockInfo {
                Address = address,
                Depth = block.BlockDepth,
                Records = 1
            });
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

            var canContinue = true;
            while (canContinue) {
                ReadBlock(address, ref helpBlock);
                var info = _blocksInfoTable[address].Value;

                if (helpBlock.DeleteRecord(data) == null) {
                    if (info.OverflowAddress != int.MinValue) {
                        address = info.OverflowAddress;
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
                        blockInfo.OverflowAddress = info.OverflowAddress;
                        _blocksInfoTable.Remove(address);
                        FreeAddress(address);
                    }
                    WriteBytes(address, helpBlock.ToByteArray());
                    canContinue = false;
                }
            }
        }

        internal void MoveRecordToBlock(ref Block<T> block, ref BlockInfo blockInfo, ref Block<T> helpBlock)
        {
            // najdeme najväčšiu adresu a z nej zobereme, aby sme uvolnovali miesto z konca
            var address = blockInfo.OverflowAddress;
            var beforeAddress = -1;

            while (true) {
                var key = _blocksInfoTable[address].Value.OverflowAddress;
                if (key == int.MinValue) {
                    break;
                } else if (key > address) {
                    beforeAddress = address;
                    address = key;
                }
            }

            var helpBlockInfo = _blocksInfoTable[address].Value;
            ReadBlock(address, ref helpBlock);
            var record = helpBlock.Records[0];
            helpBlock.DeleteRecord(record);
            --helpBlockInfo.Records;
            WriteBytes(address, helpBlock.ToByteArray());

            block.AddRecord(record);

            if (helpBlockInfo.Records == 0) {
                if (beforeAddress != -1) {
                    // nejaka adresa ukazuje na tuto tka ju musime aktualizovat ale povodneho bloku informaciu nemusime aktualizovat
                    _blocksInfoTable[beforeAddress].Value.OverflowAddress = helpBlockInfo.OverflowAddress;
                } else {
                    blockInfo.OverflowAddress = helpBlockInfo.OverflowAddress;
                }
                _blocksInfoTable.Remove(address);
                FreeAddress(address);
            }
        }

        public override int GetSize()
        {
            var result = sizeof(int) * (1 + _blocksInfoTable.Count);
            result += ((BlockInfo)Activator.CreateInstance(typeof(BlockInfo))).GetSize() * _blocksInfoTable.Count;
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
                    BlockInfo info;

                    for (int i = 0; i < infoCount; i++) {
                        //kluc
                        ms.Read(buffer);
                        var key = BitConverter.ToInt32(buffer);
                        //info
                        info = new BlockInfo();
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

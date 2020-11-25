using AUS2.GeoLoc.Structures.Tables;
using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.Structures.Hashing
{
    public class OverflowFileManager<T> : FileManager where T : IData<T>
    {
        private SortedTable<int, BlockInformations> _blocksInfoTable;

        public OverflowFileManager(string filePath, int blockSize) : base(filePath, blockSize)
        {
            _blocksInfoTable = new SortedTable<int, BlockInformations>();
        }

        public void GetBlockAndInfo(int address, ref Block<T> block, ref BlockInformations blockInfo)
        {
            ReadBlock(address, ref block);
            blockInfo = _blocksInfoTable[address].Value;
        }

        public void Add(ref T data, ref Block<T> block, ref BlockInformations blockInfo)
        {
            if (blockInfo.OverflowAddress != int.MinValue) {
                AddToExistingBlock(ref data, ref block, ref blockInfo);
            } else {
                AddNewBlock(ref data, ref block, ref blockInfo);
            }
        }

        private void AddToExistingBlock(ref T data, ref Block<T> block, ref BlockInformations blockInfo)
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

        private void AddNewBlock(ref T data, ref Block<T> block, ref BlockInformations blockInfo)
        {
            var address = GetFreeAddress();
            block.ValidCount = 0;
            block.AddRecord(data);
            WriteBytes(address, block.ToByteArray());
            blockInfo.OverflowAddress = address;
            
            _blocksInfoTable.Add(address, new BlockInformations {
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

        internal void Remove(ref T data, ref Block<T> block, ref BlockInformations blockInfo, ref Block<T> helpBlock)
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
                    if (info.Records == 0) {
                        // blok ostal prazdny tak prenastavime povodnemu bloku novu adresu zretazenia
                        // odstranime už nepotrebnu informaciu
                        // uvolnime adresu
                        blockInfo.OverflowAddress = info.OverflowAddress;
                        _blocksInfoTable.Remove(address);
                        FreeAddress(address);
                    }
                    canContinue = false;
                }
            }
        }

        internal void MoveRecordToBlock(ref Block<T> block, ref BlockInformations blockInfo, ref Block<T> helpBlock)
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
    }
}

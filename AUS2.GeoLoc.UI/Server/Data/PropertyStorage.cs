using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AUS2.GeoLoc.Structures.Hashing;
using AUS2.GeoLoc.UI.Shared;

namespace AUS2.GeoLoc.UI.Server.Data
{
    public class PropertyStorage
    {
        private ExtendibleHashingDirectory<Property> _context;
        private int _idSequence = 0;
        private int _count = 0;
        public PropertyStorage()
        {
            _context = new ExtendibleHashingDirectory<Property>("properties.dat", 3);
        }

        public bool CanSeed => true;//_count == 0;

        internal void Save()
        {
            _context.Save();
        }

        public void SeedData(int count)
        {
            //if (!CanSeed) return;
            //_idSequence = 0;

            for (int i = 0; i < count; i++) {
                var num = _idSequence++;
                _context.Add(new Property { Id = num, RegisterNumber = num, Description = $"Property {num}" });
                ++_count;
            }
        }

        public Property GetPropertyById(int id)
        {
            return _context.Find(new Property { Id = id });
        }

        public bool UpdateProperty(Property property)
        {
            return _context.Update(property);
        }

        public bool DeleteProperty(int id)
        {
            if (_context.Delete(new Property { Id = id })) {
                --_count;
                return true;
            }
            return false;
        }

        public void AddProperty(ref Property property)
        {
            property.Id = _idSequence++;
            //property = new Property { Id = _idSequence++, Description = property.Description, RegisterNumber = property.RegisterNumber };
            if (_context.Add(property)) {
                ++_count;
            }
        }

        public List<int> GetBlockAddresses()
        {
            return _context.GetBlockAddresses();
        }

        public BlockModel GetAllFromAddress(int address)
        {
            var result = _context.GetAllFromAddress(address);
            var blockModel = new BlockModel { 
                Info = new InfoModel {
                    Address = result.Item1.Address,
                    Depth = result.Item1.Depth,
                    Records = result.Item1.Records,
                    OverflowAddress = result.Item1.OverflowAddress
                },
                Data = new DataModel {
                    BFactor = result.Item2.BFactor,
                    BlockDepth = result.Item2.BlockDepth,
                    ValidCount = result.Item2.ValidCount,
                    Records = result.Item2.Records
                }
            };
            return blockModel;
        }

        public List<OverflowBlockModel> GetAllFromOverflow(int overflowAddress)
        {
            var chainData = _context.GetAllFromOverflow(overflowAddress);
            var result = new List<OverflowBlockModel>();

            foreach (var pair in chainData) {
                result.Add(new OverflowBlockModel {
                    Info = new OverflowInfoModel {
                        NextOverflowAddress = pair.Item1.NextOwerflowAddress,
                        Records = pair.Item1.Records
                    },
                    Data = new DataModel {
                        BFactor = pair.Item2.BFactor,
                        BlockDepth = pair.Item2.BlockDepth,
                        ValidCount = pair.Item2.ValidCount,
                        Records = pair.Item2.Records
                    }
                });
            }

            return result;
        } 
    }
}

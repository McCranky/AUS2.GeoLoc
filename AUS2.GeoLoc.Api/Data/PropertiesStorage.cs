using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AUS2.GeoLoc.Structures.Hashing;

namespace AUS2.GeoLoc.Api.Data
{
    public class PropertiesStorage
    {
        private ExtendibleHashingDirectory<Property> _context;
        private int _idSequence = 0;
        private int _count = 0;
        public PropertiesStorage()
        {
            _context = new ExtendibleHashingDirectory<Property>("properties.dat", 5);
        }

        public bool CanSeed => _count == 0;

        public void SeedData(int count)
        {
            if (!CanSeed) return;
            _idSequence = 0;

            for (int i = 0; i < count; i++) {
                var num = _idSequence++;
                _context.Add(new Property { Id = num, RegisterNumber = num, Description = $"Property {num}" });
            }
        }

        public Property GetPropertyById(int id)
        {
            return _context.Find(new Property { Id = id });
        }

        public bool DeleteProperty(int id)
        {
            if (_context.Delete(new Property { Id = id })) {
                --_count;
                return true;
            }
            return false;
        }

        public int AddProperty(int regusterNumber, string description)
        {
            if (_context.Add(new Property { Id = _idSequence++, Description = description, RegisterNumber = regusterNumber })) {
                ++_count;
            }
            return _idSequence;
        }

        public List<int> GetBlockAddresses()
        {
            return _context.GetBlockAddresses();
        }


    }
}

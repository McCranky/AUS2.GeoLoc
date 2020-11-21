using AUS2.GeoLoc.Structures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Tester
{
    public class Gps
    {
        public double Latitude { get; set; }
        public char LatitudeSymbol { get; set; }
        public double Longitude { get; set; }
        public char LongitudeSymbol { get; set; }
    }
    public class Property : IData<Property>
    {
        public Guid Id { get; set; }
        public int RegisterNumber { get; set; }
        public string Description{ get; set; }
        public const int Max_Description_Length = 20;

        public Property()
        {
            Id = Guid.Empty;
            RegisterNumber = -1;
            Description = "";
        }

        public byte[] ToByteArray()
        {
            var result = new List<byte>();
            result.AddRange(Id.ToByteArray());
            result.AddRange(BitConverter.GetBytes(RegisterNumber));
            result.AddRange(BitConverter.GetBytes(Description.Length));
            for (int i = 0; i < Max_Description_Length; i++) {
                result.AddRange(
                    BitConverter.GetBytes(
                            i < Description.Length ? Description[i] : 'x'
                                          )
                                );
            }

            return result.ToArray();
        }

        public void FromByteArray(byte[] array)
        {
            var offset = 0;
            Id = new Guid(array.AsSpan(0, 16));
            offset += 16;
            RegisterNumber = BitConverter.ToInt32(array, offset);
            offset += sizeof(int);
            for (int i = 0; i < Max_Description_Length; i++) {
                
            }
            throw new NotImplementedException();
        }

        public int GetSize()
        {
            throw new NotImplementedException();
        }


        public BitArray GetHash()
        {
            throw new NotImplementedException();
        }

        public bool CustomEquals(Property data)
        {
            throw new NotImplementedException();
        }

        public Property GetEmptyClass()
        {
            throw new NotImplementedException();
        }
    }
}

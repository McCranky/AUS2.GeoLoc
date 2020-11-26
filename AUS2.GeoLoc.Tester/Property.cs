using AUS2.GeoLoc.Structures.Hashing;
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
        public const int MaxDescriptionLength = 20;
        //TODO Add 2x Gps 
        public int Id { get; set; } = int.MinValue;
        public int RegisterNumber { get; set; } = int.MinValue;
        public string Description{ get; set; } = string.Empty;

        public Property() {}

        public byte[] ToByteArray()
        {
            byte[] result;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(Id));
                ms.Write(BitConverter.GetBytes(RegisterNumber));
                ms.Write(BitConverter.GetBytes(Description.Length));
                ms.Write(Encoding.UTF8.GetBytes(Description));
                for (int i = 0; i < MaxDescriptionLength - Description.Length; i++) {
                    ms.WriteByte(BitConverter.GetBytes('x')[0]);
                }
                result = ms.ToArray();
            }
            return result;
        }

        public void FromByteArray(byte[] array)
        {
            using (var ms = new MemoryStream(array)) {
                var buffer = new byte[sizeof(int)];
                ms.Read(buffer);
                Id = BitConverter.ToInt32(buffer);

                ms.Read(buffer);
                RegisterNumber = BitConverter.ToInt32(buffer);

                ms.Read(buffer);
                var descLength = BitConverter.ToInt32(buffer);

                buffer = new byte[MaxDescriptionLength];
                ms.Read(buffer);
                Description = Encoding.UTF8.GetString(buffer).Substring(0, descLength);
            }
        }

        public int GetSize()
        {
            return sizeof(byte) * MaxDescriptionLength // Description full length
                + sizeof(int) * 3; // Id, RegisterNumber, Real Description length
        }


        public BitArray GetHash()
        {
            var hash = new BitArray(BitConverter.GetBytes(Id));
            hash.Length = 1;
            return hash;
        }

        public bool CustomEquals(Property data)
        {
            return Id == data.Id;
        }

        public Property GetEmptyClass()
        {
            return new Property();
        }

        public override string ToString()
        {
            return $"Id: {Id} /// RN: {RegisterNumber} /// Desc: {Description}";
        }
    }
}

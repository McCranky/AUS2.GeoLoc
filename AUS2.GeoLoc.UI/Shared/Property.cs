using AUS2.GeoLoc.Structures.Hashing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.UI.Shared
{
    public class Gps : IRecord
    {
        public double Latitude { get; set; } = 1.0;
        public char LatitudeSymbol { get; set; } = 'N';
        public double Longitude { get; set; } = 1.0;
        public char LongitudeSymbol { get; set; } = 'E';

        public void FromByteArray(byte[] array)
        {
            using (var ms = new MemoryStream(array)) {
                var buffer = new byte[sizeof(char)];
                ms.Read(buffer);
                LatitudeSymbol = BitConverter.ToChar(buffer);

                ms.Read(buffer);
                LongitudeSymbol = BitConverter.ToChar(buffer);

                buffer = new byte[sizeof(double)];
                ms.Read(buffer);
                Latitude = BitConverter.ToDouble(buffer);

                ms.Read(buffer);
                Longitude = BitConverter.ToDouble(buffer);
            }
        }

        public int GetSize()
        {
            return (sizeof(double) + sizeof(char)) * 2;
        }

        public byte[] ToByteArray()
        {
            byte[] result;
            using (var ms = new MemoryStream()) {
                ms.Write(BitConverter.GetBytes(LatitudeSymbol));
                ms.Write(BitConverter.GetBytes(LongitudeSymbol));
                ms.Write(BitConverter.GetBytes(Latitude));
                ms.Write(BitConverter.GetBytes(Longitude));
                result = ms.ToArray();
            }
            return result;
        }

        public override string ToString()
        {
            return $"{LatitudeSymbol}:{Latitude.ToString("0.00")} {LongitudeSymbol}:{Longitude.ToString("0.00")}";
        }
    }

    public class Property : IData<Property>
    {
        public const int MaxDescriptionLength = 20;
        public int Id { get; set; } = int.MinValue;
        public int RegisterNumber { get; set; } = int.MinValue;
        public string Description { get; set; } = string.Empty;
        public Gps Gps1 { get; set; } = new Gps();
        public Gps Gps2 { get; set; } = new Gps();

        public Property() { }

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
                ms.Write(Gps1.ToByteArray());
                ms.Write(Gps2.ToByteArray());
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

                buffer = new byte[Gps1.GetSize()];
                ms.Read(buffer);
                Gps1.FromByteArray(buffer);
                
                ms.Read(buffer);
                Gps2.FromByteArray(buffer);
            }
        }

        public int GetSize()
        {
            return sizeof(byte) * MaxDescriptionLength // Description full length
                + sizeof(int) * 3  // Id, RegisterNumber, Real Description length
                + Gps1.GetSize() * 2;
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
            return $"Id: {Id} /// RN: {RegisterNumber} /// Desc: {Description} /// Gps1: {Gps1} /// Gps2: {Gps2}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public class ExtendibleHashingDirectory<T> where T : IData<T>
    {
        public int BFactor { get; private set; }
        public string FilePath { get; set; }
        private FileStream fileStream;
        //dynamicke pole celych čisel
        // hlbka hashovania(adresaru)

        public ExtendibleHashingDirectory(string filePath, int bFactor)
        {
            FilePath = filePath;
            BFactor = bFactor; //TODO?? ak bude čas tak dorobyť automaticke vypočitanie na zaklade velkosti clustera a T
            fileStream = new FileStream(FilePath, FileMode.OpenOrCreate);
        }

        public T Find(T data)
        {
            //Block<T> block;
            var block = new Block<T>(BFactor, data.GetEmptyClass());
            var hash = data.GetHash();


            //TODO extended hashing


            // inicializacia bloku
            var blockBytes = new byte[block.GetSize()];
            fileStream.Read(blockBytes, 0, blockBytes.Length); //TODO miesto 0 vypočitana adresu bloku 
            block.FromByteArray(blockBytes);
            //hladanie zaznamu v bloku
            foreach (var record in block.GetRecords()) {
                if (data.CustomEquals(record))
                    return record;
            }

            return default(T);
        }
    }
}

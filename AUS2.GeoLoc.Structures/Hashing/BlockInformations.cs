using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.Structures.Hashing
{
    public class BlockInformations
    {
        public int Address { get; set; }
        public int Records { get; set; }
        public int Depth { get; set; }
        public int OverflowAddress { get; set; } = int.MinValue;
    }
}

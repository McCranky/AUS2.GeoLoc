using AUS2.GeoLoc.Structures.Hashing;
using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.UI.Shared
{
    public class InfoModel
    {
        public int Address { get; set; }
        public int Records { get; set; }
        public int Depth { get; set; }
        public int OverflowAddress { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.UI.Shared
{
    public class DataModel
    {
        public int BFactor { get; set; }
        public int ValidCount { get; set; }
        public int BlockDepth { get; set; }
        public List<Property> Records { get; set; }
    }
}

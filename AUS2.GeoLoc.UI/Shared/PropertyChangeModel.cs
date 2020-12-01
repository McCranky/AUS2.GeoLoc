using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.UI.Shared
{
    public class PropertyChangeModel
    {
        public int OriginalId { get; set; }
        public Property Property { get; set; }
        public bool HasIdChanged => OriginalId != Property.Id;
    }
}

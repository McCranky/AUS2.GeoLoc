﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public interface IRecord
    {
        public byte[] ToByteArray();
        public void FromByteArray(byte[] array);
        public int GetSize();
    }
}
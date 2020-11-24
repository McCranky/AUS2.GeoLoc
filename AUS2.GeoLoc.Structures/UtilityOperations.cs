using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AUS2.GeoLoc.Structures
{
    public static class UtilityOperations
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetDiskFreeSpace(
            string lpRootPathName,
            out int lpSectorsPerCluster,
            out int lpBytesPerSector,
            out int lpNumberOfFreeClusters,
            out int lpTotalNumberOfClusters);
    }
}

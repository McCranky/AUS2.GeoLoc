using System.Runtime.InteropServices;

namespace AUS2.GeoLoc.Structures.Utilities
{
    public static class UtilityOperations
    {
        /// <summary>
        /// Calculates disk vales for given disk root letter like "C:\\"
        /// </summary>
        /// <param name="lpRootPathName"></param>
        /// <param name="lpSectorsPerCluster"></param>
        /// <param name="lpBytesPerSector"></param>
        /// <param name="lpNumberOfFreeClusters"></param>
        /// <param name="lpTotalNumberOfClusters"></param>
        /// <returns></returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetDiskFreeSpace(
            string lpRootPathName,
            out int lpSectorsPerCluster,
            out int lpBytesPerSector,
            out int lpNumberOfFreeClusters,
            out int lpTotalNumberOfClusters);
    }
}

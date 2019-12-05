using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer
{
    public static class GeoRegionHelper
    {
        #region Constants

        public enum SYSGEOTYPE
        {
            GEO_NATION = 0x0001,
            GEO_LATITUDE = 0x0002,
            GEO_LONGITUDE = 0x0003,
            GEO_ISO2 = 0x0004,
            GEO_ISO3 = 0x0005,
            GEO_RFC1766 = 0x0006,
            GEO_LCID = 0x0007,
            GEO_FRIENDLYNAME = 0x0008,
            GEO_OFFICIALNAME = 0x0009,
            GEO_TIMEZONES = 0x000A,
            GEO_OFFICIALLANGUAGES = 0x000B
        }

        #endregion

        #region Private Enums

        private enum GeoClass : int
        {
            Nation = 16,
            Region = 14,
        };

        #endregion

        #region Win32 Declarations

        [DllImport("kernel32.dll", ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int GetUserGeoID(GeoClass geoClass);

        [DllImport("kernel32.dll")]
        private static extern int GetUserDefaultLCID();

        [DllImport("kernel32.dll")]
        private static extern int GetGeoInfo(int geoid, int geoType, StringBuilder lpGeoData, int cchData, int langid);

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns machine current location as specified in Region and Language settings.
        /// </summary>
        /// <param name="geoFriendlyname"></param>
        public static string GetLocation(SYSGEOTYPE geoFriendlyname)
        {
            try
            {
                var geoId = GetUserGeoID(GeoClass.Nation);
                var lcid = GetUserDefaultLCID();
                var locationBuffer = new StringBuilder(100);
                GetGeoInfo(geoId, (int)geoFriendlyname, locationBuffer, locationBuffer.Capacity, lcid);

                var str = locationBuffer.ToString().Trim();
                return string.IsNullOrWhiteSpace(str) ? null : str;
            }
            catch (System.Exception e)
            {
                Trace.WriteLine(e);
                return null;
            }
        }

        public static string GetCountryCode()
            => GetLocation(SYSGEOTYPE.GEO_ISO2)
               ?? RegionInfo.CurrentRegion.TwoLetterISORegionName;

        #endregion
    }
}
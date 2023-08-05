using System;

namespace CoolapkUWP.OSSUploader.Helpers
{
    public static class DateHelper
    {
        private static readonly DateTime UnixDateBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ConvertUnixTimeStampToDateTime(this long time) => UnixDateBase.Add(new TimeSpan(time * 1000_0000));

        public static double ConvertDateTimeToUnixTimeStamp(this DateTime time) => Math.Round(time.ToUniversalTime().Subtract(UnixDateBase).TotalSeconds);
    }
}

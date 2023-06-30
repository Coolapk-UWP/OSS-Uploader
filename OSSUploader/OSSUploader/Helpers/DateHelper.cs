using System;

namespace CoolapkUWP.OSSUploader.Helpers
{
    public static class DateHelper
    {
        public enum TimeIntervalType
        {
            MonthsAgo,
            DaysAgo,
            HoursAgo,
            MinutesAgo,
            JustNow,
        }

        private static readonly DateTime unixDateBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ConvertUnixTimeStampToDateTime(this long time) => unixDateBase.Add(new TimeSpan(time * 1000_0000));

        public static double ConvertDateTimeToUnixTimeStamp(this DateTime time) => Math.Round(time.ToUniversalTime().Subtract(unixDateBase).TotalSeconds);
    }
}

using System;

namespace LSportsServer
{
    public static class CMyTime
    {
        public static DateTime GetMyTime()
        {
            DateTime dtCurrentTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Korea Standard Time");
            return dtCurrentTime;
        }

        public static DateTime AddTime(DateTime srcTime, int D, int H, int m, int s)
        {
            TimeSpan addTime = new TimeSpan(D, H, m, s);
            DateTime dstTime = srcTime + addTime;

            return dstTime;
        }

        public static string GetMyTimeStr(string strFormat = "yyyy-MM-dd HH:mm:ss")
        {
            return GetMyTime().ToString(strFormat);
        }

        public static DateTime ConvertStrToTime(string strTime)
        {
            return DateTime.Parse(strTime);
        }

        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time")).AddSeconds(timestamp);
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"));
            return Math.Round((date - dateTime).TotalSeconds) + 9 * 3600;
        }
    }
}

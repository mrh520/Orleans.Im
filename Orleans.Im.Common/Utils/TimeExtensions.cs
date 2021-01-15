
namespace System
{
    public static class TimeExtensions
    {
        /// <summary>
        /// 获取时间差
        /// </summary>
        /// <param name="beginTime">开始时间（较早）</param>
        /// <param name="endTime">结束时间（较晚）</param>
        /// <returns></returns>
        public static TimeSpan TimeDifference(this DateTime beginTime, DateTime endTime)
        {
            var endTs = new TimeSpan(endTime.Ticks);
            var beginTs = new TimeSpan(beginTime.Ticks);
            return endTs.Subtract(beginTs);
        }
        /// <summary>
        /// 获取时间相差的总天数
        /// </summary>
        /// <param name="beginTime">开始时间（较早）</param>
        /// <param name="endTime">结束时间（较晚）</param>
        /// <returns>相隔天数</returns>
        public static long DayDifference(this DateTime beginTime, DateTime endTime)
        {
            return TimeDifference(beginTime, endTime).Days;
        }
        /// <summary>
        /// 获取时间相差的小时数
        /// </summary>
        /// <param name="beginTime">开始时间（较早）</param>
        /// <param name="endTime">结束时间（较晚）</param>
        /// <returns>相隔小时数</returns>
        public static long HourDifference(this DateTime beginTime, DateTime endTime)
        {
            var ts = TimeDifference(beginTime, endTime);
            return ts.Days * 24 + ts.Hours;
        }
        /// <summary>
        /// 获取时间相差的总分钟数
        /// </summary>
        /// <param name="beginTime">开始时间（较早）</param>
        /// <param name="endTime">结束时间（较晚）</param>
        /// <returns>相隔分钟数</returns>
        public static long MinuteDifference(this DateTime beginTime, DateTime endTime)
        {
            var ts = TimeDifference(beginTime, endTime);
            return ts.Days * 1440 + ts.Hours * 60 + ts.Minutes;
        }
        /// <summary>
        /// 获取时间相差的总秒钟数
        /// </summary>
        /// <param name="beginTime">开始时间（较早）</param>
        /// <param name="endTime">结束时间（较晚）</param>
        /// <returns>相隔秒钟数</returns>
        public static long SecondDifference(this DateTime beginTime, DateTime endTime)
        {
            var ts = TimeDifference(beginTime, endTime);
            return ts.Days * 86400 + ts.Hours * 3600 + ts.Minutes * 60 + ts.Seconds;
        }

        /// <summary>
        /// 获取时间相差的总毫秒数
        /// </summary>
        /// <param name="beginTime">开始时间（较早）</param>
        /// <param name="endTime">结束时间（较晚）</param>
        /// <returns>相隔毫秒数</returns>
        public static long MillisecondDifference(this DateTime beginTime, DateTime endTime)
        {
            var ts = TimeDifference(beginTime, endTime);
            return (ts.Days * 86400 + ts.Hours * 3600 + ts.Minutes * 60 + ts.Seconds) * 1000 + ts.Milliseconds;
        }

        /// <summary>
        /// 统一时间进制为24小时制(防止时间出现AM,PM等不能入库)
        /// </summary>
        /// <param name="time"></param>
        /// <param name="separator">分隔符[-/.]</param>
        /// <returns></returns>
        public static string ToShortDate(this DateTime time, params char[] separator)
        {
            string format = separator.Length > 0 ? string.Format("yyyy{0}MM{0}dd", separator[0]) : "yyyy-MM-dd";
            return time.ToString(format);
        }

        /// <summary>
        /// 统一时间进制为24小时制(防止时间出现AM,PM等不能入库)
        /// </summary>
        /// <param name="time"></param>
        /// <param name="separator">分隔符[-/.]</param>
        /// <returns></returns>
        public static string ToLongDate(this DateTime time, params char[] separator)
        {
            string format = separator.Length > 0 ? string.Format("yyyy{0}MM{0}dd HH:mm:ss", separator[0]) : "yyyy-MM-dd HH:mm:ss";
            return time.ToString(format);
        }

        /// <summary>
        /// 本地时间转成GMT时间
        /// 本地时间为：2011-9-29 15:04:39
        /// 转换后的时间为：Thu, 29 Sep 2011 07:04:39 GMT
        /// </summary>
        /// <param name="localTime"></param>
        /// <returns></returns>
        public static string ToGMTString(this DateTime localTime)
        {
            return localTime.ToUniversalTime().ToString("r");
        }

        /// <summary>
        /// 本地时间转成GMT格式的时间
        /// 本地时间为：2011-9-29 15:04:39
        /// 转换后的时间为：Thu, 29 Sep 2011 15:04:39 GMT+0800
        /// </summary>
        /// <param name="localTime"></param>
        /// <returns></returns>
        public static string ToGMTFormat(this DateTime localTime)
        {
            return localTime.ToString("r") + localTime.ToString("zzz").Replace(":", "");
        }

        /// <summary>  
        /// GMT时间转成本地时间
        /// DateTime dt1 = GMT2Local("Thu, 29 Sep 2011 07:04:39 GMT");
        /// 转换后的dt1为：2011-9-29 15:04:39
        /// DateTime dt2 = GMT2Local("Thu, 29 Sep 2011 15:04:39 GMT+0800");
        /// 转换后的dt2为：2011-9-29 15:04:39
        /// </summary>  
        /// <param name="gmt">字符串形式的GMT时间</param>  
        /// <returns></returns>
        public static DateTime GMT2Local(this string gmt)
        {
            DateTime dt = DateTime.MinValue;
            try
            {
                string pattern = string.Empty;
                if (gmt.IndexOf("+0") != -1)
                {
                    gmt = gmt.Replace("GMT", "");
                    pattern = "ddd, dd MMM yyyy HH':'mm':'ss zzz";
                }
                if (gmt.ToUpper().IndexOf("GMT") != -1)
                {
                    pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
                }
                if (pattern != string.Empty)
                {
                    dt = DateTime.ParseExact(gmt, pattern, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
                    dt = dt.ToLocalTime();
                }
                else
                {
                    dt = Convert.ToDateTime(gmt);
                }
            }
            catch
            {
            }
            return dt;
        }

        /// <summary>
        /// 本地时间转换UTC时间(注意服务器时区)
        /// </summary>
        /// <param name="localTime"></param>
        /// <returns></returns>
        public static DateTime LocalTime2UTCTime(this DateTime localTime)
        {
            return localTime.ToUniversalTime();
        }

        /// <summary>
        /// UTC时间转换本地时间(注意服务器时区)
        /// </summary>
        /// <param name="utcTime"></param>
        /// <returns></returns>
        public static DateTime UTCTime2LocalTime(this DateTime utcTime)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(utcTime);
        }

        /// <summary>
        /// Unix时间戳转UTC时间
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime TimeStamp2UTCTime(this long timeStamp)
        {
            return new DateTime(timeStamp * 10000000 + 621355968000000000);
        }

        /// <summary>
        /// UTC时间转Unix时间戳
        /// </summary>
        /// <param name="utcTime"></param>
        /// <returns></returns>
        public static long UTCTime2TimeStamp(this DateTime utcTime)
        {
            return (utcTime.Ticks - 621355968000000000) / 10000000;
        }

        /// <summary>
        /// 获取星期几(中文)
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string WeekDay(this DateTime time)
        {
            string[] weeks = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            return weeks[(int)time.DayOfWeek];
        }

        /// <summary>
        /// 获取星期几(英文)
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string WeekDayEn(this DateTime time)
        {
            return time.DayOfWeek.ToString();
        }

        /// <summary>
        /// 获取日期的英语格式(Jan 3, 2010)
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string DateEn(this DateTime time)
        {
            string[] months = { "January", "Febrhuary", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

            return months[time.Month - 1].Substring(0, 3) + " " + time.Day + ", " + time.Year;
        }

        /// <summary>
        /// 生日转年龄
        /// </summary>
        /// <param name="birthday"></param>
        /// <returns></returns>
        public static int BirthdayToAge(this DateTime birthday)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birthday.Year;
            if (birthday > today.AddYears(-age))
                age--;
            return age < 0 ? 0 : age;
        }

        /// <summary>
        /// 获取某个日期时当月的第几周
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public static int WeekOfMonth(this DateTime day)
        {
            var firstofMonth = Convert.ToDateTime(day.Date.Year + "-" + day.Date.Month + "-" + 1);

            var i = (int)firstofMonth.Date.DayOfWeek;
            if (i == 0)
            {
                i = 7;
            }

            return (day.Date.Day + i - 2) / 7 + 1;
        }

        /// <summary>
        /// 获取某个日期时当周的第几天（根据中国的习惯，周日返回的是7，不是0）
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public static int DayOfWeekInChina(this DateTime day)
        {
            var t = (int)DateTime.Now.DayOfWeek;
            return t == 0 ? 7 : t;
        }
    }
}

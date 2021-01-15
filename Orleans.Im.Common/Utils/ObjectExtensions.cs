using System.Text.RegularExpressions;

namespace System
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// 安全获取值，当值为null时，不会抛出异常
        /// </summary>
        /// <param name="value">可空值</param>
        //public static T SafeValue<T>(this T? value) where T : struct
        //{
        //    return value ?? default;
        //}

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="removeQuote">是否删除首尾双引号</param>
        /// <returns></returns>
        public static string ToStr(this object obj, bool removeQuote = false)
        {
            if (obj == null || obj.Equals(null))
                return string.Empty;
            if (!removeQuote)
                return obj.ToString();
            var s = obj.ToString();
            if (s.StartsWith("\"") && s.EndsWith("\""))
            {
                s = s.Substring(1, s.Length - 2);
                return s;
            }
            if (s.StartsWith("\""))
            {
                s = s.Substring(1);
                return s;
            }
            if (s.EndsWith("\""))
                s = s.Substring(0, s.Length - 1);
            return s;
        }

        /// <summary>
        /// 转换为整型
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue">转化失败的时候的默认值，默认为0</param>
        /// <param name="isRound">是否支持四舍五入，默认不支持</param>
        /// <returns></returns>
        public static int ToInt(this object obj, int defaultValue = 0, bool isRound = false)
        {
            int r;
            var s = obj.ToStr();
            r = int.TryParse(s, out r) ? r : defaultValue;
            if (r == 0)
                return isRound ? Convert.ToInt32(s.ToFloat()) : (int)s.ToFloat(); //解决符合小数格式的字符串或者数值型错误转换为0
            return r;
        }

        /// <summary>
        /// 转换为长整型
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue">转化失败的时候的默认值，默认为0</param>
        /// <param name="isRound">是否支持四舍五入，默认不支持</param>
        /// <returns></returns>
        public static long ToInt64(this object obj, long defaultValue = 0, bool isRound = false)
        {
            long r;
            var s = obj.ToStr();
            r = long.TryParse(s, out r) ? r : defaultValue;
            if (r == 0)
                return isRound ? Convert.ToInt64(s.ToFloat()) : (long)s.ToFloat(); //解决符合小数格式的字符串或者数值型错误转换为0
            return r;
        }

        /// <summary>
        /// 转换为浮点数
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static float ToFloat(this object obj, float defaultValue = 0)
        {
            float r;
            return float.TryParse(obj.ToStr(), out r) ? r : defaultValue;
        }

        /// <summary>
        /// 转换为双精度型
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double ToDouble(this object obj, double defaultValue = 0)
        {
            double r;
            return double.TryParse(obj.ToStr(), out r) ? r : defaultValue;
        }

        /// <summary>
        /// 转换为Decimal
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal ToDecimal(this object obj, decimal defaultValue = 0)
        {
            decimal r;
            return decimal.TryParse(obj.ToStr(), out r) ? r : defaultValue;
        }

        /// <summary>
        /// 转换为DateTime
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this object obj)
        {
            var r = obj.ToDateTime(new DateTime(1970, 1, 1));
            if (r.HasValue)
                return (DateTime)r;
            return new DateTime(1970, 1, 1);
        }

        /// <summary>
        /// 转换为DateTime
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static DateTime? ToDateTime(this object obj, DateTime? defaultValue)
        {
            if (obj == DBNull.Value || obj == null || obj.Equals(""))
                return defaultValue;
            var r = new DateTime(1970, 1, 1);
            if (obj is string)
            {
                string str = obj.ToString();
                int len = str.Length;
                if (len < 8)
                    return r;
                if (len == 8)
                {
                    string y = str.Substring(0, 4);
                    string m = Regex.Replace(str.Substring(4, 2), @"\D*", "");
                    string d = Regex.Replace(str.Substring(6, 2), @"\D*", "");
                    str = y + "-" + m + "-" + d;
                    return DateTime.TryParse(str, out r) ? r : defaultValue;
                }
            }

            try
            {
                //return DateTime.TryParse(obj.ToString(), out r) ? r : defaultValue;   取消直接 .ToString()，会忽略掉毫秒
                return Convert.ToDateTime(obj);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 转换为Boolean
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool ToBoolean(this object obj, bool defaultValue = false)
        {
            string str = obj.ToStr();
            if (str == "1" || str.ToLower() == "true")
                return true;
            bool r;
            return bool.TryParse(str, out r) ? r : defaultValue;
        }

        /// <summary>
        /// 转换为Guid
        /// </summary>
        /// <param name="obj">输入值</param>
        public static Guid ToGuid(this object obj)
        {
            var r = Guid.TryParse(obj.ToStr(), out var result) ? (Guid?)result : null;
            return r ?? Guid.Empty;
        }

        /// <summary>
        /// 将字符串转换成指定类型
        /// </summary>
        /// <param name="str"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static object ToObject(this string str, string typeName)
        {
            if (typeName.IsEmpty())
                return "";
            typeName = typeName.ToLower();
            switch (typeName)
            {
                case "int":
                    return str.ToInt();
                case "long":
                    return str.ToInt64();
                case "double":
                    return str.ToDouble();
                case "float":
                    return str.ToFloat();
                case "decimal":
                    return str.ToDecimal();
                case "bool":
                    return str.ToBoolean();
                case "datetime":
                    return str.ToDateTime();
                default:
                    return str.SafeTrim();
            }
        }

        /// <summary>
        /// 将任意类型转换成Object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object ToObject(this object obj)
        {
            return (object)obj;
        }
    }
}

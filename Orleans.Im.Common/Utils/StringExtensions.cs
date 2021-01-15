
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace System
{
    public static class StringExtensions
    {
        /// <summary>
        /// URL编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlEncode(this string input)
        {
            return HttpUtility.UrlEncode(input);
        }

        /// <summary>
        /// URL解码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlDecode(this string input)
        {
            return HttpUtility.UrlDecode(input);
        }

        /// <summary>
        /// 字符串编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Escape(this string input)
        {
            if (input.IsEmpty())
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            byte[] data = System.Text.Encoding.Unicode.GetBytes(input);

            for (int i = 0; i < data.Length; i += 2)
            {
                sb.Append("%u");
                sb.Append(data[i + 1].ToString("X2"));

                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString();

        }

        /// <summary>
        /// 字符串解码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UnEscape(this string input)
        {
            if (input.StartsWith("%u"))
            {
                string str = input.Remove(0, 2);//删除最前面两个＂%u＂
                string[] strArr = str.Split(new string[] { "%u" }, StringSplitOptions.None);//以子字符串＂%u＂分隔
                byte[] byteArr = new byte[strArr.Length * 2];
                for (int i = 0, j = 0; i < strArr.Length; i++, j += 2)
                {
                    byteArr[j + 1] = Convert.ToByte(strArr[i].Substring(0, 2), 16); //把十六进制形式的字串符串转换为二进制字节
                    byteArr[j] = Convert.ToByte(strArr[i].Substring(2, 2), 16);
                }
                str = Encoding.Unicode.GetString(byteArr); //把字节转为unicode编码
                return str;
            }
            else
            {
                return input;
            }
        }

        public static string ToUnicode(this string original)
        {
            try
            {
                byte[] data = Encoding.BigEndianUnicode.GetBytes(original);
                int i = 0;
                StringBuilder sb = new StringBuilder();
                foreach (byte b in data)
                {
                    if (i++ % 2 == 0) sb.Append("\\u");
                    sb.AppendFormat("{0:X2}", b);
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 删除无法编码为utf-8的unicode字符，例如emoji字符
        /// </summary>
        /// <param name="original">字符串</param>
        /// <returns></returns>
        //public static string RemoveUnicode(this string original)
        //{
        //    original = original.FilterDangerousChar();
        //    if (original.Length == 0)
        //        return string.Empty;

        //    return original.RegexReplace(@"\p{Cs}", "");
        //}

        /// <summary>
        /// 首字母小写
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string InitialToLower(this string original)
        {
            if (string.IsNullOrWhiteSpace(original))
                return string.Empty;
            int len = original.Length;
            string firstChar = original.Substring(0, 1);
            string otherChar = len == 1 ? "" : original.Substring(1);
            return firstChar.ToLower() + otherChar;
        }

        /// <summary>
        /// 首字母大写
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string InitialToUpper(this string original)
        {
            if (string.IsNullOrWhiteSpace(original))
                return string.Empty;
            int len = original.Length;
            string firstChar = original.Substring(0, 1);
            string otherChar = len == 1 ? "" : original.Substring(1);
            return firstChar.ToUpper() + otherChar;
        }


        public static bool IsEmpty(this string original)
        {
            if (original == null || original.Equals(null) || original.Trim() == string.Empty)
                return true;
            return false;
        }

        public static bool IsNotEmpty(this string original)
        {
            if (original == null || original.Equals(null) || original.Trim() == string.Empty)
                return false;
            return true;
        }

        public static string SafeTrim(this string original)
        {
            return (original == null || original.Equals(null)) ? string.Empty : original.Trim();
        }

        /// <summary>
        /// 去除所有空格
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string TrimAllEmpty(this string original)
        {
            return (original == null || original.Equals(null)) ? string.Empty : new Regex("\\s+").Replace(original, "");
        }

        /// <summary>
        /// 忽略大小写比较字符串
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strB"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string strA, string strB)
        {
            return strA.Equals(strB, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// 忽略大小写,strA是否包含strB
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strB"></param>
        /// <returns></returns>
        public static bool ContainsIgnoreCase(this string strA, string strB)
        {
            return strA.IndexOf(strB, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #region 将字符串转换为数组

        //speater示例 new char[] { ',', '，' }
        public static string[] StrToArray(this string str, char[] speater)
        {
            if (string.IsNullOrEmpty(str))
                return new string[] { };
            return str.Split(speater, StringSplitOptions.RemoveEmptyEntries);
        }
        public static string[] StrToArray(this string str, char speater)
        {
            if (string.IsNullOrEmpty(str))
                return new string[] { };
            char[] c = new char[] { speater };
            return str.Split(c, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion

        #region 将数组转换为字符串

        public static string ArrayToStr(this List<int> list)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i == (list.Count - 1))
                {
                    builder.Append(list[i].ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append(list[i]);
                    builder.Append(",");
                }
            }
            return builder.ToString();
        }

        public static string ArrayToStr(this List<string> list, string speater)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i == (list.Count - 1))
                {
                    builder.Append(list[i]);
                }
                else
                {
                    builder.Append(list[i]);
                    builder.Append(speater);
                }
            }
            return builder.ToString();
        }

        #endregion

        /// <summary>
        /// 逗号分隔字符串去除重复(1,1,2,3->1,2,3)
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string DistinctString(this string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return inputString;
            return string.Join(",", inputString.Split(',').Distinct().ToArray());
        }

        /// <summary>
        /// 删除最后结尾的一个逗号
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string DeleteLastComma(this string original)
        {
            if (string.IsNullOrWhiteSpace(original))
                return original;
            return original.Substring(0, original.LastIndexOf(",", StringComparison.Ordinal));
        }

        /// <summary>
        /// 删除最后结尾的指定字符后的字符
        /// </summary>
        /// <param name="original"></param>
        /// <param name="delStr"></param>
        /// <returns></returns>
        public static string DeleteLastChar(this string original, string delStr)
        {
            if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(delStr))
                return original;
            return original.Substring(0, original.LastIndexOf(delStr, StringComparison.Ordinal));
        }

        /// <summary>
        /// 计算字符串中子串出现的次数
        /// </summary>
        /// <param name="original">字符串</param>
        /// <param name="substring">子串</param>
        /// <returns></returns>
        public static int SubstringCount(this string original, string substring)
        {
            if (original.IsEmpty() || substring.IsEmpty())
                return 0;
            if (original.Contains(substring))
            {
                var replaced = original.Replace(substring, string.Empty);
                return (original.Length - replaced.Length) / substring.Length;
            }
            return 0;
        }

        #region 截取字符长度

        /// <summary>
        /// 从字符串左侧截取
        /// </summary>
        /// <param name="original"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string Left(this string original, int length)
        {
            if (string.IsNullOrWhiteSpace(original))
                return original;
            return original.Length > length ? original.Substring(0, length) : original;
        }

        /// <summary>
        /// 从字符串右侧截取
        /// </summary>
        /// <param name="original"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string Right(this string original, int length)
        {
            if (string.IsNullOrWhiteSpace(original))
                return original;
            return original.Length > length ? original.Substring(original.Length - length) : original;
        }

        /// <summary>
        /// 截取字符长度
        /// </summary>
        /// <param name="inputString">字符</param>
        /// <param name="len">长度</param>
        /// <param name="needApostrophe">是否加省略号</param>
        /// <returns></returns>
        public static string CutString(this string inputString, int len, bool needApostrophe = false)
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return inputString;

            if (inputString.Length <= len)
                return inputString;

            if (needApostrophe)
            {
                return inputString.Substring(0, len - 1) + "···"; //len-1,为了显示"…"
            }
            else
            {
                return inputString.Substring(0, len);
            }
        }
        /// <summary>
        /// 截取字符长度
        /// </summary>
        /// <param name="inputString">字符</param>
        /// <param name="len">长度</param>
        /// <param name="needApostrophe">是否加省略号</param>
        /// <returns></returns>
        public static string CutASCIIString(this string inputString, int len, bool needApostrophe = false)
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return inputString;

            ASCIIEncoding ascii = new ASCIIEncoding();
            int tempLen = 0;
            string tempString = "";
            byte[] s = ascii.GetBytes(inputString);

            if (s.Length <= len)
                return inputString;

            if (needApostrophe)
                len = len - 2; //为了显示"…"

            for (int i = 0; i < s.Length; i++)
            {
                if ((int)s[i] == 63)
                {
                    tempLen += 2;
                }
                else
                {
                    tempLen += 1;
                }

                try
                {
                    tempString += inputString.Substring(i, 1);
                }
                catch
                {
                    break;
                }

                if (tempLen >= len)
                    break;
            }
            //如果截过则加上半个省略号 
            byte[] mybyte = Encoding.Default.GetBytes(inputString);
            if (mybyte.Length > len || needApostrophe)
                tempString += "…";
            return tempString;
        }

        #endregion

        #region 半角全角转换

        /// <summary>
        /// 转半角的函数(SBC case)
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string ToDBC(this string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return inputString;
            char[] chArray = inputString.ToCharArray();
            for (int i = 0; i < chArray.Length; i++)
            {
                if (chArray[i] == '　')
                {
                    chArray[i] = ' ';
                }
                else if ((chArray[i] > 0xff00) && (chArray[i] < 0xff5f))
                {
                    chArray[i] = (char)(chArray[i] - 0xfee0);
                }
            }
            return new string(chArray);
        }

        /// <summary>
        /// 转全角的函数(SBC case)
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static string ToSBC(this string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return inputString;
            char[] chArray = inputString.ToCharArray();
            for (int i = 0; i < chArray.Length; i++)
            {
                if (chArray[i] == ' ')
                {
                    chArray[i] = '　';
                }
                else if (chArray[i] < '\x007f')
                {
                    chArray[i] = (char)(chArray[i] + 0xfee0);
                }
            }
            return new string(chArray);
        }

        #endregion

        #region 检查过滤危险字符

        /// <summary>
        /// 检测是否含有危险字符
        /// </summary>
        /// <param name="inputString">待检测的字符串</param>
        /// <param name="dangerString">危险字符</param>
        /// <returns></returns>
        public static bool DetectDangerousChar(this string inputString, string dangerString = "")
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return false;

            string str = @"*|and|exec|insert|select|delete|update|count|master|truncate|declare|char(|mid(|chr(|'|script";
            if (!string.IsNullOrWhiteSpace(dangerString))
            {
                str = str + "|" + dangerString;
            }

            return Regex.Match(inputString, Regex.Escape(str), RegexOptions.Compiled | RegexOptions.IgnoreCase).Success;
        }

        /// <summary>
        /// 过滤包含的危险字符
        /// </summary>
        /// <param name="inputString">待过滤的字符串</param>
        /// <param name="dangerString">危险字符</param>
        /// <returns></returns>
        public static string FilterDangerousChar(this string inputString, string dangerString = "")
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return string.Empty;

            //string str = @"*|and|exec|insert|select|delete|update|count|master|truncate|declare|char(|mid(|chr(|'|script";
            //if (!string.IsNullOrWhiteSpace(dangerString))
            //{
            //    str = str + "|" + dangerString;
            //}

            //foreach (string i in str.Split('|'))
            //{
            //    inputString = Regex.Replace(inputString, " " + i, " ", RegexOptions.IgnoreCase);
            //    inputString = Regex.Replace(inputString, i + " ", " ", RegexOptions.IgnoreCase);
            //}

            //todo 过滤sql注入
            inputString = inputString.Replace("'", "’");
            //todo 过滤敏感内容
            inputString = Regex.Replace(inputString, "script", "ｓcript", RegexOptions.IgnoreCase);

            if (!string.IsNullOrWhiteSpace(dangerString))
            {
                foreach (string i in dangerString.Split('|'))
                {
                    inputString = Regex.Replace(inputString, i, "***", RegexOptions.IgnoreCase);
                    inputString = Regex.Replace(inputString, i, "***", RegexOptions.IgnoreCase);
                }
            }

            return inputString.SafeTrim();
        }

        /// <summary>
        /// 显示包含的危险字符（慎重）
        /// </summary>
        /// <param name="inputString">已过滤的字符串</param>
        /// <returns></returns>
        public static string ShowDangerousChar(this string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString))
                return string.Empty;

            inputString = inputString.Replace("’", "'");
            inputString = Regex.Replace(inputString, "ｓcript", "script", RegexOptions.IgnoreCase);

            return inputString.SafeTrim();
        }

        #endregion
    }
}

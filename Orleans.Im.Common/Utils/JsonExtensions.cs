
using Newtonsoft.Json;

namespace System
{
    public static class JsonExtensions
    {
        /// <summary>
        /// 将对象转换为Json字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="isIgnoreNull">是否删除值为null属性</param>
        /// <param name="isConvertToSingleQuotes">是否将双引号转成单引号</param>
        /// <returns></returns>
        public static string ToJson(this object obj, bool isIgnoreNull = false, bool isConvertToSingleQuotes = false)
        {
            if (obj == null)
                return "{}";
            try
            {
                var setting = new JsonSerializerSettings();
                JsonConvert.DefaultSettings = () =>
                {
                    setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                    //日期类型默认格式化处理
                    setting.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
                    setting.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                    //setting.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

                    //空值处理
                    if (isIgnoreNull)
                        setting.NullValueHandling = NullValueHandling.Ignore;

                    return setting;
                };

                var r = JsonConvert.SerializeObject(obj, setting);

                if (isConvertToSingleQuotes)
                    r = r.Replace("\"", "'");

                return r;
            }
            catch (Exception e)
            {
                return "{}";
            }
        }

        /// <summary>
        /// 将对象转换为Json字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="count"></param>
        /// <param name="isIgnoreNull">是否删除值为null属性</param>
        /// <param name="isConvertToSingleQuotes">是否将双引号转成单引号</param>
        /// <returns></returns>
        public static string ToJson(this object obj, int count, bool isIgnoreNull = false, bool isConvertToSingleQuotes = false)
        {
            string json = "{}";
            if (obj != null)
            {
                try
                {
                    var setting = new JsonSerializerSettings();
                    JsonConvert.DefaultSettings = () =>
                    {
                        //日期类型默认格式化处理
                        setting.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
                        setting.DateFormatString = "yyyy-MM-dd HH:mm:ss";

                        //空值处理
                        if (isIgnoreNull)
                            setting.NullValueHandling = NullValueHandling.Ignore;

                        return setting;
                    };
                    json = JsonConvert.SerializeObject(obj);
                }
                catch
                {
                }
            }

            var r = "{\"total\":" + count + ",\"rows\":" + json + "}";

            if (isConvertToSingleQuotes)
                r = r.Replace("\"", "'");

            return r;
        }

        /// <summary>
        /// 将Json字符串转换为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T ToObject<T>(this string json)
        {
            try
            {
                json = json.SafeTrim();
                if (json.Length == 0 || json == "{}")
                    return default(T);

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                try
                {
                    json = json.Replace("{\"", "[$$1]").Replace("\":\"", "[$$2]").Replace("\",\"", "[$$3]").Replace("\\\"", "[$$4]").Replace("\"}", "[$$5]");
                    json = json.Replace("\"", "\\\""); //过滤破坏json格式的字符
                    json = json.Replace("[$$1]", "{\"").Replace("[$$2]", "\":\"").Replace("[$$3]", "\",\"").Replace("[$$4]", "\\\"").Replace("[$$5]", "\"}");

                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch
                {
                    return default(T);
                }
            }
        }
    }
}

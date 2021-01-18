using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Orleans.Im.Common
{
    public partial class RedisHelper
    {
        #region Redis Connection Manager

        private static string ConnectString = GlobalVariable.Configuration.GetValue<string>("Redis:ConnectString");
        private static RedisHelper instance;
        private static readonly object _lock = new object();
        private static Lazy<ConnectionMultiplexer> _lazyConnection;
        private static readonly Object MultiplexerLock = new Object();

        /// <summary>
        /// 实例化入口
        /// </summary>
        /// <returns>实例对象</returns>
        public static RedisHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new RedisHelper();
                        }
                    }
                }
                return instance;
            }
        }

        private Lazy<ConnectionMultiplexer> GetManager()
        {
            if (ConnectString.IndexOf("abortConnect", StringComparison.OrdinalIgnoreCase) < 0)
            {
                ConnectString = ConnectString + ",connectTimeout=5000,responseTimeout=5000,syncTimeout=5000,abortConnect=false";
            }

            return new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(ConnectString));
        }

        private Lazy<ConnectionMultiplexer> CreateManager
        {
            get
            {
                if (_lazyConnection == null || !_lazyConnection.IsValueCreated)
                {
                    lock (MultiplexerLock)
                    {
                        if (_lazyConnection != null && _lazyConnection.IsValueCreated) return _lazyConnection;

                        _lazyConnection = GetManager();

                        //注册如下事件
                        _lazyConnection.Value.ConnectionFailed += MuxerConnectionFailed;
                        _lazyConnection.Value.ConnectionRestored += MuxerConnectionRestored;
                        _lazyConnection.Value.ErrorMessage += MuxerErrorMessage;
                        _lazyConnection.Value.ConfigurationChanged += MuxerConfigurationChanged;
                        _lazyConnection.Value.HashSlotMoved += MuxerHashSlotMoved;
                        _lazyConnection.Value.InternalError += MuxerInternalError;

                        return _lazyConnection;
                    }
                }

                return _lazyConnection;
            }
        }

        /// <summary>
        /// 测试是否可以连接
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<bool, string> IsConnected()
        {
            var msg = "";
            try
            {
                Instance.Write("redis_isconnected", "ok");
                if (Instance.Read("redis_isconnected").ToString() == "ok")
                    return new KeyValuePair<bool, string>(true, "ok");
            }
            catch (Exception e)
            {
                msg = e.Message;
            }
            return new KeyValuePair<bool, string>(false, msg);
        }

        /// <summary>
        /// 获取Redis Database实例，public，支持高级DIY
        /// </summary>
        public IDatabase GetDb(int dbIndex = 0)
        {
            if (dbIndex < 0 || dbIndex > 15)
                dbIndex = 0;
            return CreateManager.Value.GetDatabase(dbIndex);
        }

        #endregion

        #region Redis Events

        /// <summary>
        /// 配置更改时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerConfigurationChanged(object sender, EndPointEventArgs e)
        {
            //Logger.Instance.WriteLog("Redis.MuxerConfigurationChanged", "Configuration changed: " + e.EndPoint);
        }
        /// <summary>
        /// 发生错误时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerErrorMessage(object sender, RedisErrorEventArgs e)
        {
            //Logger.Instance.WriteLog("Redis.MuxerErrorMessage", "ErrorMessage: " + e.Message);
        }
        /// <summary>
        /// 重新建立连接之前的错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            // Logger.Instance.WriteLog("Redis.MuxerConnectionRestored", "ConnectionRestored: " + e.EndPoint);
        }
        /// <summary>
        /// 连接失败 ， 如果重新连接成功你将不会收到这个通知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            // Logger.Instance.WriteLog("Redis.MuxerConnectionFailed", "重新连接Endpoint failed: " + e.EndPoint + ", " + e.FailureType + (e.Exception == null ? "" : (", " + e.Exception.Message)));
        }
        /// <summary>
        /// 更改集群
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerHashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            // Logger.Instance.WriteLog("Redis.MuxerHashSlotMoved", "HashSlotMoved:NewEndPoint" + e.NewEndPoint + ", OldEndPoint" + e.OldEndPoint);
        }
        /// <summary>
        /// Redis错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerInternalError(object sender, InternalErrorEventArgs e)
        {
            // Logger.Instance.WriteLog("Redis.MuxerInternalError", "InternalError:Message" + e.Exception.Message);
        }

        #endregion

        #region Redis Distributed Lock

        public bool Lock(string value)
        {
            var flag = GetDb().StringSet("redis_distributed_lock", value, TimeSpan.FromSeconds(900), When.NotExists, CommandFlags.None); //如果存在了返回false,不存在才返回true;
            GetDb().KeyExpire("redis_distributed_lock", TimeSpan.FromSeconds(10));
            return flag;
        }

        public bool UnLock()
        {
            return GetDb().KeyDelete("redis_distributed_lock");
        }

        public string CurrentLockValue()
        {
            return Read<string>("redis_distributed_lock");
        }

        public bool Lock(string key, string value)
        {
            var flag = GetDb().StringSet(key, value, TimeSpan.FromSeconds(900), When.NotExists, CommandFlags.None); //如果存在了返回false,不存在才返回true;
            GetDb().KeyExpire(key, TimeSpan.FromSeconds(10));
            return flag;
        }

        public bool UnLock(string key)
        {
            return GetDb().KeyDelete(key);
        }

        public string CurrentLockValue(string key)
        {
            return Read<string>(key);
        }

        #endregion

        #region Redis Cache

        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exists(string key)
        {
            try
            {
                return GetDb().KeyExists(key);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 查询缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Read<T>(string key)
        {
            if (key.IsEmpty())
                return default(T);
            try
            {
                string str = GetDb().StringGet(key).ToStr(true);
                return str.ToObject<T>();
            }
            catch (Exception e)
            {
                //;
                return default(T);
            }
        }

        /// <summary>
        /// 批量查询缓存
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<string, object> Read(params string[] keys)
        {
            if (keys.Length == 0)
                return new Dictionary<string, object>();
            try
            {
                var rVals = GetDb().StringGet(keys.Select(x => (RedisKey)x).ToArray());
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (rVals[i].IsNull)
                    {
                        dict.Add(keys[i], null);
                    }
                    else
                    {
                        dict.Add(keys[i], rVals[i]);
                    }
                }
                return dict;
            }
            catch (Exception e)
            {
                //;
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 批量查询缓存
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<string, T> Read<T>(params string[] keys)
        {
            if (keys.Length == 0)
                return new Dictionary<string, T>();
            try
            {
                var rVals = GetDb().StringGet(keys.Select(x => (RedisKey)x).ToArray());
                var dict = new Dictionary<string, T>();
                for (int i = 0; i < keys.Length; i++)
                {
                    dict.Add(keys[i], ((string)rVals[i]).ToObject<T>());
                }
                return dict;
            }
            catch (Exception e)
            {
                //;
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 查询缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Read(string key)
        {
            if (key.IsEmpty())
                return null;
            try
            {
                return GetDb().StringGet(key).ToStr(true);
            }
            catch (Exception e)
            {
                // ;
                return null;
            }
        }

        /// <summary>
        /// 默认时间过期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="t"></param>
        public void Write<T>(string key, T t)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb().StringSet(key, t.ToJson());
            }
            catch (Exception e)
            {
                //;
            }
        }

        /// <summary>
        /// 定时过期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="t"></param>
        /// <param name="expries"></param>
        public void Write<T>(string key, T t, DateTime expries)
        {
            if (key.IsEmpty())
                return;
            var seconds = DateTime.Now.SecondDifference(expries);
            if (seconds < 0)
                seconds = 0;
            try
            {
                GetDb().StringSet(key, t.ToJson(), TimeSpan.FromSeconds(seconds));
            }
            catch (Exception e)
            {
                //;
            }
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb().KeyDelete(key);
            }
            catch (Exception e)
            {
                //;
            }
        }

        ///// <summary>
        ///// 删除缓存
        ///// </summary>
        ///// <param name="pattern">正则</param>
        //public void RemoveByPattern(string pattern)
        //{
        //}

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            var endpoints = CreateManager.Value.GetEndPoints(true);
            foreach (var endpoint in endpoints)
            {
                var server = CreateManager.Value.GetServer(endpoint);
                server.FlushDatabase();
                server.FlushAllDatabases();
            }
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expries"></param>
        /// <returns></returns>
        public bool SetExpire(string key, DateTime expries)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb().KeyExpire(key, expries);
            }
            catch (Exception e)
            {
                // ;
                return false;
            }
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public bool SetExpire(string key, int seconds)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb().KeyExpire(key, DateTime.Now.AddSeconds(seconds));
            }
            catch (Exception e)
            {
                //;
                return false;
            }
        }

        /// <summary>
        /// Key重命名
        /// </summary>
        /// <param name="oldKey"></param>
        /// <param name="newKey"></param>
        /// <returns></returns>
        public bool Rename(string oldKey, string newKey)
        {
            if (oldKey.IsEmpty() || newKey.IsEmpty())
                return false;
            try
            {
                return GetDb().KeyRename(oldKey, newKey);
            }
            catch (Exception e)
            {
                //;
                return false;
            }
        }

        #endregion

        #region Redis String

        /// <summary>
        /// 递增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">递增数值</param>
        /// <returns></returns>
        public long StringIncrement(string key, long value)
        {
            try
            {
                return GetDb().StringIncrement(key, value);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 递减
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">递减数值</param>
        /// <returns></returns>
        public long StringDecrement(string key, long value)
        {
            try
            {
                return GetDb().StringDecrement(key, value);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 字符串追加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long StringAppend(string key, string value)
        {
            try
            {
                return GetDb().StringAppend(key, value);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        #endregion

        #region Redis Hash

        /// <summary>
        /// 判断Hash中某项是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool HashExists(string key, string field)
        {
            if (key.IsEmpty() || field.IsEmpty())
                return false;
            try
            {
                return GetDb().HashExists(key, field);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// 向Hash中新增多项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public void HashSet<T>(string key, Dictionary<string, T> dict)
        {
            if (key.IsEmpty() || dict == null)
                return;
            var hash = new HashEntry[dict.Count];
            var i = 0;
            foreach (var v in dict)
            {
                hash[i] = new HashEntry(v.Key, v.Value.ToJson());
                i++;
            }
            try
            {
                GetDb().HashSet(key, hash);
            }
            catch (Exception e)
            {
            }
        }

        /// <summary>
        /// 向Hash中新增一项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HashSet<T>(string key, string field, T value)
        {
            if (key.IsEmpty() || field.IsEmpty())
                return false;
            try
            {
                return GetDb().HashSet(key, field, value.ToJson());
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// 查询Hash中某项值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public T HashGet<T>(string key, string field)
        {
            if (key.IsEmpty() || field.IsEmpty())
                return default(T);
            try
            {
                string value = GetDb().HashGet(key, field);
                return value.ToObject<T>();
            }
            catch (Exception e)
            {
                return default(T);
            }
        }

        /// <summary>
        /// 查询Hash中多项的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, object> HashGet(string key, params string[] fields)
        {
            if (key.IsEmpty() || fields.Length == 0)
                return new Dictionary<string, object>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, object>();
                var vals = GetDb().HashGet(key, fields.Select(x => (RedisValue)x).ToArray());
                var dict = new Dictionary<string, object>();
                for (var i = 0; i < fields.Length; i++)
                {
                    if (vals[i].IsNull)
                    {
                        dict.Add(fields[i], null);
                    }
                    else
                    {
                        dict.Add(fields[i], vals[i]);
                    }
                }
                return dict;
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 查询Hash中多项的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, T> HashGet<T>(string key, params string[] fields)
        {
            if (key.IsEmpty() || fields.Length == 0)
                return new Dictionary<string, T>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, T>();
                var vals = GetDb().HashGet(key, fields.Select(x => (RedisValue)x).ToArray());
                var dict = new Dictionary<string, T>();
                for (var i = 0; i < fields.Length; i++)
                {
                    dict.Add(fields[i], vals[i].IsNull ? default(T) : ((string)vals[i]).ToObject<T>());
                }
                return dict;
            }
            catch (Exception e)
            {
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 查询Hash所有项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<string, object> HashGet(string key)
        {
            if (key.IsEmpty())
                return new Dictionary<string, object>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, object>();
                var hash = GetDb().HashGetAll(key);
                return hash.ToDictionary<HashEntry, string, object>(v => v.Name, v => v.Value);
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 查询Hash所有项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<string, T> HashGet<T>(string key)
        {
            if (key.IsEmpty())
                return new Dictionary<string, T>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, T>();
                var hash = GetDb().HashGetAll(key);
                return hash.ToDictionary<HashEntry, string, T>(v => v.Name, v => ((string)v.Value).ToObject<T>());
            }
            catch (Exception e)
            {
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 查询Hash共有多少项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long HashLength(string key)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb().HashLength(key);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 查询Hash所有项名称
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string> HashFields(string key)
        {
            if (key.IsEmpty())
                return new List<string>();
            try
            {
                return GetDb().HashKeys(key).Select(v => (string)v).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 查询Hash所有项名称
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<T> HashFields<T>(string key)
        {
            if (key.IsEmpty())
                return new List<T>();
            try
            {
                return GetDb().HashKeys(key).Select(v => ((string)v).ToObject<T>()).ToList();
            }
            catch (Exception e)
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// 删除Hash中某项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool HashDelete(string key, string field)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb().HashDelete(key, field);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        /// <summary>
        /// Hash中某项递增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value">递增数值</param>
        /// <returns></returns>
        public long HashIncrement(string key, string field, long value)
        {
            try
            {
                return GetDb().HashIncrement(key, field, value);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// Hash中某项递减
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value">递减数值</param>
        /// <returns></returns>
        public long HashDecrement(string key, string field, long value)
        {
            try
            {
                return GetDb().HashDecrement(key, field, value);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        #endregion

        #region Redis Lists

        /// <summary>
        /// 列表左侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListLeftPush(string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb().ListLeftPush(key, items[0]);

                return GetDb().ListLeftPush(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 列表左侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListLeftPush<T>(string key, params T[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb().ListLeftPush(key, items[0].ToJson());

                return GetDb().ListLeftPush(key, items.Select(x => (RedisValue)x.ToJson()).ToArray());
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 列表右侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListRightPush(string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb().ListRightPush(key, items[0]);

                return GetDb().ListRightPush(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 列表右侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListRightPush<T>(string key, params T[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb().ListRightPush(key, items[0].ToJson());

                return GetDb().ListRightPush(key, items.Select(x => (RedisValue)x.ToJson()).ToArray());
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 列表左侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public string ListLeftPop(string key)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb().ListLeftPop(key);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 列表左侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public T ListLeftPop<T>(string key)
        {
            if (key.IsEmpty())
                return default(T);
            try
            {
                return ((string)GetDb().ListLeftPop(key)).ToObject<T>();
            }
            catch (Exception e)
            {
                ;
                return default(T);
            }
        }

        /// <summary>
        /// 列表右侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public string ListRightPop(string key)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb().ListRightPop(key);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 列表右侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public T ListRightPop<T>(string key)
        {
            if (key.IsEmpty())
                return default(T);
            try
            {
                return ((string)GetDb().ListRightPop(key)).ToObject<T>();
            }
            catch (Exception e)
            {
                ;
                return default(T);
            }
        }

        /// <summary>
        /// 列表中在item1左边插入item2
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns>队列长度</returns>
        public long ListInsertBefore(string key, string item1, string item2)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb().ListInsertBefore(key, item1, item2);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表中在item1右边插入item2
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns>队列长度</returns>
        public long ListInsertAfter(string key, string item1, string item2)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb().ListInsertAfter(key, item1, item2);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表设置指定索引的元素值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index">大于等于0 左边开始索引，小于等于-1 从右边开始索引 </param>
        /// <param name="item"></param>
        public void ListSetByIndex(string key, long index, string item)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb().ListSetByIndex(key, index, item);
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 列表获取指定索引的元素值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index">大于等于0 左边开始索引，小于等于-1 从右边开始索引 </param>
        /// <returns></returns>
        public string ListGetByIndex(string key, long index)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb().ListGetByIndex(key, index);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 根据索引范围返回列表，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <returns></returns>
        public List<string> ListRange(string key, long start = 0, long stop = -1)
        {
            if (key.IsEmpty())
                return new List<string>();
            try
            {
                return GetDb().ListRange(key, start, stop).Select(v => (string)v).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据索引范围返回列表，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <returns></returns>
        public List<T> ListRange<T>(string key, long start = 0, long stop = -1)
        {
            if (key.IsEmpty())
                return new List<T>();
            try
            {
                return GetDb().ListRange(key, start, stop).Select(v => ((string)v).ToObject<T>()).ToList();
            }
            catch (Exception e)
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// 只保留列表中指定的片段
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        public void ListTrim(string key, long start, long stop)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb().ListTrim(key, start, stop);
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 删除列表中前count个item
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="count">大于0，从左边开始删除；小于0，从右边开始删除；等于0，删除所有</param>
        /// <returns>返回删除数量</returns>
        public long ListRemove(string key, string item, long count)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb().ListRemove(key, item, count);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long ListLength(string key)
        {
            if (key.IsEmpty())
                return 0;
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb().ListLength(key);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        #endregion

        #region Redis Sets

        /// <summary>
        /// 集合新增项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public long SetAdd(string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;

            if (items.Length == 1)
                return GetDb().SetAdd(key, items[0]) ? 1 : 0;

            try
            {
                return GetDb().SetAdd(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合删除项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public long SetRemove(string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;

            if (items.Length == 1)
                return GetDb().SetRemove(key, items[0]) ? 1 : 0;

            try
            {
                return GetDb().SetRemove(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 随机从集合弹出一项
        /// </summary>
        /// <param name="key"></param>
        /// <returns>被弹出的item</returns>
        public string SetPop(string key)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb().SetPop(key);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 集合间转移item
        /// </summary>
        /// <param name="fromKey"></param>
        /// <param name="toKey"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetMove(string fromKey, string toKey, string item)
        {
            if (fromKey.IsEmpty() || toKey.IsEmpty())
                return false;
            try
            {
                return GetDb().SetMove(fromKey, toKey, item);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 随机返回集合count个项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="count">大于0，返回不重复的count个元素；小于0，返回可能重复的|count|个元素</param>
        /// <returns></returns>
        public List<string> SetRandomMembers(string key, long count = 1)
        {
            if (key.IsEmpty() || count == 0)
                return new List<string>();
            try
            {
                return GetDb().SetRandomMembers(key, count).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取集合所有项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string> SetMembers(string key)
        {
            if (key.IsEmpty())
                return new List<string>();

            try
            {
                return GetDb().SetMembers(key).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 查询集合某项是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetContains(string key, string item)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb().SetContains(key, item);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 查询集合长度
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long SetLength(string key)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb().SetLength(key);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合求差集(A-B，返回A中存在B中不存在的元素；若有C，A和B的差集在和C求差集)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<string> SetDifference(params string[] keys)
        {
            if (keys.Length < 2)
                return new List<string>();
            try
            {
                return GetDb().SetCombine(SetOperation.Difference, keys.Select(x => (RedisKey)x).ToArray()).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 集合求差集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public long SetDifferenceStore(string toKey, params string[] keys)
        {
            if (toKey.IsEmpty() || keys.Length < 2)
                return 0;
            try
            {
                return GetDb().SetCombineAndStore(SetOperation.Difference, toKey, keys.Select(x => (RedisKey)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合求交集(A∩B，返回A中存在且B中也存在的元素；若有C，A和B的交集在和C求交集)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<string> SetIntersect(params string[] keys)
        {
            if (keys.Length < 2)
                return new List<string>();
            try
            {
                return GetDb().SetCombine(SetOperation.Intersect, keys.Select(x => (RedisKey)x).ToArray()).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 集合求交集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public long SetIntersectStore(string toKey, params string[] keys)
        {
            if (toKey.IsEmpty() || keys.Length < 2)
                return 0;
            try
            {
                return GetDb().SetCombineAndStore(SetOperation.Intersect, toKey, keys.Select(x => (RedisKey)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合求并集(A∪B，返回A中存在或B中也存在的去重的元素；若有C，A和B的并集在和C求并集)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<string> SetUnion(params string[] keys)
        {
            if (keys.Length < 2)
                return new List<string>();
            try
            {
                return GetDb().SetCombine(SetOperation.Union, keys.Select(x => (RedisKey)x).ToArray()).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 集合求并集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public long SetUnionStore(string toKey, params string[] keys)
        {
            if (toKey.IsEmpty() || keys.Length < 2)
                return 0;
            try
            {
                return GetDb().SetCombineAndStore(SetOperation.Union, toKey, keys.Select(x => (RedisKey)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        #endregion

        #region Redis Sorted Sets

        /// <summary>
        /// 有序集合新增项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="score">分数</param>
        /// <returns></returns>
        public bool SortedSetAdd(string key, string item, double score)
        {
            if (key.IsEmpty())
                return false;

            try
            {
                return GetDb().SortedSetAdd(key, item, score);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 有序集合新增项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dict">item;score</param>
        /// <returns></returns>
        public long SortedSetAdd(string key, Dictionary<string, double> dict)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb().SortedSetAdd(key, dict.Select(x => new SortedSetEntry(x.Key, x.Value)).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取有序集合某项的分数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public double? SortedSetScore(string key, string item)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb().SortedSetScore(key, item);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 增加有序集合元素分数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public double SortedSetIncrement(string key, string item, double score)
        {
            if (key.IsEmpty() || item.IsEmpty())
                return 0;

            try
            {
                return GetDb().SortedSetIncrement(key, item, score);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 减少有序集合元素分数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public double SortedSetDecrement(string key, string item, double score)
        {
            if (key.IsEmpty() || item.IsEmpty())
                return 0;

            try
            {
                return GetDb().SortedSetDecrement(key, item, score);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取有序集合长度
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minScore">最小分值</param>
        /// <param name="maxScore">最大分值</param>
        /// <returns></returns>
        public double SortedSetLength(string key, double? minScore, double? maxScore)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                if (!minScore.HasValue || !maxScore.HasValue)
                    return GetDb().SortedSetLength(key);

                return GetDb().SortedSetLength(key, (double)minScore, (double)maxScore);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取有序集合长度
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minItem"></param>
        /// <param name="maxItem"></param>
        /// <returns></returns>
        public double SortedSetLengthByValue(string key, string minItem, string maxItem)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb().SortedSetLengthByValue(key, minItem, minItem);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 有序集合求差集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="dict">item;weigth 乘法因子,默认传1</param>
        /// <returns></returns>
        public long SortedSetDifferenceStore(string toKey, Dictionary<string, double> dict)
        {
            if (toKey.IsEmpty() || dict.Count < 2)
                return 0;

            try
            {
                return GetDb().SortedSetCombineAndStore(SetOperation.Difference, toKey, dict.Select(x => (RedisKey)x.Key).ToArray(), dict.Select(x => x.Value).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 有序集合求交集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="dict">item;weigth 乘法因子,默认传1</param>
        /// <returns></returns>
        public long SortedSetIntersectStore(string toKey, Dictionary<string, double> dict)
        {
            if (toKey.IsEmpty() || dict.Count < 2)
                return 0;

            try
            {
                return GetDb().SortedSetCombineAndStore(SetOperation.Intersect, toKey, dict.Select(x => (RedisKey)x.Key).ToArray(), dict.Select(x => x.Value).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 有序集合求并集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="dict">item;weigth 乘法因子,默认传1</param>
        /// <returns></returns>
        public long SortedSetUnionStore(string toKey, Dictionary<string, double> dict)
        {
            if (toKey.IsEmpty() || dict.Count < 2)
                return 0;

            try
            {
                return GetDb().SortedSetCombineAndStore(SetOperation.Union, toKey, dict.Select(x => (RedisKey)x.Key).ToArray(), dict.Select(x => x.Value).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 根据索引范围返回有序集合，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <returns></returns>
        public List<string> SortedSetRangeByRank(string key, long start = 0, long stop = -1, string orderType = "asc")
        {
            if (key.IsEmpty() || (orderType != "asc" && orderType != "desc"))
                return new List<string>();

            try
            {
                return GetDb().SortedSetRangeByRank(key, start, stop, orderType == "asc" ? Order.Ascending : Order.Descending).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据索引范围返回有序集合，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <returns>item;score</returns>
        public Dictionary<string, double> SortedSetRangeByRankWithScores(string key, long start = 0, long stop = -1, string orderType = "asc")
        {
            if (key.IsEmpty() || (orderType != "asc" && orderType != "desc"))
                return new Dictionary<string, double>();

            try
            {
                return GetDb().SortedSetRangeByRankWithScores(key, start, stop, orderType == "asc" ? Order.Ascending : Order.Descending).ToDictionary<SortedSetEntry, string, double>(x => x.Element, x => x.Score);
            }
            catch (Exception e)
            {
                return new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// 根据分值范围返回有序集合
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minScore"></param>
        /// <param name="maxScore"></param>
        /// <param name="includeType">0 闭区间，1 开区间</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public List<string> SortedSetRangeByScore(string key, double minScore = -1.0 / 0.0, double maxScore = 1.0 / 0.0, int includeType = 0, string orderType = "asc", long skip = 0, long take = -1)
        {
            if (key.IsEmpty() || includeType < 0 || includeType > 1 || (orderType != "asc" && orderType != "desc"))
                return new List<string>();

            try
            {
                return GetDb().SortedSetRangeByScore(key, minScore, maxScore, includeType == 0 ? Exclude.None : Exclude.Both, orderType == "asc" ? Order.Ascending : Order.Descending, skip, take).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据分值范围返回有序集合
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minScore"></param>
        /// <param name="maxScore"></param>
        /// <param name="includeType">0 闭区间，1 开区间</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns>item;score</returns>
        public Dictionary<string, double> SortedSetRangeByScoreWithScores(string key, double minScore = -1.0 / 0.0, double maxScore = 1.0 / 0.0, int includeType = 0, string orderType = "asc", long skip = 0, long take = -1)
        {
            if (key.IsEmpty() || includeType < 0 || includeType > 1 || (orderType != "asc" && orderType != "desc"))
                return new Dictionary<string, double>();

            try
            {
                return GetDb().SortedSetRangeByScoreWithScores(key, minScore, maxScore, includeType == 0 ? Exclude.None : Exclude.Both, orderType == "asc" ? Order.Ascending : Order.Descending).ToDictionary<SortedSetEntry, string, double>(x => x.Element, x => x.Score);
            }
            catch (Exception e)
            {
                return new Dictionary<string, double>();
            }
        }

        #endregion

        #region Redis Distributed Lock With DbIndex

        //Redis 分布式锁使用示例
        //var redis = RedisHelper.Instance;
        //var clientId = Guid.NewGuid().ToString();
        //var isSuccess = redis.Lock(clientId);

        //if (isSuccess)
        //{
        //    try
        //    {
        //        //业务操作
        //    }
        //    finally
        //    {

        //        if (clientId.Equals(redis.CurrentLockValue()))
        //        {
        //            //执行过程中异常，释放锁
        //            UnLock();
        //        }
        //    }
        //}
        //else
        //{
        //    msg = "资源正忙,请刷新后重试";
        //}

        public bool Lock(int dbIndex, string value)
        {
            var flag = GetDb(dbIndex).StringSet("redis_distributed_lock", value, TimeSpan.FromSeconds(900), When.NotExists, CommandFlags.None); //如果存在了返回false,不存在才返回true;
            GetDb(dbIndex).KeyExpire("redis_distributed_lock", TimeSpan.FromSeconds(10));
            return flag;
        }

        public bool UnLock(int dbIndex)
        {
            return GetDb(dbIndex).KeyDelete("redis_distributed_lock");
        }

        public string CurrentLockValue(int dbIndex)
        {
            return Read<string>(dbIndex, "redis_distributed_lock");
        }

        public bool Lock(int dbIndex, string key, string value)
        {
            var flag = GetDb(dbIndex).StringSet(key, value, TimeSpan.FromSeconds(900), When.NotExists, CommandFlags.None); //如果存在了返回false,不存在才返回true;
            GetDb(dbIndex).KeyExpire(key, TimeSpan.FromSeconds(10));
            return flag;
        }

        public bool UnLock(int dbIndex, string key)
        {
            return GetDb(dbIndex).KeyDelete(key);
        }

        public string CurrentLockValue(int dbIndex, string key)
        {
            return Read<string>(dbIndex, key);
        }

        #endregion

        #region Redis Cache With DbIndex

        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exists(int dbIndex, string key)
        {
            try
            {
                return GetDb(dbIndex).KeyExists(key);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 查询缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Read<T>(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return default(T);
            try
            {
                string str = GetDb(dbIndex).StringGet(key).ToStr(true);
                return str.ToObject<T>();
            }
            catch (Exception e)
            {
                ;
                return default(T);
            }
        }

        /// <summary>
        /// 批量查询缓存
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<string, object> Read(int dbIndex, params string[] keys)
        {
            if (keys.Length == 0)
                return new Dictionary<string, object>();
            try
            {
                var rVals = GetDb(dbIndex).StringGet(keys.Select(x => (RedisKey)x).ToArray());
                var dict = new Dictionary<string, object>();
                for (int i = 0; i < keys.Length; i++)
                {
                    if (rVals[i].IsNull)
                    {
                        dict.Add(keys[i], null);
                    }
                    else
                    {
                        dict.Add(keys[i], rVals[i]);
                    }
                }
                return dict;
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 批量查询缓存
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public IDictionary<string, T> Read<T>(int dbIndex, params string[] keys)
        {
            if (keys.Length == 0)
                return new Dictionary<string, T>();
            try
            {
                var rVals = GetDb(dbIndex).StringGet(keys.Select(x => (RedisKey)x).ToArray());
                var dict = new Dictionary<string, T>();
                for (int i = 0; i < keys.Length; i++)
                {
                    dict.Add(keys[i], ((string)rVals[i]).ToObject<T>());
                }
                return dict;
            }
            catch (Exception e)
            {
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 查询缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Read(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return null;
            try
            {
                return GetDb(dbIndex).StringGet(key).ToStr(true);
            }
            catch (Exception e)
            {
                ;
                return null;
            }
        }

        /// <summary>
        /// 默认时间过期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="t"></param>
        public void Write<T>(int dbIndex, string key, T t)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb(dbIndex).StringSet(key, t.ToJson());
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 定时过期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="t"></param>
        /// <param name="expries"></param>
        public void Write<T>(int dbIndex, string key, T t, DateTime expries)
        {
            if (key.IsEmpty())
                return;
            var seconds = DateTime.Now.SecondDifference(expries);
            if (seconds < 0)
                seconds = 0;
            try
            {
                GetDb(dbIndex).StringSet(key, t.ToJson(), TimeSpan.FromSeconds(seconds));
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key"></param>
        public void Remove(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb(dbIndex).KeyDelete(key);
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expries"></param>
        /// <returns></returns>
        public bool SetExpire(int dbIndex, string key, DateTime expries)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).KeyExpire(key, expries);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public bool SetExpire(int dbIndex, string key, int seconds)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).KeyExpire(key, DateTime.Now.AddSeconds(seconds));
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// Key重命名
        /// </summary>
        /// <param name="oldKey"></param>
        /// <param name="newKey"></param>
        /// <returns></returns>
        public bool Rename(int dbIndex, string oldKey, string newKey)
        {
            if (oldKey.IsEmpty() || newKey.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).KeyRename(oldKey, newKey);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        #endregion

        #region Redis String With DbIndex

        /// <summary>
        /// 递增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">递增数值</param>
        /// <returns></returns>
        public long StringIncrement(int dbIndex, string key, long value)
        {
            try
            {
                return GetDb(dbIndex).StringIncrement(key, value);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 递减
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">递减数值</param>
        /// <returns></returns>
        public long StringDecrement(int dbIndex, string key, long value)
        {
            try
            {
                return GetDb(dbIndex).StringDecrement(key, value);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 字符串追加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long StringAppend(int dbIndex, string key, string value)
        {
            try
            {
                return GetDb(dbIndex).StringAppend(key, value);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        #endregion

        #region Redis Hash With DbIndex

        /// <summary>
        /// 判断Hash中某项是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool HashExists(int dbIndex, string key, string field)
        {
            if (key.IsEmpty() || field.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).HashExists(key, field);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 向Hash中新增多项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public void HashSet<T>(int dbIndex, string key, Dictionary<string, T> dict)
        {
            if (key.IsEmpty() || dict == null)
                return;
            var hash = new HashEntry[dict.Count];
            var i = 0;
            foreach (var v in dict)
            {
                hash[i] = new HashEntry(v.Key, v.Value.ToJson());
                i++;
            }
            try
            {
                GetDb(dbIndex).HashSet(key, hash);
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 向Hash中新增一项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HashSet<T>(int dbIndex, string key, string field, T value)
        {
            if (key.IsEmpty() || field.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).HashSet(key, field, value.ToJson());
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 查询Hash中某项值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public T HashGet<T>(int dbIndex, string key, string field)
        {
            if (key.IsEmpty() || field.IsEmpty())
                return default(T);
            try
            {
                string value = GetDb(dbIndex).HashGet(key, field);
                return value.ToObject<T>();
            }
            catch (Exception e)
            {
                ;
                return default(T);
            }
        }

        /// <summary>
        /// 查询Hash中多项的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, object> HashGet(int dbIndex, string key, params string[] fields)
        {
            if (key.IsEmpty() || fields.Length == 0)
                return new Dictionary<string, object>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, object>();
                var vals = GetDb(dbIndex).HashGet(key, fields.Select(x => (RedisValue)x).ToArray());
                var dict = new Dictionary<string, object>();
                for (var i = 0; i < fields.Length; i++)
                {
                    if (vals[i].IsNull)
                    {
                        dict.Add(fields[i], null);
                    }
                    else
                    {
                        dict.Add(fields[i], vals[i]);
                    }
                }
                return dict;
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 查询Hash中多项的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public Dictionary<string, T> HashGet<T>(int dbIndex, string key, params string[] fields)
        {
            if (key.IsEmpty() || fields.Length == 0)
                return new Dictionary<string, T>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, T>();
                var vals = GetDb(dbIndex).HashGet(key, fields.Select(x => (RedisValue)x).ToArray());
                var dict = new Dictionary<string, T>();
                for (var i = 0; i < fields.Length; i++)
                {
                    dict.Add(fields[i], vals[i].IsNull ? default(T) : ((string)vals[i]).ToObject<T>());
                }
                return dict;
            }
            catch (Exception e)
            {
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 查询Hash所有项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<string, object> HashGet(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return new Dictionary<string, object>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, object>();
                var hash = GetDb(dbIndex).HashGetAll(key);
                return hash.ToDictionary<HashEntry, string, object>(v => v.Name, v => v.Value);
            }
            catch (Exception e)
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 查询Hash所有项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Dictionary<string, T> HashGet<T>(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return new Dictionary<string, T>();
            try
            {
                if (!Exists(key))
                    return new Dictionary<string, T>();
                var hash = GetDb(dbIndex).HashGetAll(key);
                return hash.ToDictionary<HashEntry, string, T>(v => v.Name, v => ((string)v.Value).ToObject<T>());
            }
            catch (Exception e)
            {
                return new Dictionary<string, T>();
            }
        }

        /// <summary>
        /// 查询Hash共有多少项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long HashLength(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb(dbIndex).HashLength(key);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// 查询Hash所有项名称
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string> HashFields(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return new List<string>();
            try
            {
                return GetDb(dbIndex).HashKeys(key).Select(v => (string)v).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 查询Hash所有项名称
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<T> HashFields<T>(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return new List<T>();
            try
            {
                return GetDb(dbIndex).HashKeys(key).Select(v => ((string)v).ToObject<T>()).ToList();
            }
            catch (Exception e)
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// 删除Hash中某项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool HashDelete(int dbIndex, string key, string field)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).HashDelete(key, field);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// Hash中某项递增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value">递增数值</param>
        /// <returns></returns>
        public long HashIncrement(int dbIndex, string key, string field, long value)
        {
            try
            {
                return GetDb(dbIndex).HashIncrement(key, field, value);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// Hash中某项递减
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value">递减数值</param>
        /// <returns></returns>
        public long HashDecrement(int dbIndex, string key, string field, long value)
        {
            try
            {
                return GetDb(dbIndex).HashDecrement(key, field, value);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        #endregion

        #region Redis Lists With DbIndex

        /// <summary>
        /// 列表左侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListLeftPush(int dbIndex, string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb(dbIndex).ListLeftPush(key, items[0]);

                return GetDb(dbIndex).ListLeftPush(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表左侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListLeftPush<T>(int dbIndex, string key, params T[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb(dbIndex).ListLeftPush(key, items[0].ToJson());

                return GetDb(dbIndex).ListLeftPush(key, items.Select(x => (RedisValue)x.ToJson()).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表右侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListRightPush(int dbIndex, string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb(dbIndex).ListRightPush(key, items[0]);

                return GetDb(dbIndex).ListRightPush(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表右侧添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items">List集合需要ToArray()</param>
        /// <returns>列表长度</returns>
        public long ListRightPush<T>(int dbIndex, string key, params T[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;
            try
            {
                if (items.Length == 1)
                    return GetDb(dbIndex).ListRightPush(key, items[0].ToJson());

                return GetDb(dbIndex).ListRightPush(key, items.Select(x => (RedisValue)x.ToJson()).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表左侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public string ListLeftPop(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb(dbIndex).ListLeftPop(key);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 列表左侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public T ListLeftPop<T>(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return default(T);
            try
            {
                return ((string)GetDb(dbIndex).ListLeftPop(key)).ToObject<T>();
            }
            catch (Exception e)
            {
                ;
                return default(T);
            }
        }

        /// <summary>
        /// 列表右侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public string ListRightPop(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb(dbIndex).ListRightPop(key);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 列表右侧删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns>返回弹出值</returns>
        public T ListRightPop<T>(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return default(T);
            try
            {
                return ((string)GetDb(dbIndex).ListRightPop(key)).ToObject<T>();
            }
            catch (Exception e)
            {
                ;
                return default(T);
            }
        }

        /// <summary>
        /// 列表中在item1左边插入item2
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns>队列长度</returns>
        public long ListInsertBefore(int dbIndex, string key, string item1, string item2)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb(dbIndex).ListInsertBefore(key, item1, item2);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表中在item1右边插入item2
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns>队列长度</returns>
        public long ListInsertAfter(int dbIndex, string key, string item1, string item2)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb(dbIndex).ListInsertAfter(key, item1, item2);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 列表设置指定索引的元素值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index">大于等于0 左边开始索引，小于等于-1 从右边开始索引 </param>
        /// <param name="item"></param>
        public void ListSetByIndex(int dbIndex, string key, long index, string item)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb(dbIndex).ListSetByIndex(key, index, item);
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 列表获取指定索引的元素值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index">大于等于0 左边开始索引，小于等于-1 从右边开始索引 </param>
        /// <returns></returns>
        public string ListGetByIndex(int dbIndex, string key, long index)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb(dbIndex).ListGetByIndex(key, index);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 根据索引范围返回列表，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <returns></returns>
        public List<string> ListRange(int dbIndex, string key, long start = 0, long stop = -1)
        {
            if (key.IsEmpty())
                return new List<string>();
            try
            {
                return GetDb(dbIndex).ListRange(key, start, stop).Select(v => (string)v).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据索引范围返回列表，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <returns></returns>
        public List<T> ListRange<T>(int dbIndex, string key, long start = 0, long stop = -1)
        {
            if (key.IsEmpty())
                return new List<T>();
            try
            {
                return GetDb(dbIndex).ListRange(key, start, stop).Select(v => ((string)v).ToObject<T>()).ToList();
            }
            catch (Exception e)
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// 只保留列表中指定的片段
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        public void ListTrim(int dbIndex, string key, long start, long stop)
        {
            if (key.IsEmpty())
                return;
            try
            {
                GetDb(dbIndex).ListTrim(key, start, stop);
            }
            catch (Exception e)
            {
                ;
            }
        }

        /// <summary>
        /// 删除列表中前count个item
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="count">大于0，从左边开始删除；小于0，从右边开始删除；等于0，删除所有</param>
        /// <returns>返回删除数量</returns>
        public long ListRemove(int dbIndex, string key, string item, long count)
        {
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb(dbIndex).ListRemove(key, item, count);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long ListLength(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return 0;
            if (key.IsEmpty())
                return 0;
            try
            {
                return GetDb(dbIndex).ListLength(key);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        #endregion

        #region Redis Sets With DbIndex

        /// <summary>
        /// 集合新增项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public long SetAdd(int dbIndex, string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;

            if (items.Length == 1)
                return GetDb(dbIndex).SetAdd(key, items[0]) ? 1 : 0;

            try
            {
                return GetDb(dbIndex).SetAdd(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合删除项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public long SetRemove(int dbIndex, string key, params string[] items)
        {
            if (key.IsEmpty() || items.Length == 0)
                return 0;

            if (items.Length == 1)
                return GetDb(dbIndex).SetRemove(key, items[0]) ? 1 : 0;

            try
            {
                return GetDb(dbIndex).SetRemove(key, items.Select(x => (RedisValue)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 随机从集合弹出一项
        /// </summary>
        /// <param name="key"></param>
        /// <returns>被弹出的item</returns>
        public string SetPop(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return "";
            try
            {
                return GetDb(dbIndex).SetPop(key);
            }
            catch (Exception e)
            {
                return "";
            }
        }

        /// <summary>
        /// 集合间转移item
        /// </summary>
        /// <param name="fromKey"></param>
        /// <param name="toKey"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetMove(int dbIndex, string fromKey, string toKey, string item)
        {
            if (fromKey.IsEmpty() || toKey.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).SetMove(fromKey, toKey, item);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 随机返回集合count个项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="count">大于0，返回不重复的count个元素；小于0，返回可能重复的|count|个元素</param>
        /// <returns></returns>
        public List<string> SetRandomMembers(int dbIndex, string key, long count = 1)
        {
            if (key.IsEmpty() || count == 0)
                return new List<string>();
            try
            {
                return GetDb(dbIndex).SetRandomMembers(key, count).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取集合所有项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string> SetMembers(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return new List<string>();

            try
            {
                return GetDb(dbIndex).SetMembers(key).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 查询集合某项是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetContains(int dbIndex, string key, string item)
        {
            if (key.IsEmpty())
                return false;
            try
            {
                return GetDb(dbIndex).SetContains(key, item);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 查询集合长度
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long SetLength(int dbIndex, string key)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb(dbIndex).SetLength(key);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合求差集(A-B，返回A中存在B中不存在的元素；若有C，A和B的差集在和C求差集)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<string> SetDifference(int dbIndex, params string[] keys)
        {
            if (keys.Length < 2)
                return new List<string>();
            try
            {
                return GetDb(dbIndex).SetCombine(SetOperation.Difference, keys.Select(x => (RedisKey)x).ToArray()).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 集合求差集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public long SetDifferenceStore(int dbIndex, string toKey, params string[] keys)
        {
            if (toKey.IsEmpty() || keys.Length < 2)
                return 0;
            try
            {
                return GetDb(dbIndex).SetCombineAndStore(SetOperation.Difference, toKey, keys.Select(x => (RedisKey)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合求交集(A∩B，返回A中存在且B中也存在的元素；若有C，A和B的交集在和C求交集)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<string> SetIntersect(int dbIndex, params string[] keys)
        {
            if (keys.Length < 2)
                return new List<string>();
            try
            {
                return GetDb(dbIndex).SetCombine(SetOperation.Intersect, keys.Select(x => (RedisKey)x).ToArray()).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 集合求交集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public long SetIntersectStore(int dbIndex, string toKey, params string[] keys)
        {
            if (toKey.IsEmpty() || keys.Length < 2)
                return 0;
            try
            {
                return GetDb(dbIndex).SetCombineAndStore(SetOperation.Intersect, toKey, keys.Select(x => (RedisKey)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 集合求并集(A∪B，返回A中存在或B中也存在的去重的元素；若有C，A和B的并集在和C求并集)
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<string> SetUnion(int dbIndex, params string[] keys)
        {
            if (keys.Length < 2)
                return new List<string>();
            try
            {
                return GetDb(dbIndex).SetCombine(SetOperation.Union, keys.Select(x => (RedisKey)x).ToArray()).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {

                return new List<string>();
            }
        }

        /// <summary>
        /// 集合求并集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        public long SetUnionStore(int dbIndex, string toKey, params string[] keys)
        {
            if (toKey.IsEmpty() || keys.Length < 2)
                return 0;
            try
            {
                return GetDb(dbIndex).SetCombineAndStore(SetOperation.Union, toKey, keys.Select(x => (RedisKey)x).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        #endregion

        #region Redis Sorted Sets With DbIndex

        /// <summary>
        /// 有序集合新增项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="score">分数</param>
        /// <returns></returns>
        public bool SortedSetAdd(int dbIndex, string key, string item, double score)
        {
            if (key.IsEmpty())
                return false;

            try
            {
                return GetDb(dbIndex).SortedSetAdd(key, item, score);
            }
            catch (Exception e)
            {
                ;
                return false;
            }
        }

        /// <summary>
        /// 有序集合新增项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dict">item;score</param>
        /// <returns></returns>
        public long SortedSetAdd(int dbIndex, string key, Dictionary<string, double> dict)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetAdd(key, dict.Select(x => new SortedSetEntry(x.Key, x.Value)).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取有序集合某项的分数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public double? SortedSetScore(int dbIndex, string key, string item)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetScore(key, item);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 增加有序集合元素分数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public double SortedSetIncrement(int dbIndex, string key, string item, double score)
        {
            if (key.IsEmpty() || item.IsEmpty())
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetIncrement(key, item, score);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 减少有序集合元素分数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public double SortedSetDecrement(int dbIndex, string key, string item, double score)
        {
            if (key.IsEmpty() || item.IsEmpty())
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetDecrement(key, item, score);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取有序集合长度
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minScore">最小分值</param>
        /// <param name="maxScore">最大分值</param>
        /// <returns></returns>
        public double SortedSetLength(int dbIndex, string key, double? minScore, double? maxScore)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                if (!minScore.HasValue || !maxScore.HasValue)
                    return GetDb(dbIndex).SortedSetLength(key);

                return GetDb(dbIndex).SortedSetLength(key, (double)minScore, (double)maxScore);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 获取有序集合长度
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minItem"></param>
        /// <param name="maxItem"></param>
        /// <returns></returns>
        public double SortedSetLengthByValue(int dbIndex, string key, string minItem, string maxItem)
        {
            if (key.IsEmpty())
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetLengthByValue(key, minItem, minItem);
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 有序集合求差集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="dict">item;weigth 乘法因子,默认传1</param>
        /// <returns></returns>
        public long SortedSetDifferenceStore(int dbIndex, string toKey, Dictionary<string, double> dict)
        {
            if (toKey.IsEmpty() || dict.Count < 2)
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetCombineAndStore(SetOperation.Difference, toKey, dict.Select(x => (RedisKey)x.Key).ToArray(), dict.Select(x => x.Value).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 有序集合求交集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="dict">item;weigth 乘法因子,默认传1</param>
        /// <returns></returns>
        public long SortedSetIntersectStore(int dbIndex, string toKey, Dictionary<string, double> dict)
        {
            if (toKey.IsEmpty() || dict.Count < 2)
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetCombineAndStore(SetOperation.Intersect, toKey, dict.Select(x => (RedisKey)x.Key).ToArray(), dict.Select(x => x.Value).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 有序集合求并集,将结果存到toKey集合中，若toKey已存在则覆盖
        /// </summary>
        /// <param name="toKey"></param>
        /// <param name="dict">item;weigth 乘法因子,默认传1</param>
        /// <returns></returns>
        public long SortedSetUnionStore(int dbIndex, string toKey, Dictionary<string, double> dict)
        {
            if (toKey.IsEmpty() || dict.Count < 2)
                return 0;

            try
            {
                return GetDb(dbIndex).SortedSetCombineAndStore(SetOperation.Union, toKey, dict.Select(x => (RedisKey)x.Key).ToArray(), dict.Select(x => x.Value).ToArray());
            }
            catch (Exception e)
            {
                ;
                return 0;
            }
        }

        /// <summary>
        /// 根据索引范围返回有序集合，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <returns></returns>
        public List<string> SortedSetRangeByRank(int dbIndex, string key, long start = 0, long stop = -1, string orderType = "asc")
        {
            if (key.IsEmpty() || (orderType != "asc" && orderType != "desc"))
                return new List<string>();

            try
            {
                return GetDb(dbIndex).SortedSetRangeByRank(key, start, stop, orderType == "asc" ? Order.Ascending : Order.Descending).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据索引范围返回有序集合，start和stop都不传值，代表获取所有元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="stop">大于等于0 左边开始索引，小于等于-1 从右边开始索引</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <returns>item;score</returns>
        public Dictionary<string, double> SortedSetRangeByRankWithScores(int dbIndex, string key, long start = 0, long stop = -1, string orderType = "asc")
        {
            if (key.IsEmpty() || (orderType != "asc" && orderType != "desc"))
                return new Dictionary<string, double>();

            try
            {
                return GetDb(dbIndex).SortedSetRangeByRankWithScores(key, start, stop, orderType == "asc" ? Order.Ascending : Order.Descending).ToDictionary<SortedSetEntry, string, double>(x => x.Element, x => x.Score);
            }
            catch (Exception e)
            {
                return new Dictionary<string, double>();
            }
        }

        /// <summary>
        /// 根据分值范围返回有序集合
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minScore"></param>
        /// <param name="maxScore"></param>
        /// <param name="includeType">0 闭区间，1 开区间</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public List<string> SortedSetRangeByScore(int dbIndex, string key, double minScore = -1.0 / 0.0, double maxScore = 1.0 / 0.0, int includeType = 0, string orderType = "asc", long skip = 0, long take = -1)
        {
            if (key.IsEmpty() || includeType < 0 || includeType > 1 || (orderType != "asc" && orderType != "desc"))
                return new List<string>();

            try
            {
                return GetDb(dbIndex).SortedSetRangeByScore(key, minScore, maxScore, includeType == 0 ? Exclude.None : Exclude.Both, orderType == "asc" ? Order.Ascending : Order.Descending, skip, take).Select(x => (string)x).ToList();
            }
            catch (Exception e)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据分值范围返回有序集合
        /// </summary>
        /// <param name="key"></param>
        /// <param name="minScore"></param>
        /// <param name="maxScore"></param>
        /// <param name="includeType">0 闭区间，1 开区间</param>
        /// <param name="orderType">排序方式，asc/desc</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns>item;score</returns>
        public Dictionary<string, double> SortedSetRangeByScoreWithScores(int dbIndex, string key, double minScore = -1.0 / 0.0, double maxScore = 1.0 / 0.0, int includeType = 0, string orderType = "asc", long skip = 0, long take = -1)
        {
            if (key.IsEmpty() || includeType < 0 || includeType > 1 || (orderType != "asc" && orderType != "desc"))
                return new Dictionary<string, double>();

            try
            {
                return GetDb(dbIndex).SortedSetRangeByScoreWithScores(key, minScore, maxScore, includeType == 0 ? Exclude.None : Exclude.Both, orderType == "asc" ? Order.Ascending : Order.Descending).ToDictionary<SortedSetEntry, string, double>(x => x.Element, x => x.Score);
            }
            catch (Exception e)
            {
                return new Dictionary<string, double>();
            }
        }

        #endregion
    }
}

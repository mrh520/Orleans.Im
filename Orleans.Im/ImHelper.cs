using Orleans.Im.Common;
using Orleans.Im.Grains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Im
{
    public class ImHelper
    {
        private static IClusterClient _client = (IClusterClient)GlobalVariable.ApplicationServices.GetService(typeof(IClusterClient));


        public async static Task<(bool, string)> SendMessage(Packet packet)
        {
            switch (packet.SendType)
            {
                // 单聊
                case 0:
                    {
                        var grain = _client.GetGrain<IChatGrain>(packet.SendId);
                        var data = await grain.SendMessage(packet);
                    }
                    break;
                // 群聊
                case 1:
                    {
                        // 获取群聊成员
                        var list = RedisHelper.Instance.HashFields(packet.ChanName);
                        var grain = _client.GetGrain<IChatGrain>(packet.SendId);
                        foreach (var receiveId in list)
                        {
                            //不给自己发消息                           
                            if (receiveId == packet.SendId)
                            {
                                continue;
                            }
                            packet.ReceiveId = receiveId;
                            await grain.SendMessage(packet);
                        }
                    }
                    break;
            }
            return await Task.FromResult((true, "ok"));
        }



        /// <summary>
        /// 创建群聊
        /// </summary>
        /// <param name="chanName"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static Task<(bool, string)> CreateChan(string chanName, string clientId)
        {
            //判断chanName是否存在
            if (RedisHelper.Instance.Exists(chanName))
            {
                return Task.FromResult((false, "该名称已存在！"));
            }
            RedisHelper.Instance.HashSet<string>(chanName, clientId, clientId);
            AddUserChan(chanName, clientId);
            return Task.FromResult((true, "创建成功！"));
        }

        /// <summary>
        /// 加入群聊
        /// </summary>
        /// <param name="chanName"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static Task<(bool, string)> JoinChan(string chanName, string clientId)
        {
            // 判断chanName是否存在
            if (RedisHelper.Instance.Exists(chanName))
            {
                RedisHelper.Instance.HashSet<string>(chanName, clientId, clientId);
                AddUserChan(chanName, clientId);
                return Task.FromResult((true, "加入成功！"));
            }
            return Task.FromResult((false, "该群聊不存在！"));
        }

        /// <summary>
        /// 离开群聊
        /// </summary>
        /// <param name="chanName"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static Task<(bool, string)> LeaveChan(string chanName, string clientId)
        {
            if (RedisHelper.Instance.HashDelete(chanName, clientId))
            {
                RemoveUserChan(chanName, clientId);
                return Task.FromResult((true, "离开成功！"));
            }
            return Task.FromResult((true, "离开失败！"));
        }

        /// <summary>
        /// 上线
        /// </summary>
        /// <returns></returns>
        public static void Online()
        {
            RedisHelper.Instance.StringIncrement(Constant.WS_IM_ONLINE, 1);
        }

        /// <summary>
        /// 下线
        /// </summary>
        /// <returns></returns>
        public static void Offline()
        {
            RedisHelper.Instance.StringDecrement(Constant.WS_IM_ONLINE, 1);
        }

        /// <summary>
        /// 添加朋友
        /// </summary>
        /// <returns></returns>
        public static Task AddFriend(string clientId, string friendId)
        {
            var key = Constant.FRIEND + clientId;
            RedisHelper.Instance.HashSet(key, friendId, friendId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 删除朋友
        /// </summary>
        /// <returns></returns>
        public static Task RemoveFriend(string clientId, string friendId)
        {
            var key = Constant.FRIEND + clientId;
            RedisHelper.Instance.HashDelete(key, friendId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取用户朋友
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static Task<List<string>> GetUserFriendList(string clientId)
        {
            var key = Constant.FRIEND + clientId;
            List<string> list = RedisHelper.Instance.HashFields(key);
            return Task.FromResult(list);
        }

        /// <summary>
        /// 获取用户所有频道
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static Task<List<string>> GetUserChanList(string clientId)
        {
            var key = Constant.CHAN + clientId;
            List<string> list = RedisHelper.Instance.HashFields(key);
            return Task.FromResult(list);
        }


        #region 私有方法

        private static void AddUserChan(string chanName, string clientId)
        {
            string key = Constant.CHAN + clientId;
            RedisHelper.Instance.HashSet<string>(key, chanName, chanName);
        }

        private static void RemoveUserChan(string chanName, string clientId)
        {
            string key = Constant.CHAN + clientId;
            RedisHelper.Instance.HashDelete(key, chanName);
        }
        #endregion
    }
}

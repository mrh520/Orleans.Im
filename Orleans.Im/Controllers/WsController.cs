using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Orleans.Im.Common;
using Orleans.Im.Grains;

namespace Orleans.Im.Controllers
{
    [Route("api/[controller]/[action]")]
    public class WsController : Controller
    {
        private readonly IGrainFactory _client;
        public WsController(IGrainFactory client)
        {
            _client = client;
        }

        [HttpPost]
        public async Task<object> SendMessage([FromBody] Packet packet)
        {
            ApiResult<object> result = new ApiResult<object>();
            var grain = _client.GetGrain<IChatGrain>(packet.SendId);
            var (status, msg) = await ImHelper.SendMessage(packet);
            if (!status)
            {
                result.Code = 1;
            }
            result.Msg = msg;
            result.Data = new { };
            return result;
        }

        /// <summary>
        /// 添加朋友
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="friendId"></param>
        /// <returns></returns>
        public async Task<object> AddFriend(string clientId, string friendId)
        {
            ApiResult<object> result = new ApiResult<object>();
            await ImHelper.AddFriend(clientId, friendId);
            result.Msg = "success";
            result.Data = new { };
            return result;
        }

        /// <summary>
        /// 创建群聊
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<object> CreateChan(string chanName, string clientId)
        {
            ApiResult<object> result = new ApiResult<object>();
            var (status, msg) = await ImHelper.CreateChan(chanName, clientId);
            if (!status)
            {
                result.Code = 1;
            }
            result.Msg = msg;
            result.Data = new { };
            return result;
        }

        /// <summary>
        /// 加入群聊
        /// </summary>
        /// <param name="chanName"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<object> JoinChan(string chanName, string clientId)
        {
            ApiResult<object> result = new ApiResult<object>();
            var (status, msg) = await ImHelper.JoinChan(chanName, clientId);
            if (!status)
            {
                result.Code = 1;
            }
            result.Msg = msg;
            result.Data = new { };
            return result;
        }

        /// <summary>
        /// 获取用户群聊列表
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<object> GetUserChanList(string clientId)
        {
            ApiResult<object> result = new ApiResult<object>();
            var data = await ImHelper.GetUserChanList(clientId);
            result.Msg = "success";
            result.Data = data;
            return result;
        }

        /// <summary>
        /// 获取用户朋友列表
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async Task<object> GetUserFriendList(string clientId)
        {
            ApiResult<object> result = new ApiResult<object>();
            var data = await ImHelper.GetUserFriendList(clientId);
            result.Msg = "success";
            result.Data = data;
            return result;
        }

        public object GetList()
        {
            var result = new ApiResult<IM_List>();
            result.Code = 0;
            result.Data = new IM_List
            {
                mine = new IM_User
                {
                    id = "565f2b2d-661b-4e8f-8c32-ffc2b32e49fc",
                    username = "闵仁辉",
                    status = "online",
                    sign = "一个开心的开发者",
                    avatar = "http://cdn.firstlinkapp.com/upload/2016_6/1465575923433_33812.jpg"
                },
                friend = new List<IM_Friend>
                {

                    new IM_Friend
                    {
                        groupname = "我的好友",
                        id = 1,
                        online = 2,
                        list = new List<IM_User>
                        {
                            new IM_User
                            {
                                id = "565f2b2d-661b-4e8f-8c32-ffc2b32e49fc",
                                username = "闵仁辉",
                                status = "online",
                                sign = "一个开心的开发者",
                                avatar = "http://cdn.firstlinkapp.com/upload/2016_6/1465575923433_33812.jpg"
                            },
                            new IM_User
                            {
                                id = "b3cd2e6a-6f76-459c-9c21-7e37aaefa229",
                                username = "测试2",
                                status = "online",
                                sign = "一个开心的开发者",
                                avatar = "http://cdn.firstlinkapp.com/upload/2016_6/1465575923433_33812.jpg"
                            },
                             new IM_User
                            {
                                id = "e714e811-88e4-49ae-a3b1-1613594f1693",
                                username = "测试3",
                                status = "online",
                                sign = "一个开心的开发者",
                                avatar = "http://cdn.firstlinkapp.com/upload/2016_6/1465575923433_33812.jpg"
                            }
                        }
                    }
                },
                group = new List<IM_Group>
                {
                   new IM_Group
                   {
                       groupname = "C#开发群",
                       id="101",
                       avatar ="http://tp2.sinaimg.cn/5488749285/50/5719808192/1"
                   }
                }
            };
            return result;
        }


        public object GetMembers()
        {
            var result = new ApiResult<object>();
            result.Code = 0;
            result.Data = new
            {
                owner = new IM_User
                {
                    id = "565f2b2d-661b-4e8f-8c32-ffc2b32e49fc",
                    username = "闵仁辉",
                    status = "online",
                    sign = "一个开心的开发者",
                    avatar = "http://cdn.firstlinkapp.com/upload/2016_6/1465575923433_33812.jpg"
                },

                list = new List<IM_User>
                {
                    new IM_User
                    {
                        id = "b3cd2e6a-6f76-459c-9c21-7e37aaefa229",
                        username = "测试2",
                        status = "online",
                        sign = "一个开心的开发者",
                        avatar = "http://cdn.firstlinkapp.com/upload/2016_6/1465575923433_33812.jpg"
                    },
                    new IM_User
                    {
                        id = "e714e811-88e4-49ae-a3b1-1613594f1693",
                        username = "测试3",
                        status = "online",
                        sign = "一个开心的开发者",
                        avatar = "http://cdn.firstlinkapp.com/upload/2016_6/1465575923433_33812.jpg"
                    }
                }
            };
            return result;
        }

        public object PreConnect()
        {
            var num = new Random().Next(0, TestUsers.Count());
            return new
            {
                code = 0,
                token = TestUsers[num]
            };
        }

        private string[] TestUsers
        {
            get
            {
                //var arr = new string[] { "565f2b2d-661b-4e8f-8c32-ffc2b32e49fc", "b3cd2e6a-6f76-459c-9c21-7e37aaefa229", "e714e811-88e4-49ae-a3b1-1613594f1693", "b024928e-69c7-47a1-b6ba-cf4e6ac1f141", "c96f2eae-3e7f-4d4c-a6bb-fb1dfa683528", "d3a16412-20be-48ce-b2c1-5ee5a64b35ee", "cf94b015-e5e9-45b8-a25a-60b73fbf4857", "ce1fa5a7-de12-4917-8801-ab689f797280", "3dd88acb-3e5a-4536-a143-813e1c26caa8", "1631bf4c-afb6-44b6-a2c3-c2f841248aa9" };
                var arr = new string[] { "565f2b2d-661b-4e8f-8c32-ffc2b32e49fc", "b3cd2e6a-6f76-459c-9c21-7e37aaefa229", "e714e811-88e4-49ae-a3b1-1613594f1693", "b024928e-69c7-47a1-b6ba-cf4e6ac1f141", "c96f2eae-3e7f-4d4c-a6bb-fb1dfa683528", "d3a16412-20be-48ce-b2c1-5ee5a64b35ee", "cf94b015-e5e9-45b8-a25a-60b73fbf4857", "ce1fa5a7-de12-4917-8801-ab689f797280", "3dd88acb-3e5a-4536-a143-813e1c26caa8", "1631bf4c-afb6-44b6-a2c3-c2f841248aa9" };
                return arr;
            }
        }
    }
}

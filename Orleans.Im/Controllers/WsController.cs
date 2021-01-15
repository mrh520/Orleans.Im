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
        public async Task<string> SendMessage([FromBody] ChatMessage msg)
        {
            var grain = _client.GetGrain<IChatGrain>(msg.SendId);
            var result = await grain.SendMessage(msg);
            return result;
        }

        public object PreConnect()
        {
            return new
            {
                code = 0,
                token = Guid.NewGuid()
            };
        }
    }
}

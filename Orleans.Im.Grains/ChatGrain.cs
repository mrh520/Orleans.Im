using System;
using System.Collections.Generic;
using System.Text;
using Orleans;
using Orleans.Streams.Core;
using System.Threading.Tasks;

using System.Net.WebSockets;
using System.Threading;
using Orleans.Im.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Im.Grains
{
    public class ChatGrain : Grain, IChatGrain
    {
        IClusterClient _client;
        public ChatGrain(IClusterClient client)
        {
            _client = client;
        }
        private bool status = false;
        public async Task<bool> Online()
        {
            status = true;
            return await Task.FromResult(status);
        }

        public Task Offline()
        {
            var ss = new ClientWebSocket();
            status = false;
            return Task.CompletedTask;
        }

        public async Task<(bool, string)> SendMessage(Packet packet)
        {
            var provider = GetStreamProvider(Constant.STREAM_PROVIDER);
            var stream = provider.GetStream<Packet>(Guid.Parse(packet.SendId), Constant.SERVERS_STREAM);

            await stream.OnNextAsync(packet);

            return await Task.FromResult((true, "ok"));
        }

        public override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }
    }

    public interface IChatGrain : IGrainWithStringKey
    {
        Task<bool> Online();

        Task Offline();

        Task<(bool, string)> SendMessage(Packet packet);
    }

}

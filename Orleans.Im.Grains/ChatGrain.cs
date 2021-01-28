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
    public class ChatGrain : Grain, IChatGrain,IIncomingGrainCallFilter
    {
        IClusterClient _client;
        public ChatGrain(IClusterClient client)
        {
            _client = client;
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

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            await context.Invoke();
        }
    }

    public interface IChatGrain : IGrainWithStringKey
    {
        Task<(bool, string)> SendMessage(Packet packet);
    }

}

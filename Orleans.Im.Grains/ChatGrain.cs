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

        public Task<string> SendMessage(ChatMessage msg)
        {
            var provider = GetStreamProvider(Constant.STREAM_PROVIDER);
            var stream = provider.GetStream<ChatMessage>(Guid.Parse(msg.ReceiveId), Constant.SERVERS_STREAM);
            stream.OnNextAsync(msg);
            return Task.FromResult("ok");
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

        Task<string> SendMessage(ChatMessage msg);
    }

    //public class StreamingHistoryGrain : Grain, IStreamingHistoryGrain, IAsyncObserver<ChatMessage>
    //{
    //    private List<int> receivedItems = new List<int>();
    //    private List<StreamSubscriptionHandle<int>> subscriptionHandles = new List<StreamSubscriptionHandle<int>>();

    //    public async Task Subc(Guid streamId, string provider, string filterData = null)
    //    {
    //        var stream = base.GetStreamProvider(provider).GetStream<int>(streamId, "111");
    //        this.subscriptionHandles.Add(await stream.SubscribeAsync((msg, _) => ProcessAllMessage(msg)));
    //        await stream.OnNextAsync(1000);
    //    }

    //    private Task ProcessAllMessage(int message)
    //    {
    //        Console.WriteLine(message);
    //        return Task.CompletedTask;
    //    }
    //    public Task<List<int>> GetReceivedItems() => Task.FromResult(this.receivedItems);

    //    public async Task StopBeingConsumer()
    //    {
    //        foreach (var sub in this.subscriptionHandles)
    //        {
    //            await sub.UnsubscribeAsync();
    //        }
    //    }

    //    public Task OnCompletedAsync() => Task.CompletedTask;

    //    public Task OnErrorAsync(Exception ex) => Task.CompletedTask;

    //    public Task OnNextAsync(ChatMessage msg, StreamSequenceToken token = null)
    //    {

    //        return Task.CompletedTask;
    //    }
    //}

    //public interface IStreamingHistoryGrain : IGrainWithStringKey
    //{
    //    Task Subc(Guid streamId, string provider, string filterData = null);

    //    Task StopBeingConsumer();

    //    Task<List<int>> GetReceivedItems();
    //}
}

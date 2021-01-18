using Microsoft.AspNetCore.Http;
using Orleans.Im.Common;
using Orleans.Im.Grains;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Im
{
    public class ImServer
    {
        private static ConcurrentDictionary<string, WebSocket> _socketClients = new ConcurrentDictionary<string, WebSocket>();
        private static IStreamProvider _streamProvider;
        private static IClusterClient _clusterClient;
        private readonly IServiceProvider _provider;
        public ImServer(IServiceProvider provider)
        {
            _provider = provider;
            _clusterClient = (IClusterClient)_provider.GetService(typeof(IClusterClient));
            _streamProvider = _clusterClient.GetStreamProvider(Constant.STREAM_PROVIDER);
        }

        const int BufferSize = 4096;
        internal async Task Acceptor(HttpContext context, Func<Task> next)
        {
            if (!context.WebSockets.IsWebSocketRequest) return;
            string clientId = context.Request.Query["token"];
            if (string.IsNullOrEmpty(clientId)) return;
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            _socketClients.TryAdd(clientId, socket);

            var buffer = new byte[BufferSize];
            var seg = new ArraySegment<byte>(buffer);
            var grain = _clusterClient.GetGrain<IChatGrain>(clientId);
            await grain.Online();
            await ImHelper.Online();
            var stream = _streamProvider.GetStream<Packet>(Guid.Parse(clientId), Constant.SERVERS_STREAM);

            await stream.SubscribeAsync(async (msg, _) => await ProcessMessage(msg));

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var incoming = await socket.ReceiveAsync(seg, CancellationToken.None);
                    var outgoing = new ArraySegment<byte>(buffer, 0, incoming.Count);
                }
                socket.Abort();
            }
            catch
            {
            }
            await grain.Offline();
            await ImHelper.Offline();
            _socketClients.TryRemove(clientId, out _);

        }

        private async Task ProcessMessage(Packet packet)
        {
            var flag = _socketClients.TryGetValue(packet.ReceiveId, out var socket);
            if (flag)
            {
                var outgoing = new ArraySegment<byte>(Encoding.UTF8.GetBytes(packet.ToJson()));
                await socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
            }           

        }

    }
}

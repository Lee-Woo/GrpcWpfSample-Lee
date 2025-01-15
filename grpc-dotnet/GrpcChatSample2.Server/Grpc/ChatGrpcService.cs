using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcChatSample.Common;
using GrpcChatSample2.Server.Infrastructure;
using GrpcChatSample2.Server.Model;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Reactive.Linq;

namespace GrpcChatSample2.Server.Rpc
{
    [Export]
    public class ChatGrpcService : Chat.ChatBase
    {
        [Import]
        private Logger m_logger = null;

        [Import]
        private ChatService m_chatService = null;
        private readonly Empty m_empty = new Empty();

        // 用于保存用户与其连接的映射  
        private static ConcurrentDictionary<string, IAsyncStreamWriter<ChatLog>> _clientConnections = new ConcurrentDictionary<string, IAsyncStreamWriter<ChatLog>>();
        private static ConcurrentDictionary<string, CancellationToken> _clientCancelTokens = new ConcurrentDictionary<string, CancellationToken>();
        
        public override async Task Subscribe(Empty request, IServerStreamWriter<ChatLog> responseStream, ServerCallContext context)
        {
            var peer = context.Peer; // keep peer information because it is not available after disconnection
            var clientName = context.RequestHeaders.GetValue("client_id");
            m_logger.Info($"clientName {clientName} , {peer} subscribes.");

            _clientConnections[clientName] = responseStream;
            _clientCancelTokens[clientName] = context.CancellationToken;
          
            context.CancellationToken.Register(() => m_logger.Info($"{clientName} cancels subscription."));

            // Completing the method means disconnecting the stream by server side.
            // If subscribing IObservable, you have to block this method after the subscription.
            // I prefer converting IObservable to IAsyncEnumerable to consume the sequense here
            // because gRPC interface is in IAsyncEnumerable world.
            // Note that the chat service model itself is in IObservable world
            // because chat is naturally recognized as an event sequence.

            try
            {
                await m_chatService.GetChatLogsAsObservable()
                    .ToAsyncEnumerable()
                    .ForEachAwaitAsync(async (x) =>
                    {
                        //将当前客户端历史聊天记录返回
                        if (x.FromName.Equals(clientName))
                            await responseStream.WriteAsync(x);
                    }, context.CancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException ex)
            {
                m_logger.Info($"{clientName} unsubscribed.");
            }
        }

        public override async Task<WriteResponse> Write(ChatLog request, ServerCallContext context)
        {
            m_logger.Info($"{context.Peer} {request}");

            if (_clientConnections.TryGetValue(request.ToName, out var targetClient))
            {
                m_chatService.Add(request);

                // 发送消息到指定的客户端  
                await targetClient.WriteAsync(request, _clientCancelTokens[request.ToName]);

                return await Task.FromResult(new WriteResponse { Success = true });
            }
            else
            {
                m_logger.Info($"FromName: {request.FromName} , ToName: {request.ToName} , Target user [{request.ToName}] is not connected.");

                // 目标客户端未连接  
                return await Task.FromResult(new WriteResponse { Success = false, ErrorMessage = $"Target user [{request.ToName}] is not connected." });
            }
        }
    }
}

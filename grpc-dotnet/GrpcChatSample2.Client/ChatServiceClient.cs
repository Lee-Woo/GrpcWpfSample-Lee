using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using GrpcChatSample.Common;
using System.Threading.Channels;

namespace GrpcChatSample2.Client
{
    public class ChatServiceClient : IDisposable
    {
        private readonly Chat.ChatClient m_client;
        private readonly GrpcChannel m_channel; // If you create multiple client instances, the GrpcChannel should be shared.
        private bool disposedValue;

        private string m_ClientId = "wangli";

        public ChatServiceClient()
        {

        }

        public ChatServiceClient(string clientId) : this()
        {
            m_ClientId = clientId;

            // See https://docs.microsoft.com/en-us/aspnet/core/grpc/client

            // To enable https, the server must be configured to use https.
            var https = false;

            if (https)
            {
                // See https://docs.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz#client-certificate-authentication
                // for client certificate authentication

                var httpHandler = new HttpClientHandler();

                // Here you can disable validation for server certificate for your easy local test
                // See https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot#call-a-grpc-service-with-an-untrustedinvalid-certificate
                //httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                m_channel = GrpcChannel.ForAddress("https://localhost:50052", new GrpcChannelOptions { HttpHandler = httpHandler });
                m_client = new Chat.ChatClient(
                    m_channel
                    .Intercept(new ClientIdInjector(m_ClientId)) // 2nd
                    .Intercept(new HeadersInjector())); // 1st
            }
            else
            {
                // create insecure channel
                m_channel = GrpcChannel.ForAddress("http://localhost:50052");
                m_client = new Chat.ChatClient(
                    m_channel
                    .Intercept(new ClientIdInjector(m_ClientId)) // 2nd
                    .Intercept(new HeadersInjector())); // 1st
            }
        }

        public async Task<WriteResponse?> Write(ChatLog chatLog)
        {
            return await m_client.WriteAsync(chatLog);
        }

        public IAsyncEnumerable<ChatLog> ChatLogs()
        {
            var call = m_client.Subscribe(new Empty());

            // I do not want to expose gRPC such as IAsyncStreamReader or AsyncServerStreamingCall.
            // I also do not want to bother user of this class with asking to dispose the call object.

            return call.ResponseStream
                .ToAsyncEnumerable()
                .Catch<ChatLog, RpcException>((ex) =>
                {
                    // 注意：根据需要，您可以返回一个特定的结果，例如：  
                    // return AsyncEnumerable.Empty<ChatLog>();  

                    // 当发生 RPC 异常（例如未连接时）  
                    return GetErrorResponse();
                })
                .Finally(() => call.Dispose());

        }
        // 处理未连接时的返回（可选）  
        private async IAsyncEnumerable<ChatLog> GetErrorResponse()
        {
            yield return new ChatLog
            {
                FromName = "System",
                ToName = "User",
                Content = "Unable to connect to the chat server. Please try again later.",
                At = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
            };

            // 如果需要，可以引发更多的错误处理逻辑  
            await Task.CompletedTask; // 为了兼容异步  
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_channel.Dispose(); // disposes all of active calls.
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

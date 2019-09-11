namespace OrbitalForge.ServiceMesh.HttpListener
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;
    using OrbitalForge.ServiceMesh.Core.Rpc;
    using System.Net.Http;
    using System.Reflection;
    using System.Collections.Concurrent;

    internal class HttpListenerClient
    {
        private volatile object syncObj = new object();

        const int MaxWait = 5;

        private readonly Core.Rpc.ServiceMesh.ServiceMeshClient serviceMesh;

        public HttpListenerClient(string ipAddress, int port) 
        {
            Channel channel = new Channel($"{ipAddress}:{port}", ChannelCredentials.Insecure);
            serviceMesh = new Core.Rpc.ServiceMesh.ServiceMeshClient(channel);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var sendHeartBeat = Task.Factory.StartNew(async () => {
                
                    while(true) 
                    {
                        using(var eventStream = serviceMesh.EventStream()) 
                        {
                            await InitAsync(eventStream, false);

                            Console.WriteLine("Sending Heartbeat ...");

                            await eventStream.RequestStream.WriteAsync(new StreamingMessage() {
                                WorkerHeartbeat = new WorkerHeartbeat(),
                                RequestId = Guid.NewGuid().ToString()
                            });
                        }
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
            }, cancellationToken);
                
            while(!cancellationToken.IsCancellationRequested) 
            {
                using(var eventStream = serviceMesh.EventStream()) 
                {
                    await InitAsync(eventStream, true);

                    try {
                        // Wait For Event
                        await eventStream.ResponseStream.MoveNext(cancellationToken);

                        // Process Message

                        // Send Response
                        await eventStream.RequestStream.WriteAsync(new StreamingMessage());
                    } catch(Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        private static async Task InitAsync(AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> eventStream, bool isListener) 
        {
            var setupMessage = new StreamingMessage() {
                WorkerInitRequest = new WorkerInitRequest()
            };

            if(isListener) {
                setupMessage.WorkerInitRequest.Capabilities.Add("Listener", "true");
            }

            setupMessage.WorkerInitRequest.HostVersion = Assembly.GetExecutingAssembly().FullName;

            await eventStream.RequestStream.WriteAsync(setupMessage);

            // Since we're not queueing the data corectly ... we actually run into an issue where we've queued the client before it's fully ready.
            // TODO: Fixme
            await eventStream.ResponseStream.MoveNext(CancellationToken.None);
            
            if(eventStream.ResponseStream.Current.ContentCase != StreamingMessage.ContentOneofCase.WorkerInitResponse)
            {
                throw new Exception("Was expecing an init response.");
            }

            if (eventStream.ResponseStream.Current.WorkerInitResponse.Result.Status != Core.Rpc.StatusResult.Types.Status.Success) 
            {
                throw new Exception(eventStream.ResponseStream.Current.WorkerInitResponse.Result.Exception.Message);
            }
        }

        private static async Task RunEventClientOnSameStream(int id, Core.Rpc.ServiceMesh.ServiceMeshClient client) 
        {
            using(var eventStream = client.EventStream()) 
            {
                

                for(int count = 0; count < 10000; count++) {
                    var message = new StreamingMessage();

                    message.RequestId = $"{id}:{count}";
                    await eventStream.RequestStream.WriteAsync(message);

                    var source = new CancellationTokenSource();
                    var task = eventStream.ResponseStream.MoveNext(source.Token);
                    source.CancelAfter(TimeSpan.FromMilliseconds(50));

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://www.google.com"));

                    if(await task) 
                    {
                        Console.WriteLine($"Response: {eventStream.ResponseStream.Current.RequestId}");
                    } else {
                        Console.WriteLine("Response Not Recieved In A Timely Manner!");
                        throw new TimeoutException();
                    }
                }

                await eventStream.RequestStream.CompleteAsync();
            }
        }

        private static async Task SendMessage(Core.Rpc.ServiceMesh.ServiceMeshClient client, int count) 
        {
            using(var eventStream = client.EventStream()) 
            {
                var message = new StreamingMessage();
                message.RequestId = count.ToString();
                await eventStream.RequestStream.WriteAsync(message);
                var responseRecieved = await eventStream.ResponseStream.MoveNext();

                if(responseRecieved) 
                {
                    Console.WriteLine($"Response: {eventStream.ResponseStream.Current.RequestId}");
                }

                await eventStream.RequestStream.CompleteAsync();
            }
        }
    }
}
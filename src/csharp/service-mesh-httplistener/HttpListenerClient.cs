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

        private int messagesProcessed = 0;

        private readonly Core.Rpc.ServiceMesh.ServiceMeshClient serviceMesh;

        public HttpListenerClient(string ipAddress, int port) 
        {
            Channel channel = new Channel($"{ipAddress}:{port}", ChannelCredentials.Insecure);
            serviceMesh = new Core.Rpc.ServiceMesh.ServiceMeshClient(channel);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using(var eventStream = serviceMesh.EventStream()) 
            {
                await InitAsync(eventStream);

                var sendHeartBeat = Task.Factory.StartNew(async () => {
                        while(true) 
                        {
                            Console.WriteLine("Sending Heartbeat ...");

                            await eventStream.RequestStream.WriteAsync(new StreamingMessage() {
                                WorkerHeartbeat = new WorkerHeartbeat(),
                                RequestId = Guid.NewGuid().ToString()
                            });

                            Console.WriteLine($"Messages Processed: {messagesProcessed}");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                }, cancellationToken);
                
                while(!cancellationToken.IsCancellationRequested) 
                {
                    try {
                        // Wait For Event
                        await eventStream.ResponseStream.MoveNext(cancellationToken);

                        // Process Message
                        if(eventStream.ResponseStream.Current.ContentCase != StreamingMessage.ContentOneofCase.InvocationRequest)
                        {
                            throw new Exception("Was expecing an invocation.");
                        }

                        // Send Response
                        await eventStream.RequestStream.WriteAsync(new StreamingMessage(){
                            RequestId = eventStream.ResponseStream.Current.RequestId,
                            InvocationResponse = new InvocationResponse() {
                                InvocationId = eventStream.ResponseStream.Current.InvocationRequest.InvocationId
                            }
                        });

                        messagesProcessed++;
                    } catch(Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        private static async Task InitAsync(AsyncDuplexStreamingCall<StreamingMessage, StreamingMessage> eventStream) 
        {
            await eventStream.RequestStream.WriteAsync(new StreamingMessage() {
                StartStream = new StartStream() {
                    WorkerId = Guid.NewGuid().ToString()
                }
            });

            await eventStream.ResponseStream.MoveNext(CancellationToken.None);

            if(eventStream.ResponseStream.Current.ContentCase != StreamingMessage.ContentOneofCase.WorkerInitRequest)
            {
                throw new Exception("Was expecing an init request.");
            }

            Console.WriteLine($"Server Version: {eventStream.ResponseStream.Current.WorkerInitRequest.HostVersion}");
            
            // Complete initialization
            await eventStream.RequestStream.WriteAsync(new StreamingMessage() {
                WorkerInitResponse = new WorkerInitResponse() {
                    WorkerVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
                }
            });
        }
    }
}
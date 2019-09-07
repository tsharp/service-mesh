using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using OrbitalForge.ServiceMesh.Core.Rpc;
using System.Net;
using System.Net.Http;

namespace OrbitalForge.ServiceMesh.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(typeof(OrbitalForge.ServiceMesh.Core.Rpc.ServiceMesh.ServiceMeshClient));
            Channel channel = new Channel("172.17.0.2:80", ChannelCredentials.Insecure);
            var client = new Core.Rpc.ServiceMesh.ServiceMeshClient(channel);

            Task.WaitAll(
                RunEventClientOnSameStream(0, client), 
                RunEventClientOnSameStream(1, client), 
                RunEventClientOnSameStream(2, client),
                RunEventClientOnSameStream(3, client),
                RunEventClientOnSameStream(4, client));
        }

        private static async Task RunEventClient(Core.Rpc.ServiceMesh.ServiceMeshClient client) 
        {
            for(int i = 0; i < 10000; i++)
            {
                await SendMessage(client, i);
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
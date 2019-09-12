namespace OrbitalForge.ServiceMesh.Server.HttpTunnel
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Grpc.Core;
    using OrbitalForge.ServiceMesh.Core;

    class Program
    {
        static void Main(string[] args)
        {
            Run(80);
        }

        private static void Run(int port)
        {
            var meshServer = new ServiceMeshServer();

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { Core.Rpc.ServiceMesh.BindService(meshServer) },
                Ports = { new ServerPort(GetLocalIPAddress(), port, ServerCredentials.Insecure) }
            };

            server.Start();

            Console.WriteLine("HTTP Relay Server listening on port " + port);
            Console.WriteLine(GetLocalIPAddress());
            Console.WriteLine("Press Ctrl-C to stop the server...");
            
            System.AppDomain.CurrentDomain.ProcessExit += (s, e) => 
            {
                Console.WriteLine("Server Shutting Down ...");
                server.ShutdownAsync().Wait();
                Console.WriteLine("Server Stopped.");
            };

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            var tasks = new [] {
                SendMessages(meshServer),
                SendMessages(meshServer),
                SendMessages(meshServer),
                SendMessages(meshServer)
            };
           
            while(true) 
            {
                var msgPerSecond = (double)meshServer.ProcessedMessages / (double)timer.ElapsedMilliseconds * 1000.0;
                Console.WriteLine($"Messages Processed: {msgPerSecond}/s - {meshServer.ProcessedMessages}");
                System.Threading.Thread.Sleep(250);
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static async Task SendMessages(ServiceMeshServer server) 
        {
            while(true) 
            {
                var result = await server.SendRequestAsync(new Core.Rpc.StreamingMessage() {
                    RequestId = Guid.NewGuid().ToString(),
                    InvocationRequest = new Core.Rpc.InvocationRequest() {
                        InvocationId = Guid.NewGuid().ToString()
                    }
                });

                // Console.WriteLine($"result: {result.RequestId}");
            }
        }
    }
}

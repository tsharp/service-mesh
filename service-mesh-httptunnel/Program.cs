namespace OrbitalForge.ServiceMesh.Server.HttpTunnel
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using Grpc.Core;

    class Program
    {
        static void Main(string[] args)
        {
            Run(80);
        }

        private static void Run(int port)
        {
            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { Core.Rpc.ServiceMesh.BindService(new Core.ServiceMeshServer()) },
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

           
            while(true) 
            {
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
    }
}

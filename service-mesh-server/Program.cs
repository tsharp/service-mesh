using System;
using System.Net;
using System.Net.Sockets;
using Grpc.Core;

namespace OrbitalForge.ServiceMesh.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Run();
        }

        private static void Run()
        {
            const int Port = 50051;

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { Core.Rpc.ServiceMesh.BindService(new Core.ServiceMeshServer()) },
                Ports = { new ServerPort(GetLocalIPAddress(), Port, ServerCredentials.Insecure) }
            };

            server.Start();

            Console.WriteLine("RouteGuide server listening on port " + Port);
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

            /*
            if (System.Environment.UserInteractive)
            {
                Console.ReadKey();
            }

            server.ShutdownAsync().Wait();
            */
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

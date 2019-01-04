using System;
using Grpc.Core;

namespace OrbitalForge.ServiceMesh.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            const int Port = 50051;

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { Core.Rpc.ServiceMesh.BindService(new Core.ServiceMeshServer()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("RouteGuide server listening on port " + Port);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}

namespace OrbitalForge.ServiceMesh.Server.HttpTunnel
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;

    internal class HttpTunnelServer : Core.ServiceMeshServer 
    {
        protected override Task<Core.Rpc.StreamingMessage> OnMessage(Core.Rpc.StreamingMessage message) 
        {
            Console.WriteLine(message.RequestId);
            return Task.FromResult(new Core.Rpc.StreamingMessage(){
                RequestId = $"http:{Guid.NewGuid().ToString()}"
            });
        }
    }

}
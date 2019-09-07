namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class ServiceMeshServer : Rpc.ServiceMesh.ServiceMeshBase
    {
        public int ConnectedWorkers { get => workers.Count; }

        private ConcurrentQueue<ServiceMeshWorker> workers = new ConcurrentQueue<ServiceMeshWorker>();

        public override async Task EventStream(IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream, IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream, ServerCallContext context) 
        {
            var worker = new ServiceMeshWorker(requestStream, responseStream);

            try 
            {
                await worker.InitAsync();

                if(worker.Capabilities.ContainsKey("Listener")) 
                {
                    workers.Enqueue(worker);
                }

                await worker.RunAsync(OnMessage);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        // TODO: Implement Keep-Alive Ping

        // TODO: Implement Is Healthy Check

        protected virtual Task<Core.Rpc.StreamingMessage> OnMessage(Core.Rpc.StreamingMessage message) 
        {
            Console.WriteLine(message.RequestId);
            return Task.FromResult(new Core.Rpc.StreamingMessage(){
                RequestId = $"base:{Guid.NewGuid().ToString()}"
            });
        }
    }
}
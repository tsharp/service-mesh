namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class ServiceMeshServer : Rpc.ServiceMesh.ServiceMeshBase
    {
        private int connectedWorkers = 0;

        public virtual int ConnectedWorkers { get => connectedWorkers; }

        private ConcurrentQueue<ServiceMeshWorker> workers = new ConcurrentQueue<ServiceMeshWorker>();

        public override async Task EventStream(IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream, IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream, ServerCallContext context) 
        {
            var worker = new ServiceMeshWorker(requestStream, responseStream);

            Interlocked.Increment(ref connectedWorkers);

            try 
            {
                await worker.InitAsync();

                await RegisterWorkerAsync(worker);

                await worker.RunAsync(OnMessage);
            }
            finally
            {
                Interlocked.Decrement(ref connectedWorkers);

                await UnRegisterWorkerAsync(worker);
            }
        }

        // TODO: Implement Keep-Alive Ping

        // TODO: Implement Is Healthy Check

        protected virtual Task RegisterWorkerAsync(ServiceMeshWorker worker) 
        {
            return Task.CompletedTask;
        }

        protected virtual Task UnRegisterWorkerAsync(ServiceMeshWorker worker) 
        {
            return Task.CompletedTask;
        }

        protected virtual Task<Core.Rpc.StreamingMessage> OnMessage(Core.Rpc.StreamingMessage message) 
        {
            Console.WriteLine(message.RequestId);
            return Task.FromResult(new Core.Rpc.StreamingMessage(){
                RequestId = $"base:{Guid.NewGuid().ToString()}"
            });
        }
    }
}
namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;
    using OrbitalForge.ServiceMesh.Core.Rpc;

    public class ServiceMeshServer : Rpc.ServiceMesh.ServiceMeshBase, IServiceMeshWorker
    {
        private int connectedWorkers = 0;

        public int ProcessedMessages { get; private set; } = 0;

        public virtual int ConnectedWorkers { get => connectedWorkers; }

        private ConcurrentDictionary<string, Core.ServiceMeshWorker> registeredWorkers = new ConcurrentDictionary<string, Core.ServiceMeshWorker>();

        private ConcurrentQueue<Core.ServiceMeshWorker> workers = new ConcurrentQueue<Core.ServiceMeshWorker>();

        public override async Task EventStream(IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream, IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream, ServerCallContext context) 
        {
            var worker = new ServiceMeshWorker(requestStream, responseStream);

            Interlocked.Increment(ref connectedWorkers);

            try 
            {
                await worker.InitAsync();

                RegisterWorker(worker);

                await worker.RunAsync();
            }
            finally
            {
                Interlocked.Decrement(ref connectedWorkers);

                UnRegisterWorker(worker);
            }
        }

        private void RegisterWorker(Core.ServiceMeshWorker worker)
        {
            registeredWorkers.TryAdd(worker.WorkerId, worker);
            workers.Enqueue(worker);
            Console.WriteLine($"+ Worker Added {""}:{registeredWorkers.Count}");
        }

        private void UnRegisterWorker(Core.ServiceMeshWorker worker)
        {
            registeredWorkers.TryRemove(worker.WorkerId, out Core.ServiceMeshWorker unregistered);
            Console.WriteLine($"- Worker Removed {""}:{registeredWorkers.Count}");
        }

        private async Task<Core.ServiceMeshWorker> AcquireWorkerAsync()
        {
            Core.ServiceMeshWorker worker;

            while(!workers.TryDequeue(out worker)) {
                await Task.Delay(5);
            }

            return worker;
        }

        private void ReleaseWorker(Core.ServiceMeshWorker worker)
        {
            workers.Enqueue(worker);
        }

        public async Task<StreamingMessage> SendRequestAsync(StreamingMessage request)
        {
            var worker = await AcquireWorkerAsync();

            try 
            {
                var response = await worker.SendRequestAsync(request);
                ProcessedMessages++;
                return response;
            } 
            finally
            {
                ReleaseWorker(worker);
            }
        }
    }
}
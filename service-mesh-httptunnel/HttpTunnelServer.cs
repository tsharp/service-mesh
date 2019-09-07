namespace OrbitalForge.ServiceMesh.Server.HttpTunnel
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    internal class HttpTunnelServer : Core.ServiceMeshServer 
    {
        private Random random = new Random();

        private ConcurrentDictionary<int, Core.ServiceMeshWorker> workers = new ConcurrentDictionary<int, Core.ServiceMeshWorker>();

        protected override Task RegisterWorkerAsync(Core.ServiceMeshWorker worker)
        {
            if(worker.IsListener) 
            {
                workers.TryAdd(worker.GetHashCode(), worker);
                Console.WriteLine($"++ Worker Added {""}:{workers.Count}");
            }

            return Task.CompletedTask;
        }

        protected override Task UnRegisterWorkerAsync(Core.ServiceMeshWorker worker)
        {
            if( worker.IsListener &&
                workers.TryRemove(worker.GetHashCode(), out Core.ServiceMeshWorker removedWorker))
            {
                Console.WriteLine($"-- Worker Removed {""}:{workers.Count}");
            }

            return Task.CompletedTask;
        }

        protected override async Task<Core.Rpc.StreamingMessage> OnMessage(Core.Rpc.StreamingMessage message) 
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            while(workers.IsEmpty) 
            {
                if(timer.Elapsed.TotalSeconds >= 30)
                {
                    throw new TimeoutException("Unable to connect to listener.");
                }
            }

            var workerIndex = random.Next(workers.Count - 1);
            
            try {
                Console.WriteLine($"Selected Worker Index: {workerIndex}");

                await workers.ToArray()[workerIndex].Value.SendRequestAsync(new Core.Rpc.StreamingMessage(){
                    RequestId = $"http:{Guid.NewGuid().ToString()}"
                });
            } catch(Exception ex) {
                Console.WriteLine(ex);
            }

            return new Core.Rpc.StreamingMessage(){
                RequestId = $"http:{Guid.NewGuid().ToString()}"
            };

            
        }
    }

}
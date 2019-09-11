namespace OrbitalForge.ServiceMesh.Server.HttpTunnel
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    internal class HttpTunnelServer : Core.ServiceMeshServer 
    {
        private Random random = new Random();

        private ConcurrentDictionary<int, Core.ServiceMeshWorker> registeredWorkers = new ConcurrentDictionary<int, Core.ServiceMeshWorker>();

        private ConcurrentStack<Core.ServiceMeshWorker> freeWorkers = new ConcurrentStack<Core.ServiceMeshWorker>();

        protected override Task RegisterWorkerAsync(Core.ServiceMeshWorker worker)
        {
            if(worker.IsListener) 
            {
                registeredWorkers.TryAdd(worker.GetHashCode(), worker);
                freeWorkers.Push(worker);
                Console.WriteLine($"+ Worker Added {""}:{registeredWorkers.Count}");
            }

            return Task.CompletedTask;
        }

        protected override Task UnRegisterWorkerAsync(Core.ServiceMeshWorker worker)
        {
            if(worker.IsListener)
            {
                registeredWorkers.TryRemove(worker.GetHashCode(), out Core.ServiceMeshWorker unregistered);
                Console.WriteLine($"- Worker Removed {""}:{registeredWorkers.Count}");
            }

            return Task.CompletedTask;
        }

        protected override async Task<Core.Rpc.StreamingMessage> OnMessage(Core.Rpc.StreamingMessage message) 
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            Core.ServiceMeshWorker worker = null;

            while(!freeWorkers.TryPop(out worker))
            {
                // Wait 10ms before trying to find a worker
                await Task.Delay(10);

                if(timer.Elapsed.TotalSeconds >= 30)
                {
                    throw new TimeoutException("Unable to connect to listener.");
                }
            }

            if(worker == null) {
                throw new Exception("PANIC: Worker not found!");
            }

            try 
            {
                // Since these may be out of order - this needs to be synchronized.
                return await worker.SendRequestAsync(message.Clone());
            } 
            finally 
            {
                freeWorkers.Push(worker);
            }        
        }
    }

}
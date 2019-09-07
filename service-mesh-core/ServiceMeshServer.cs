namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class ServiceMeshServer : Rpc.ServiceMesh.ServiceMeshBase
    {
        const int MaxWait = 1000;

        private volatile int connectedWorkers = 0;

        public int ConnectedWorkers { get => connectedWorkers; }

        public override async Task EventStream(IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream, IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream, ServerCallContext context) 
        {
            Interlocked.Increment(ref connectedWorkers);

            try 
            {
                var source = new CancellationTokenSource();
                var task = requestStream.MoveNext(source.Token);
                source.CancelAfter(TimeSpan.FromMilliseconds(MaxWait));

                while(await task) 
                {
                    await responseStream.WriteAsync(await OnMessage(requestStream.Current));
                    task = requestStream.MoveNext(source.Token);
                    source.CancelAfter(TimeSpan.FromMilliseconds(MaxWait));
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            Interlocked.Decrement(ref connectedWorkers);
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
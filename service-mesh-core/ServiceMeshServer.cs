namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;

    public class ServiceMeshServer : Rpc.ServiceMesh.ServiceMeshBase
    {
        const int MaxWait = 50;

        private volatile int connectedWorkers = 0;

        public override async Task EventStream(IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream, IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream, ServerCallContext context) 
        {
            Interlocked.Increment(ref connectedWorkers);

            var source = new CancellationTokenSource();
            var task = requestStream.MoveNext(source.Token);
            source.CancelAfter(TimeSpan.FromMilliseconds(MaxWait));

            while(await task) 
            {
                Console.WriteLine(requestStream.Current.RequestId);
                await responseStream.WriteAsync(new Core.Rpc.StreamingMessage() { });
                task = requestStream.MoveNext(source.Token);
                source.CancelAfter(TimeSpan.FromMilliseconds(MaxWait));
            }

            Interlocked.Decrement(ref connectedWorkers);
        }
    }
}
namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;
    using static OrbitalForge.ServiceMesh.Core.Rpc.StreamingMessage;

    internal class ServiceMeshWorker
    {
        const int MaxWait = 1000;

        private bool isInitialized = false;

        private readonly IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream;
        private readonly IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream;

        public IReadOnlyDictionary<string, string> Capabilities { get; private set; }

        public string HostVersion { get; private set; }

        public ServiceMeshWorker(IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream, IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream)
        {
            this.requestStream = requestStream;

            this.responseStream = responseStream;
        }

        public async Task InitAsync()
        {
            await requestStream.MoveNext(CancellationToken.None);

            if(requestStream.Current.ContentCase != ContentOneofCase.WorkerInitRequest) 
            {
                throw new Exception("Worker is not in the correct state to initialize.");
            }

            Capabilities = requestStream.Current.WorkerInitRequest
                .Capabilities.ToDictionary(k => k.Key, v => v.Value);

            HostVersion = requestStream.Current.WorkerInitRequest.HostVersion;
            
            var repsonse = new Rpc.WorkerInitResponse();
            repsonse.WorkerVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            repsonse.Result = new Rpc.StatusResult();
            repsonse.Result.Status = Rpc.StatusResult.Types.Status.Success;

            await responseStream.WriteAsync(new Core.Rpc.StreamingMessage() {
                WorkerInitResponse = repsonse
            });    

            isInitialized = true;      
        }

        public async Task RunAsync(Func<Core.Rpc.StreamingMessage, Task<Core.Rpc.StreamingMessage>> messageCallback)
        {
            if(!isInitialized) 
            {
                throw new InvalidProgramException("You must initialize the worker before calling RunAsync.");
            }

            var source = new CancellationTokenSource();
            var task = requestStream.MoveNext(source.Token);
            source.CancelAfter(TimeSpan.FromMilliseconds(MaxWait));

            while(await task) 
            {
                await responseStream.WriteAsync(await messageCallback(requestStream.Current));
                task = requestStream.MoveNext(source.Token);
                source.CancelAfter(TimeSpan.FromMilliseconds(MaxWait));
            }
        }
    }
}
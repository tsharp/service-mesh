namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Grpc.Core;
    using static OrbitalForge.ServiceMesh.Core.Rpc.StreamingMessage;

    public class ServiceMeshWorker
    {
        const int MaxWait = 1000;

        private bool isInitialized = false;

        private readonly IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream;
        private readonly IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream;

        private ConcurrentStack<Core.Rpc.StreamingMessage> responses = new ConcurrentStack<Core.Rpc.StreamingMessage>();

        public IReadOnlyDictionary<string, string> Capabilities { get; private set; }

        public string HostVersion { get; private set; }

        public bool IsListener 
        {
            get 
            {
                return Capabilities.ContainsKey("Listener");
            }
        }

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

            Console.WriteLine("Worker Initialized.");    
        }

        public async Task<Core.Rpc.StreamingMessage> SendRequestAsync(Core.Rpc.StreamingMessage request) 
        {
            // Since these may be out of order - this needs to be synchronized.
            await responseStream.WriteAsync(request);

            Core.Rpc.StreamingMessage response = null;

            Console.WriteLine("Lets wait for a message ...");

            while(!responses.TryPop(out response)) 
            {
                Console.WriteLine("zz Awaiting Response ...");
                await Task.Delay(10);
            }


            Console.WriteLine("zz Response Returned...");

            return response;
        }

        public async Task RunAsync(Func<Core.Rpc.StreamingMessage, Task<Core.Rpc.StreamingMessage>> messageCallback)
        {
            if(!isInitialized) 
            {
                throw new InvalidProgramException("You must initialize the worker before calling RunAsync.");
            }

            while(await requestStream.MoveNext(CancellationToken.None)) 
            {
                if(requestStream.Current.ContentCase == ContentOneofCase.WorkerHeartbeat) 
                {
                    Console.WriteLine("Observed Heartbeat");
                    continue;
                }

                if(!IsListener) 
                {
                    var result = await messageCallback.Invoke(requestStream.Current);
                    await responseStream.WriteAsync(result);
                    continue;
                }
            
                Console.WriteLine("aa Recieved Response ...");

                responses.Push(requestStream.Current.Clone());

                Console.WriteLine("aa Response Queued ...");
            }

            Console.WriteLine("aa Exiting Node.");

            // var sender = Task.Factory.StartNew(() => {
                
            // });

            // await Task.WhenAll(listener, sender);
        }
    }
}
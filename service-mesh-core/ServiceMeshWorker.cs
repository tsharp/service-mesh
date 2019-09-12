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
        private delegate void MessageRecievedEventHandler(ServiceMeshWorker sender, Core.Rpc.StreamingMessage message);

        const int MaxWait = 1000;

        private bool isInitialized = false;

        private readonly IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream;
        private readonly IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream;

        private event MessageRecievedEventHandler MessageRecieved;

        public IReadOnlyDictionary<string, string> Capabilities { get; private set; }

        public string WorkerId { get; private set;}

        public string WorkerVersion { get; private set; }

        public ServiceMeshWorker(IAsyncStreamReader<Core.Rpc.StreamingMessage> requestStream, IServerStreamWriter<Core.Rpc.StreamingMessage> responseStream)
        {
            this.requestStream = requestStream;

            this.responseStream = responseStream;
        }

        public async Task InitAsync()
        {
            // Ack
            await requestStream.MoveNext(CancellationToken.None);

            if(requestStream.Current.ContentCase != ContentOneofCase.StartStream) 
            {
                throw new Exception("The worker did not send StartStreamContent");
            }

            // Set The Worker Id
            WorkerId = requestStream.Current.StartStream.WorkerId;

            // Syn - Init Request
            await responseStream.WriteAsync(new Core.Rpc.StreamingMessage() {
                WorkerInitRequest = new Rpc.WorkerInitRequest() {
                    HostVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()
                }
            });

            // Ack
            await requestStream.MoveNext(CancellationToken.None);

            if(requestStream.Current.ContentCase != ContentOneofCase.WorkerInitResponse) 
            {
                throw new Exception("The worker did not send WorkerInitResponse");
            }

            Capabilities = requestStream.Current.WorkerInitResponse
                .Capabilities.ToDictionary(k => k.Key, v => v.Value);

            WorkerVersion = requestStream.Current.WorkerInitResponse.WorkerVersion;
            
            isInitialized = true;  

            Console.WriteLine("Worker Initialized.");    
        }

        public async Task<Core.Rpc.StreamingMessage> SendRequestAsync(Core.Rpc.StreamingMessage request)
        {
            Core.Rpc.StreamingMessage response = null;

            MessageRecievedEventHandler handler = (sender, message) => {
                response = message;
            };

            try 
            {
                MessageRecieved += handler;

                // Since these may be out of order - this needs to be synchronized.
                await responseStream.WriteAsync(request);

                while(response == null)
                {
                    await Task.Delay(10);
                }

                return response;
            }
            finally
            {
                MessageRecieved -= handler;
            }
        }

        private void OnMessage(Rpc.StreamingMessage message) 
        {
            MessageRecieved?.Invoke(this, message);
        }

        public async Task RunAsync()
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

                OnMessage(requestStream.Current.Clone());
            }
        }
    }
}
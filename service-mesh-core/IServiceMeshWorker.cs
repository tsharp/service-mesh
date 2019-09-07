namespace OrbitalForge.ServiceMesh.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    
    internal interface IServiceMeshWorker 
    {
        Task InitiateWorkerAsync();
    }
}
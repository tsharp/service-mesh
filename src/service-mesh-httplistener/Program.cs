namespace OrbitalForge.ServiceMesh.HttpListener
{
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpListenerClient("52.137.95.212", 80);
            await client.RunAsync(CancellationToken.None);
        }
    }
}
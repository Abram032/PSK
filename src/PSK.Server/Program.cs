using PSK.Protocols.Tcp;
using System;
using System.Threading.Tasks;

namespace PSK.Server
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var rec = new TcpReceiver();
            rec.Start();

            await Task.Delay(-1);
        }
    }
}

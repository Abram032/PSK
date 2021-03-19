using PSK.Core.Models;
using PSK.Protocols.Tcp;
using PSK.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PSK.Server
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var rec = new TcpReceiver();
            rec.OnReceived += OnReceived;
            rec.Start();

            await Task.Delay(-1);
        }

        private static void OnReceived(object sender, OnReceivedEventArgs e)
        {
            var command = e.Data.Split(' ').FirstOrDefault();
            var pingService = new Ping();
            pingService.HandleRequest(e);
            //Commands queue? Add command to execute and create a worker service that executes and responds to clients.
        }
    }
}

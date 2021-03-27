using Newtonsoft.Json;
using PSK.Core.Models.Services;
using PSK.Core.Options;
using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Client
{
    class Program
    {
        public static Stopwatch stopwatch = new Stopwatch();
        public static async Task Main(string[] args)
        {
            string server = "localhost";
            var client = new TcpClient(server, 21021);

            var transceiver = new TcpTransceiver();
            transceiver.Start(client);

            //await transceiver.Transmit(GetConfigMessage());
            await transceiver.Transmit(GetPingMessage());

            await Task.Delay(-1);
        }

        public static string GetPingMessage()
        {
            return "ping 4194304 TESTTESTTESTTEST\n";
        }

        public static string GetConfigMessage()
        {
            var options = new PingServiceOptions
            {
                IsActive = true
            };

            var request = new ConfigureRequest
            {
                Command = ConfigureCommand.GetConfig,
                Type = options.GetType(),
                Options = JsonConvert.SerializeObject(options)
            };

            return $"configure {Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)))}\n";
        }
    }
}
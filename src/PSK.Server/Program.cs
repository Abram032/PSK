using Microsoft.Extensions.Configuration;
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
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .Build();

            var server = new Server(configuration);
            server.Start();
            await Task.Delay(-1);
        }
    }
}

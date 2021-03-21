using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PSK.Protocols.Tcp;
using PSK.Services;
using System.Threading.Tasks;

namespace PSK.Server
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            //Configuration builder
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            //Dependency injection pipeline
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(configure =>
                {
                    configure.ClearProviders();
                    configure.AddConfiguration(configuration.GetSection("Logging"));
                    configure.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    });
                })
                //Server
                .AddSingleton<IServer, Server>()
                //Protocols
                .AddSingleton<ITcpReceiver, TcpReceiver>()
                //.AddSingleton<ITcpTransmitter, TcpTransmitter>()
                //Services
                .AddTransient<IPingService, PingService>()
                .BuildServiceProvider();

            var server = serviceProvider.GetRequiredService<IServer>();

            server.Start();

            await Task.Delay(-1);
        }
    }
}

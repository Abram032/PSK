using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PSK.Core;
using PSK.Protocols.Tcp;
using PSK.Services;
using System.Threading;
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
                //Options configuration
                .Configure<TcpReceiverOptions>(configuration.GetSection($"Protocols:TCP:{nameof(TcpReceiverOptions)}"))
                .Configure<RequestChannelOptions>(configuration.GetSection(nameof(RequestChannelOptions)))
                .Configure<ServerOptions>(configuration.GetSection(nameof(ServerOptions)))
                //Channel
                .AddSingleton<IRequestChannel, RequestChannel>()
                //Receivers
                .AddSingleton<ITcpReceiver, TcpReceiver>()
                //Transmitters
                .AddTransient<ITcpTransmitter, TcpTransmitter>()
                //Services
                .AddSingleton<IPingService, PingService>()
                //Server
                .AddSingleton<IServer, Server>()
                .BuildServiceProvider();

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var server = serviceProvider.GetRequiredService<IServer>();

            server.Start();

            await Task.Delay(-1);
        }
    }
}

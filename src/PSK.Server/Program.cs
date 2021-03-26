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
                .Configure<TcpListenerOptions>(configuration.GetSection(nameof(TcpListenerOptions)))
                .Configure<RequestChannelOptions>(configuration.GetSection(nameof(RequestChannelOptions)))
                .Configure<ServerOptions>(configuration.GetSection(nameof(ServerOptions)))
                //Client Service
                .AddSingleton<IClientService, ClientService>()
                //Channel
                .AddSingleton<IRequestChannel, RequestChannel>()
                //Listeners
                .AddSingleton<ITcpListener, TcpListener>()
                //Transceivers
                .AddTransient<ITcpTransceiver, TcpTransceiver>()
                //Services
                .AddSingleton<IPingService, PingService>()
                //Server
                .AddSingleton<IServer, Server>()
                .BuildServiceProvider();

            //TODO: Allow to remove/add dynamically available services depending on ConfigurationService

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var server = serviceProvider.GetRequiredService<IServer>();

            server.Start();

            await Task.Delay(-1);
        }
    }
}

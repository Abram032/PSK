using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core;
using PSK.Core.Options;
using PSK.Protocols.Tcp;
using PSK.Services;
using PSK.Services.Configure;
using PSK.Services.Ping;
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
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
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
                .Configure<PingServiceOptions>(configuration.GetSection(nameof(PingServiceOptions)))
                //Client Service
                .AddSingleton<IClientService, ClientService>()
                //Channel
                .AddSingleton<IRequestChannel, RequestChannel>()
                //Listeners
                .AddSingleton<ITcpListener, TcpListener>()
                //Transceivers
                .AddTransient<ITcpTransceiver, TcpTransceiver>()
                //Services
                .AddSingleton<IConfigureServices, ConfigureServices>()
                .AddTransient<IPingService>(provider =>
                {
                    var options = provider.GetRequiredService<IOptionsMonitor<PingServiceOptions>>();
                    return options.CurrentValue.IsActive ? new PingService(options) : null;
                })
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

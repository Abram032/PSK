using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core;
using PSK.Core.Models;
using PSK.Core.Options;
using PSK.Protocols.Tcp;
using PSK.Services;
using PSK.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PSK.Server
{
    public class Server : IServer
    {
        private IReadOnlyList<IListener> listeners;
        private IReadOnlyDictionary<string, Type> serviceTypes;
        //private IDisposable monitor;

        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        private readonly IOptionsMonitor<ServerOptions> _options;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Channel<Message> _requestChannel;
        private readonly IClientService _clientService;

        public Server(IOptionsMonitor<ServerOptions> options, ILogger<Server> logger, Channel<Message> requestChannel, 
            IServiceProvider serviceProvider, IClientService clientService)
        {
            _options = options;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _requestChannel = requestChannel;
            _clientService = clientService;
        }

        //FIX: OnChange fires twice due to a bug in File Watcher
        //private void OnOptionsChanged(ServerOptions options)
        //{
        //    _logger.LogWarning("Configuration change detected! Restarting server!");
        //    Stop().Wait();
        //    Start();
        //}

        public void Start()
        {
            _logger.LogInformation("Starting server");

            //Threading
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            //Service configuration
            serviceTypes = Assembly.GetAssembly(typeof(IService))
                .GetTypes()
                .Where(
                    t => t.IsAssignableTo(typeof(IService)) &&
                    t != typeof(IService) &&
                    t.IsInterface &&
                    t.GetCustomAttribute<Command>() is not null
                )
                .ToDictionary(s => s.GetCustomAttribute<Command>()?.Value);

            //Listener configuration
            listeners = Assembly.GetAssembly(typeof(TcpListener))
                .GetTypes()
                .Where(
                    t => t.IsAssignableTo(typeof(IListener)) &&
                    t != typeof(IListener) &&
                    t.IsInterface
                )
                .Select(t => _serviceProvider.GetService(t) as IListener)
                .ToList();

            foreach (var listener in listeners)
            {
                listener.Start();
            }

            for (int i = 0; i < _options.CurrentValue.RequestWorkers; i++)
            {
                Task.Factory.StartNew(() => RequestWorker(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            //Options monitor
            //monitor = _options.OnChange(OnOptionsChanged);

            _logger.LogInformation($"Available services: {string.Join(", ", serviceTypes.Keys)}");
            _logger.LogInformation($"Available protocols: {string.Join(", ", listeners.Select(r => r.GetType().Name.Replace("Listener", "")))}");
            _logger.LogInformation($"Number of request workers: {_options.CurrentValue.RequestWorkers}");
            _logger.LogInformation("Server started");
        }

        public async Task Stop()
        {
            _logger.LogInformation("Stopping server");

            //monitor.Dispose();
            foreach (var listener in listeners)
            {
                listener.Stop();
                listener.Dispose();
            }
            _clientService.ClearClients();
            _requestChannel.Reader.ReadAllAsync(cancellationToken);
            cancellationTokenSource.Cancel();

            _logger.LogInformation("Server stopped");
        }

        private async Task RequestWorker()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (await _requestChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    var request = await _requestChannel.Reader.ReadAsync(cancellationToken);
                    var client = _clientService.GetClientById(request.ClientId);

                    if (client == null)
                    {
                        _logger.LogWarning($"Could not find active transmitter for client '{request.ClientId}'");
                        continue;
                    }

                    try
                    {
                        var response = await ProcessRequest(request);
                        await client.Transceiver.Transmit(response);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                        if(client != null)
                        {
                            await client.Transceiver.Transmit(e.Message);
                        }
                    }
                }
            }
        }

        public async Task<string> ProcessRequest(Message request)
        {
            if (serviceTypes.TryGetValue(request.Command, out var serviceType))
            {
                var service = _serviceProvider.GetService(serviceType) as IService;
                
                if(service != null)
                {
                    return await service.ProcessRequest(request.ClientId, request.Data);
                }                
            }

            var reason = $"Could not find service for '{request.Command}' command.";
            _logger.LogWarning($"Processing '{request.Command}' command for client '{request.ClientId}' failed. {reason}");
            return reason;
        }

        public void Dispose()
        {
            Stop().Wait();
            cancellationTokenSource.Dispose();
        }
    }
}

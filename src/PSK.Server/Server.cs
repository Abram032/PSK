using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core;
using PSK.Core.Models;
using PSK.Protocols.Tcp;
using PSK.Services;
using PSK.Services.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Server
{
    public class Server : IServer
    {
        private IReadOnlyList<IReceiver> receivers;
        private ConcurrentDictionary<Guid, ITransmitter> transmitters;
        private IReadOnlyDictionary<string, Type> serviceTypes;

        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        private readonly IOptions<ServerOptions> _options;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRequestChannel _requestChannel;

        public Server(IOptions<ServerOptions> options, ILogger<Server> logger, IRequestChannel requestChannel, IServiceProvider serviceProvider)
        {
            _options = options;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _requestChannel = requestChannel;
        }

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

            //Receiver configuration
            receivers = Assembly.GetAssembly(typeof(TcpReceiver))
                .GetTypes()
                .Where(
                    t => t.IsAssignableTo(typeof(IReceiver)) &&
                    t != typeof(IReceiver) &&
                    t.IsInterface
                )
                .Select(t => _serviceProvider.GetService(t) as IReceiver)
                .ToList();

            foreach (var receiver in receivers)
            {
                receiver.OnConnected += OnConnected;
                receiver.OnDisconnected += OnDisconnected;
            }

            //Trasmitter configuration
            transmitters = new ConcurrentDictionary<Guid, ITransmitter>();

            foreach (var receiver in receivers)
            {
                receiver.Start();
            }

            for (int i = 0; i < _options.Value.RequestWorkers; i++)
            {
                Task.Factory.StartNew(() => RequestWorker(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            _logger.LogInformation($"Available services: {string.Join(", ", serviceTypes.Keys)}");
            _logger.LogInformation($"Available protocols: {string.Join(", ", receivers.Select(r => r.GetType().Name.Replace("Receiver", "")))}");
            _logger.LogInformation($"Number of request workers: {_options.Value.RequestWorkers}");
            _logger.LogInformation("Server started");
        }

        public async Task Stop()
        {
            _logger.LogInformation("Stopping server");

            foreach (var receiver in receivers)
            {
                receiver.OnConnected -= OnConnected;
                receiver.OnDisconnected -= OnDisconnected;
                receiver.Stop();
            }
            await _requestChannel.ClearAsync(cancellationToken);
            cancellationTokenSource.Cancel();

            _logger.LogInformation("Server stopped");
        }

        private async Task RequestWorker()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (await _requestChannel.WaitToReadAsync(cancellationToken))
                {
                    var request = await _requestChannel.ReadAsync(cancellationToken);

                    if (!transmitters.TryGetValue(request.ClientId, out var transmitter))
                    {
                        _logger.LogWarning($"Could not find active transmitter for client '{request.ClientId}'");
                        continue;
                    }

                    try
                    {
                        var arguments = request.Data.Split(' ');
                        if (arguments.Length != 2)
                        {
                            _logger.LogWarning($"Bad request from client '{request.ClientId}'. Invalid amount of arguments.");
                            await transmitter.Transmit("Bad request. Invalid amount of arguments!");
                            continue;
                        }
                        var command = arguments.FirstOrDefault();
                        var data = arguments.LastOrDefault();

                        if (!serviceTypes.TryGetValue(command, out var serviceType))
                        {
                            var reason = $"Could not find service for '{command}' command.";
                            _logger.LogWarning($"Processing '{command}' command for client '{request.ClientId}' failed. {reason}");
                            await transmitter.Transmit(reason);
                            continue;
                        }

                        var service = _serviceProvider.GetService(serviceType) as IService;
                        var serviceResponse = await service.ProcessRequest(data);

                        await transmitter.Transmit(serviceResponse);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
            }
        }

        private void OnConnected(object sender, OnConnectedEventArgs e)
        {
            string protocol = null;
            switch (e.Client)
            {
                case TcpClient client:
                    protocol = "TCP";
                    var trasnmitter = _serviceProvider.GetService(typeof(ITcpTransmitter)) as ITcpTransmitter;
                    trasnmitter.Start(client);
                    transmitters.TryAdd(e.ClientId, trasnmitter);
                    break;
                default:
                    protocol = "unknown";
                    break;
            }
            _logger.LogInformation($"New client '{e.ClientId}' connected using '{protocol}' protocol");
            _logger.LogInformation($"Number of clients connected: {transmitters.Count}");
        }

        private void OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            _logger.LogInformation($"Client '{e.ClientId}' disconnected");
            if (!transmitters.TryRemove(e.ClientId, out var transmitter))
            {
                _logger.LogWarning($"Could not find and remove transmitter for client '{e.ClientId}'");
                return;
            }
            transmitter.Dispose();
        }

        public void Dispose()
        {
            Stop().Wait();
            cancellationTokenSource.Dispose();
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PSK.Server
{
    public class Server : IServer
    {
        private readonly IReadOnlyList<IReceiver> receivers;
        private readonly ConcurrentDictionary<Guid, ITransmitter> transmitters;
        private readonly IReadOnlyDictionary<string, Type> serviceTypes;
        private readonly Channel<OnReceivedEventArgs> requestChannel;

        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenSource cancellationTokenSource;

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;

        public Server(IConfiguration configuration, ILogger<Server> logger, IServiceProvider serviceProvider)
        {
            //Dependency injection
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;

            //Request channel for request worker
            _logger.LogDebug("Creating request channel");
            requestChannel = Channel.CreateUnbounded<OnReceivedEventArgs>();

            //Service configuration
            _logger.LogDebug("Configuring services");
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
            _logger.LogDebug("Configuring receivers");
            receivers = Assembly.GetAssembly(typeof(TcpReceiver))
                .GetTypes()
                .Where(
                    t => t.IsAssignableTo(typeof(IReceiver)) &&
                    t != typeof(IReceiver) &&
                    t.IsInterface
                )
                .Select(t => serviceProvider.GetService(t) as IReceiver)
                .ToList();

            foreach(var receiver in receivers)
            {
                receiver.OnConnected += OnConnected;
                receiver.OnDisconnected += OnDisconnected;
                receiver.OnReceived += async(s, e) => await OnReceived(s, e);
            }

            //Trasmitter configuration
            _logger.LogDebug("Configuring transmitters");
            transmitters = new ConcurrentDictionary<Guid, ITransmitter>();

            //Threading
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            _logger.LogInformation($"Available services: {string.Join(", ", serviceTypes.Keys)}");
            _logger.LogInformation($"Available protocols: {string.Join(", ", receivers.Select(x => x.GetType().Name.Replace("Receiver", "")))}");
        }

        public void Start()
        {
            _logger.LogInformation("Starting server");

            foreach (var receiver in receivers)
            {
                _logger.LogDebug($"Staring {receiver.GetType().Name}");
                receiver.Start();
            }

            _logger.LogDebug($"Configuring request workers");
            var workerCountRaw = _configuration["Server:RequestWorkers"];
            if (!int.TryParse(workerCountRaw, out int workerCount))
            {
                _logger.LogError($"Could not parse worker count '{workerCountRaw}' to int");
                return;
            }

            for(int i = 0; i < workerCount; i++)
            {
                _logger.LogDebug($"Starting request worker #{i + 1}");
                Task.Factory.StartNew(() => RequestHandler(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            _logger.LogInformation("Server started");
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping server");
            foreach (var receiver in receivers)
            {
                _logger.LogDebug($"Stopping {receiver.GetType().Name}");
                receiver.Stop();
            }
            foreach(var client in transmitters.Keys)
            {
                if (!transmitters.TryRemove(client, out var transmitter))
                {
                    _logger.LogWarning($"Could not find and remove client '{client}'");
                    continue;
                }
                transmitter.Dispose();
            }
            if(!requestChannel.Writer.TryComplete())
            {
                _logger.LogWarning($"Could not complete request channel");
            }
            cancellationTokenSource.Cancel();
            _logger.LogInformation("Server stopped");
        }

        private async Task RequestHandler()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while(await requestChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    var request = await requestChannel.Reader.ReadAsync(cancellationToken);
                    _logger.LogDebug($"Processing request for client '{request.ClientId}'");

                    if (!transmitters.TryGetValue(request.ClientId, out var transmitter))
                    {
                        _logger.LogWarning($"Could not find active transmitter for client '{request.ClientId}'");
                        continue;
                    }

                    var arguments = request.Data.Split(' ');
                    if(arguments.Length != 2)
                    {
                        _logger.LogWarning($"Bad request from client '{request.ClientId}'. Invalid amount of arguments.");
                        transmitter.Transmit("Bad request. Invalid amount of arguments!");
                        continue;
                    }
                    var command = arguments.FirstOrDefault();
                    var data = arguments.LastOrDefault();

                    _logger.LogDebug($"Processing '{command}' command for client '{request.ClientId}' ({data.Length} bytes)");
                    if (!serviceTypes.TryGetValue(command, out var serviceType))
                    {
                        var reason = $"Could not find service for '{command}' command.";
                        _logger.LogWarning($"Processing '{command}' command for client '{request.ClientId}' failed. {reason}");
                        transmitter.Transmit(reason);
                        continue;
                    }

                    var service = _serviceProvider.GetService(serviceType) as IService;
                    var serviceResponse = await service.ProcessRequest(data);
                    _logger.LogDebug($"Processed '{command}' command for client '{request.ClientId}'");
                    
                    _logger.LogDebug($"Sending response to client '{request.ClientId}' ({serviceResponse.Length} bytes)");
                    transmitter.Transmit(serviceResponse);
                    _logger.LogDebug($"Response sent to client '{request.ClientId}'");
                }
            }
        }

        private async Task OnReceived(object sender, OnReceivedEventArgs e)
        {
            _logger.LogInformation($"Request received from client '{e.ClientId}' ({e.Data.Length} bytes)");
            await requestChannel.Writer.WriteAsync(e, cancellationToken);
        }

        private void OnConnected(object sender, OnConnectedEventArgs e)
        {
            string protocol = null;
            switch (e.Client)
            {
                case TcpClient client:
                    protocol = "TCP";
                    transmitters.TryAdd(e.ClientId, new TcpTransmitter(client));
                    break;
                default:
                    protocol = "unknown";
                    break;
            }
            _logger.LogInformation($"New client '{e.ClientId}' connected using '{protocol}' protocol");
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
            Stop();
            cancellationTokenSource.Dispose();
        }
    }
}

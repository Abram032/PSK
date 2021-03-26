﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core;
using PSK.Protocols.Tcp;
using PSK.Services;
using PSK.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Server
{
    public class Server : IServer
    {
        private IReadOnlyList<IListener> listeners;
        private IReadOnlyDictionary<string, Type> serviceTypes;

        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        private readonly IOptions<ServerOptions> _options;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRequestChannel _requestChannel;
        private readonly IClientService _clientService;

        public Server(IOptions<ServerOptions> options, ILogger<Server> logger, IRequestChannel requestChannel, 
            IServiceProvider serviceProvider, IClientService clientService)
        {
            _options = options;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _requestChannel = requestChannel;
            _clientService = clientService;
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

            for (int i = 0; i < _options.Value.RequestWorkers; i++)
            {
                Task.Factory.StartNew(() => RequestWorker(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            _logger.LogInformation($"Available services: {string.Join(", ", serviceTypes.Keys)}");
            _logger.LogInformation($"Available protocols: {string.Join(", ", listeners.Select(r => r.GetType().Name.Replace("Listener", "")))}");
            _logger.LogInformation($"Number of request workers: {_options.Value.RequestWorkers}");
            _logger.LogInformation("Server started");
        }

        public async Task Stop()
        {
            _logger.LogInformation("Stopping server");

            //TODO: Clear clients
            foreach (var listener in listeners)
            {
                listener.Stop();
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
                    var client = _clientService.GetClientById(request.ClientId);

                    if (client == null)
                    {
                        _logger.LogWarning($"Could not find active transmitter for client '{request.ClientId}'");
                        continue;
                    }

                    try
                    {
                        //TODO: Move to separate method and return only answer to transmit whether service is available and returns response or not
                        if (!serviceTypes.TryGetValue(request.Command, out var serviceType))
                        {
                            var reason = $"Could not find service for '{request.Command}' command.";
                            _logger.LogWarning($"Processing '{request.Command}' command for client '{request.ClientId}' failed. {reason}");
                            await client.Transceiver.Transmit(reason);
                            continue;
                        }

                        var service = _serviceProvider.GetService(serviceType) as IService;
                        var serviceResponse = await service.ProcessRequest(request.Data);

                        await client.Transceiver.Transmit(serviceResponse);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
            }
        }

        public void Dispose()
        {
            Stop().Wait();
            cancellationTokenSource.Dispose();
        }
    }
}

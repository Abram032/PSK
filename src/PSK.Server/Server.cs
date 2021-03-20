using Microsoft.Extensions.Configuration;
using PSK.Core;
using PSK.Core.Enums;
using PSK.Core.Models;
using PSK.Core.Server;
using PSK.Protocols.Tcp;
using PSK.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using PSK.Services.Models;

namespace PSK.Server
{
    public class Server : IServer, IDisposable
    {
        private readonly IReadOnlyList<IReceiver> receivers;
        private readonly IReadOnlyDictionary<string, IService> services;
        private readonly ConcurrentDictionary<Guid, ITransmitter> transmitters;
        private readonly ConcurrentQueue<OnReceivedEventArgs> requestQueue;

        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        public Server(IConfiguration configuration)
        {
            //Request queue
            requestQueue = new ConcurrentQueue<OnReceivedEventArgs>();

            //Service configuration
            services = Assembly.GetAssembly(typeof(IService))
                .GetTypes()
                .Where(
                    t => t.IsAssignableTo(typeof(IService)) &&
                    t.GetConstructor(Type.EmptyTypes) is not null &&
                    t.GetCustomAttribute<Command>() is not null
                )
                .Select(t => Activator.CreateInstance(t) as IService)
                .ToDictionary(s => s.GetType().GetCustomAttribute<Command>()?.Value);

            //Receiver configuration
            IReceiver tcpReceiver = new TcpReceiver(configuration);
            receivers = new List<IReceiver>
            {
                tcpReceiver
            };
            foreach(var receiver in receivers)
            {
                receiver.OnConnected += OnConnected;
                receiver.OnDisconnected += OnDisconnected;
                receiver.OnReceived += OnReceived;
            }

            //Trasmitter configuration
            transmitters = new ConcurrentDictionary<Guid, ITransmitter>();

            //Threading
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            foreach(var receiver in receivers)
            {
                receiver.Start();
            }

            cancellationToken = cancellationTokenSource.Token;
            Task.Factory.StartNew(() => RequestHandler(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            foreach(var receiver in receivers)
            {
                receiver.Stop();
            }

            cancellationTokenSource.Cancel();
        }

        private async Task RequestHandler()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if(!requestQueue.IsEmpty && requestQueue.TryDequeue(out var request))
                {
                    var arguments = request.Data.Split(' ');
                    var command = arguments.FirstOrDefault();
                    var data = arguments.LastOrDefault();

                    Console.WriteLine($"{DateTime.Now}: Processing '{command}' command ({data.Length} bytes)...");
                    if (!services.TryGetValue(command, out var service))
                    {
                        Console.WriteLine($"{DateTime.Now}: Error processing '{command}' command ({data.Length} bytes)!");
                        continue;
                    }

                    var serviceResponse = await service.HandleRequest(data);
                    Console.WriteLine($"{DateTime.Now}: Processed '{command}' command!");

                    if(!transmitters.TryGetValue(request.ClientId, out var transmitter))
                    {
                        Console.WriteLine($"{DateTime.Now}: Could not find client '{request.ClientId}'!");
                        continue;
                    }
                    Console.WriteLine($"{DateTime.Now}: Sending response ({serviceResponse.Length} bytes)...");
                    transmitter.Transmit(serviceResponse);
                    Console.WriteLine($"{DateTime.Now}: Response sent!");
                }
            }
        }

        private void OnReceived(object sender, OnReceivedEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now}: Request received ({e.Data.Length} bytes)!");
            requestQueue.Enqueue(e);
        }

        private void OnConnected(object sender, OnConnectedEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now}: New client '{e.ClientId}' connected using '{e.ClientType}' protocol!");
            switch(e.ClientType)
            {
                case ClientType.TCP:
                    transmitters.TryAdd(e.ClientId, new TcpTransmitter((TcpClient)e.Client));
                    break;
            }
        }

        private void OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            if (!transmitters.TryRemove(e.ClientId, out var transmitter))
            {
                Console.WriteLine($"{DateTime.Now}: Could not find and remove client '{e.ClientId}'!");
                return;
            }
            Console.WriteLine($"{DateTime.Now}: Client '{e.ClientId}' disconnected!");
            transmitter.Dispose();
        }

        public void Dispose()
        {
            Stop();
            cancellationTokenSource.Dispose();
        }
    }
}

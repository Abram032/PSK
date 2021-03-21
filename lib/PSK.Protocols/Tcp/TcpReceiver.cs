using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PSK.Core;
using PSK.Core.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Protocols.Tcp
{
    public interface ITcpReceiver : IReceiver { }
    public class TcpReceiver : ITcpReceiver
    {
        private readonly TcpListener listener;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenSource cancellationTokenSource;

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public TcpReceiver(IConfiguration configuration, ILogger<TcpReceiver> logger)
        {
            _configuration = configuration;
            _logger = logger;

            if (!int.TryParse(configuration["Protocols:TCP:ListenPort"], out int tcpPort))
            {
                _logger.LogError($"Could not parse TCP port '{configuration["Protocols:TCP:ListenPort"]}' to int");
                return;
            }

            _logger.LogDebug($"Configuring TCP listener on port: {tcpPort}");
            listener = new TcpListener(IPAddress.Any, tcpPort);
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        public event EventHandler<OnReceivedEventArgs> OnReceived;
        public event EventHandler<OnConnectedEventArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;

        public void Dispose()
        {
            Stop();
            cancellationTokenSource.Dispose();
        }

        public void Listen()
        {
            _logger.LogDebug($"TCP Listener started");
            while (!cancellationToken.IsCancellationRequested)
            {
                //TODO: How to close TcpClient properly and get notified
                int.TryParse(_configuration["Protocols:TCP:ReceiveTimeout"], out int receiveTimeout);
                int.TryParse(_configuration["Protocols:TCP:SendTimeout"], out int sendTimeout);
                var clientId = Guid.NewGuid();
                var client = listener.AcceptTcpClient();
                client.ReceiveTimeout = receiveTimeout;
                client.SendTimeout = sendTimeout;
                client.LingerState = new LingerOption(true, 15);

                OnConnected?.Invoke(this, new OnConnectedEventArgs
                {
                    ClientId = clientId,
                    Client = client
                });

                Task.Factory.StartNew(() => Receive(clientId, client), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        public async Task Receive(Guid clientId, TcpClient client)
        {
            var sb = new StringBuilder();
            var stream = client.GetStream();
            var buffer = new byte[4000];
            while(client.Connected)
            {
                if(!stream.DataAvailable || !stream.CanRead)
                {
                    continue;
                }
                var dataLength = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                while (dataLength > 0)
                {
                    sb.Append(Encoding.ASCII.GetString(buffer, 0, dataLength));
                    if (sb[sb.Length - 1] == '\n')
                    {
                        sb.Remove(sb.Length - 1, 1); //Removing '/n' from the end

                        OnReceived?.Invoke(this, new OnReceivedEventArgs
                        {
                            ClientId = clientId,
                            Data = sb.ToString()
                        });
                        sb.Clear();
                    }
                    dataLength = 0;
                    Array.Clear(buffer, 0, buffer.Length);
                }
            }
            OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs
            {
                ClientId = clientId
            });
        }

        public void Start()
        {
            _logger.LogDebug("Starting TCP Receiver");
            if(listener == null)
            {
                _logger.LogError("Could not start TCP listener");
                return;
            }
            listener.Start();
            Task.Factory.StartNew(() => Listen(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _logger.LogDebug("TCP Receiver started");
        }

        public void Stop()
        {
            _logger.LogDebug("Stopping TCP Receiver");
            cancellationTokenSource.Cancel();
            listener.Stop();
            _logger.LogDebug("TCP Receiver stopped");
        }
    }
}

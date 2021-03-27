using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core;
using PSK.Core.Models;
using PSK.Core.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Protocols.Tcp
{
    public interface ITcpListener : IListener { }
    public class TcpListener : ITcpListener
    {
        private System.Net.Sockets.TcpListener listener;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        private readonly IOptions<TcpListenerOptions> _options;
        private readonly ILogger _logger;
        private readonly IClientService _clientService;
        private readonly IServiceProvider _serviceProvider;

        //TODO: Use IOptionsMonitor<T> and update the listener if configuration changes
        public TcpListener(IOptions<TcpListenerOptions> options, ILogger<TcpListener> logger, IClientService clientService, IServiceProvider serviceProvider)
        {
            _options = options;
            _logger = logger;
            _clientService = clientService;
            _serviceProvider = serviceProvider;
        }

        private void Listen()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = listener.AcceptTcpClient();
                    tcpClient.ReceiveTimeout = _options.Value.ReceiveTimeout;
                    tcpClient.SendTimeout = _options.Value.SendTimeout;

                    var transreceiver = (ITransceiver)_serviceProvider.GetService(typeof(ITcpTransceiver));
                    transreceiver.Start(tcpClient);
                    _clientService.AddClient(new Client
                    {
                        Id = transreceiver.Id,
                        Transceiver = transreceiver
                    });

                    _logger.LogInformation($"New client '{transreceiver.Id}' connected using 'TCP' protocol");
                    _logger.LogInformation($"Number of clients connected: {_clientService.ClientCount}");
                } 
                catch(Exception e)
                {
                    _logger.LogError(e.Message);
                    listener.Stop();
                    listener = null;
                }
            }
        }

        public void Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            listener = new System.Net.Sockets.TcpListener(IPAddress.Any, _options.Value.ListenPort);
            listener.Start();
            Task.Factory.StartNew(() => Listen(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            listener.Stop();
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }
    }
}

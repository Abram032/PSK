using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PSK.Core;
using PSK.Core.Models;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PSK.Protocols.Tcp
{
    public interface ITcpTransceiver : ITransceiver { }
    public class TcpTransceiver : BaseTransceiver, ITcpTransceiver
    {
        protected TcpClient client;
        private readonly Channel<Message> _messageChannel;
        private readonly IClientService _clientService;
        private readonly ILogger _logger;

        public TcpTransceiver(Channel<Message> messageChannel, ILogger<TcpTransceiver> logger = null, IClientService clientService = null)
            : base()
        {
            _logger = logger;
            _clientService = clientService;
            _messageChannel = messageChannel;
        }

        public override void Start(object client = null)
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            this.client = client as TcpClient;

            var reader = PipeReader.Create(this.client.GetStream());
            Task.Factory.StartNew(() => Receive(reader), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Active = true;
        }

        public override async Task Transmit(string data)
        {
            try
            {
                ReadOnlyMemory<byte> response = 
                    data.LastOrDefault().Equals('\n') ? 
                    Encoding.ASCII.GetBytes($"{data}") : Encoding.ASCII.GetBytes($"{data}\n");
                await client.GetStream().WriteAsync(response);
            }
            catch(Exception e)
            {
                LogException(e.Message);
                Stop();
                Dispose();
            }
        }

        public override void Stop()
        {
            Active = false;

            DisconnectClient();
            if(client.Connected)
            {
                client.GetStream().Close();
            }
            client.Close();
            cancellationTokenSource.Cancel();
        }

        public override void Dispose()
        {
            client.Dispose();
            cancellationTokenSource.Dispose();
        }

        protected override void LogException(string message)
        {
            _logger.LogError(message);
        }

        protected override void DisconnectClient()
        {
            _logger.LogInformation($"Client '{Id}' disconnected.");
            _clientService.RemoveClient(Id);
        }

        protected override async ValueTask ProcessMessage(Guid clientId, string command, string data)
        {
            while (await _messageChannel.Writer.WaitToWriteAsync(cancellationToken))
            {
                await _messageChannel.Writer.WriteAsync(new Message
                {
                    ClientId = clientId,
                    Command = command,
                    Data = data
                });
                return;
            }
        }
    }
}

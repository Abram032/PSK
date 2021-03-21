using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core;
using PSK.Core.Models;
using System;
using System.Buffers;
using System.IO.Pipelines;
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
        private TcpListener listener;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        private readonly IOptions<TcpReceiverOptions> _options;
        private readonly ILogger _logger;
        private readonly IRequestChannel _requestChannel;

        public TcpReceiver(IOptions<TcpReceiverOptions> options, ILogger<TcpReceiver> logger, IRequestChannel requestChannel)
        {
            _options = options;
            _logger = logger;
            _requestChannel = requestChannel;
        }

        //public event EventHandler<OnReceivedEventArgs> OnReceived;
        public event EventHandler<OnConnectedEventArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;

        public void Dispose()
        {
            Stop();
            cancellationTokenSource.Dispose();
        }

        private void Listen()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //TODO: How to close TcpClient properly and get notified
                try
                {
                    var clientId = Guid.NewGuid();
                    var client = listener.AcceptTcpClient();
                    client.ReceiveTimeout = _options.Value.ReceiveTimeout;
                    client.SendTimeout = _options.Value.SendTimeout;

                    OnConnected?.Invoke(this, new OnConnectedEventArgs
                    {
                        ClientId = clientId,
                        Client = client
                    });

                    Task.Factory.StartNew(() => Receive(clientId, client), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                } 
                catch(Exception e)
                {
                    _logger.LogError(e.Message);
                    listener.Stop();
                    listener = null;
                }
            }
        }

        private async Task Receive(Guid clientId, TcpClient client)
        {
            var reader = PipeReader.Create(client.GetStream());
            while(client.Connected || !cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                {
                    await ProcessLine(clientId, line);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
            }
            await reader.CompleteAsync();
            OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs
            {
                ClientId = clientId
            });
        }

        private bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            SequencePosition? position = buffer.PositionOf((byte)'\n');
            if(!position.HasValue)
            {
                line = default;
                return false;
            }

            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }

        private async ValueTask ProcessLine(Guid clientId, ReadOnlySequence<byte> line)
        {
            var stringBuilder = new StringBuilder();
            foreach(var segment in line)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(segment.Span));
            }

            while(await _requestChannel.WaitToWriteAsync(cancellationToken))
            {
                await _requestChannel.WriteAsync(new Request
                {
                    ClientId = clientId,
                    Data = stringBuilder.ToString()
                });
                return;
            }
        }

        public void Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            listener = new TcpListener(IPAddress.Any, _options.Value.ListenPort);
            listener.Start();
            Task.Factory.StartNew(() => Listen(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            listener.Stop();
            cancellationTokenSource.Cancel();
        }
    }
}

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

        public event EventHandler<OnConnectedEventArgs> OnConnected;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;

        public void Dispose()
        {
            Stop();
            cancellationTokenSource.Dispose();
        }

        //TOOD: Allow only listening in class, rename to ITcpListener or something around that
        private void Listen()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
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

        //TODO: Move to Transrecveiver
        private async Task Receive(Guid clientId, TcpClient client)
        {
            var reader = PipeReader.Create(client.GetStream());
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult result = await reader.ReadAsync(cancellationToken);
                    if(result.IsCompleted)
                    {
                        //Client disconnected
                        break;
                    }
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                    {
                        await ProcessLine(clientId, line);
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
            }
            finally
            {
                await reader.CompleteAsync();
                OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs
                {
                    ClientId = clientId
                });
            }
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
            SequencePosition? position = line.PositionOf((byte)' ');
            if(!position.HasValue)
            {
                //Discard read line, incomplete request
                //TODO: Send response
                return;
            }

            var command = line.Slice(0, position.Value);
            var data = line.Slice(line.GetPosition(1, position.Value));

            //Build command
            var stringBuilder = new StringBuilder();
            foreach(var segment in command)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(segment.Span).ToLower());
            }
            var parsedCommand = stringBuilder.ToString();
            //Build data
            stringBuilder.Clear();
            foreach (var segment in data)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(segment.Span));
            }
            var parsedData = stringBuilder.ToString();

            while (await _requestChannel.WaitToWriteAsync(cancellationToken))
            {
                await _requestChannel.WriteAsync(new Request
                {
                    ClientId = clientId,
                    Command = parsedCommand,
                    Data = parsedData
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PSK.Core;
using PSK.Core.Models;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Protocols.Tcp
{
    public interface ITcpTransceiver : ITransceiver { }
    public class TcpTransceiver : ITcpTransceiver
    {
        private TcpClient client;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        private readonly ILogger _logger;
        private readonly IRequestChannel _requestChannel;
        private readonly IClientService _clientService;

        public Guid Id { get; set; }

        public TcpTransceiver(ILogger<TcpTransceiver> logger, IRequestChannel requestChannel, IClientService clientService)
        {
            _logger = logger;
            _requestChannel = requestChannel;
            _clientService = clientService;

            Id = Guid.NewGuid();
        }

        public void Start(object client)
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            this.client = client as TcpClient;

            Task.Factory.StartNew(() => Receive(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task Transmit(string data)
        {
            try
            {
                ReadOnlyMemory<byte> response = Encoding.ASCII.GetBytes($"{data}");
                await client.GetStream().WriteAsync(response);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                Stop();
                Dispose();
            }
        }

        private async Task Receive()
        {
            var reader = PipeReader.Create(client.GetStream());
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ReadResult result = await reader.ReadAsync(cancellationToken);
                    if (result.IsCompleted)
                    {
                        break;
                    }
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                    {
                        await ProcessLine(Id, line);
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            finally
            {
                await reader.CompleteAsync();
                Stop();
                Dispose();
            }
        }

        private bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            SequencePosition? position = buffer.PositionOf((byte)'\n');
            if (!position.HasValue)
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
            if (!position.HasValue)
            {
                await Transmit("Bad request.");
                return;
            }

            var command = line.Slice(0, position.Value);
            var data = line.Slice(line.GetPosition(1, position.Value));

            //Build command
            var stringBuilder = new StringBuilder();
            foreach (var segment in command)
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

        public void Stop()
        {
            _logger.LogInformation($"Client '{Id}' disconnected");
            cancellationTokenSource.Cancel();
            client.GetStream().Close();
            client.Close();
            _clientService.RemoveClient(Id);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}

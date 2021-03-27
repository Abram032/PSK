using PSK.Protocols.Tcp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Client
{
    public class TcpTransceiver : ITcpTransceiver
    {
        private TcpClient client;
        private Stopwatch stopwatch;

        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;

        public Guid Id { get; }

        public TcpTransceiver()
        {
            Id = Guid.NewGuid();
            stopwatch = new Stopwatch();
        }
        public void Start(object client)
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            this.client = client as TcpClient;

            Task.Factory.StartNew(() => Receive(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            if (client.Connected)
            {
                client.GetStream().Close();
            }
            client.Close();
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            client.Dispose();
            cancellationTokenSource.Dispose();
        }

        public async Task Transmit(string data)
        {
            try
            {
                ReadOnlyMemory<byte> response = Encoding.ASCII.GetBytes($"{data}");
                stopwatch.Start();
                await client.GetStream().WriteAsync(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Stop();
                Dispose();
            }
        }

        public async Task Receive()
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
                Console.WriteLine(e.Message);
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
            var stringBuilder = new StringBuilder();
            foreach (var segment in line)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(segment.Span));
            }

            stopwatch.Stop();
            Console.WriteLine(stringBuilder.ToString());
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Reset();
        }
    }
}

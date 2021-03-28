using Microsoft.Extensions.Logging;
using PSK.Core;
using PSK.Core.Models;
using PSK.Protocols.Tcp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PSK.Protocols
{
    public abstract class BaseTransceiver : ITransceiver
    {
        protected CancellationToken cancellationToken;
        protected CancellationTokenSource cancellationTokenSource;

        public Guid Id { get; }
        public bool Active { get; protected set; }

        public BaseTransceiver()
        {
            Id = Guid.NewGuid();
        }

        public abstract void Dispose();

        public abstract void Start(object client = null);

        public abstract void Stop();

        public abstract Task Transmit(string data);

        protected abstract ValueTask ProcessMessage(Guid clientId, string command, string data);

        protected virtual void DisconnectClient() { }

        protected virtual async Task Receive(PipeReader reader)
        {
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
                LogException(e.Message);
            }
            finally
            {
                await reader.CompleteAsync();
                Stop();
                Dispose();
            }
        }

        protected virtual void LogException(string message)
        {
            Console.WriteLine(message);
        }

        protected virtual bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
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

        protected virtual async ValueTask ProcessLine(Guid clientId, ReadOnlySequence<byte> line)
        {
            SequencePosition? position = line.PositionOf((byte)' ');
            if (!position.HasValue)
            {
                await ProcessCommandlessLine(clientId, line);
            }
            else
            {
                await ProcessCommandLine(clientId, line);
            }
        }

        protected virtual async Task ProcessCommandLine(Guid clientId, ReadOnlySequence<byte> line)
        {
            SequencePosition? position = line.PositionOf((byte)' ');
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
            await ProcessMessage(clientId, parsedCommand, parsedData);
        }

        protected virtual Task ProcessCommandlessLine(Guid clientId, ReadOnlySequence<byte> line)
        {
            return Transmit("Bad request. Invalid amount of arguments.");
        }
    }
}

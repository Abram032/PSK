using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PSK.Server.Core
{
    public class RequestChannel : IRequestChannel
    {
        private Channel<Request> requestChannel;

        private readonly ILogger _logger;
        private readonly IOptions<RequestChannelOptions> _options;

        public RequestChannel(ILogger<RequestChannel> logger, IOptions<RequestChannelOptions> options)
        {
            _logger = logger;
            _options = options;

            CreateChannel();
        }

        private void CreateChannel()
        {
            if (_options.Value.Capacity > 0)
            {
                requestChannel = Channel.CreateBounded<Request>(_options.Value.Capacity);
            }
            else
            {
                requestChannel = Channel.CreateUnbounded<Request>();
            }
        }
        public async ValueTask WriteAsync(Request request, CancellationToken token = default)
        {
            await requestChannel.Writer.WriteAsync(request, token);
            _logger.LogInformation($"Request received from client '{request.ClientId}'");
        }
        public ValueTask<bool> WaitToWriteAsync(CancellationToken token = default) => requestChannel.Writer.WaitToWriteAsync(token);
        public ValueTask<Request> ReadAsync(CancellationToken token = default) => requestChannel.Reader.ReadAsync(token);
        public ValueTask<bool> WaitToReadAsync(CancellationToken token = default) => requestChannel.Reader.WaitToReadAsync(token);
        public IAsyncEnumerable<Request> ReadAllAsync(CancellationToken token = default) => requestChannel.Reader.ReadAllAsync(token);
        public async Task ClearAsync(CancellationToken token = default)
        {
            requestChannel.Writer.Complete();
            if(requestChannel.Reader.CanCount && requestChannel.Reader.Count > 0)
            {
                _logger.LogWarning($"Discarding {requestChannel.Reader.Count} requests from the channel");
            }
            CreateChannel();
        }
    }
}

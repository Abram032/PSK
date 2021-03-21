using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSK.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Core
{
    public class RequestQueue : IRequestChannel
    {
        private readonly ConcurrentQueue<Request> requestQueue;

        private readonly ILogger _logger;
        private readonly IOptions<RequestChannelOptions> _options;

        public RequestQueue(ILogger<RequestChannel> logger, IOptions<RequestChannelOptions> options)
        {
            _logger = logger;
            _options = options;

            requestQueue = new ConcurrentQueue<Request>();
        }

        public Task ClearAsync(CancellationToken token = default)
        {
            requestQueue.Clear();
            return;
        }

        public IAsyncEnumerable<Request> ReadAllAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Request> ReadAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> WaitToReadAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> WaitToWriteAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask WriteAsync(Request request, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}

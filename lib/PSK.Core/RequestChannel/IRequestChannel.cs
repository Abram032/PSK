using PSK.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface IRequestChannel
    {
        ValueTask WriteAsync(Request request, CancellationToken token = default);
        ValueTask<bool> WaitToWriteAsync(CancellationToken token = default);
        ValueTask<Request> ReadAsync(CancellationToken token = default);
        ValueTask<bool> WaitToReadAsync(CancellationToken token = default);
        IAsyncEnumerable<Request> ReadAllAsync(CancellationToken token = default);
        Task ClearAsync(CancellationToken token = default);
    }
}

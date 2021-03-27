using System;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface ITransceiver : IDisposable
    {
        Guid Id { get; }

        void Start(object client);
        void Stop();
        Task Transmit(string data);
    }
}

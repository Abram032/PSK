using System;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface ITransceiver : IDisposable
    {
        Guid Id { get; }
        bool Active { get; }

        void Start(object client = null);
        void Stop();
        Task Transmit(string data);
    }
}

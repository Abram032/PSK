using System;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface ITransmitter : IDisposable
    {
        void Start(object client);
        void Stop();
        Task Transmit(string data);
    }
}

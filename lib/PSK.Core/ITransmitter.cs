using System;

namespace PSK.Core
{
    public interface ITransmitter : IDisposable
    {
        void Transmit(string data);
    }
}

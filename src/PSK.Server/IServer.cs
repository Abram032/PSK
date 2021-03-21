using System;

namespace PSK.Server
{
    public interface IServer : IDisposable
    {
        void Start();
        void Stop();
    }
}

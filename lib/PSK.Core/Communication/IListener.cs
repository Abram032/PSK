using System;

namespace PSK.Core
{
    public interface IListener : IDisposable
    {
        void Start();
        void Stop();
    }
}

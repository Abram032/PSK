using PSK.Core.Models;
using System;

namespace PSK.Core
{
    public interface IReceiver : IDisposable
    {
        void Start();
        void Stop();

        event EventHandler<OnReceivedEventArgs> OnReceived;
        event EventHandler<OnConnectedEventArgs> OnConnected;
        event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
    }
}

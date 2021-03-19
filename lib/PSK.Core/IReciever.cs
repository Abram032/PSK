using System;
using System.Threading.Tasks;
using PSK.Core.Models;

namespace PSK.Core
{
    public interface IReceiver
    {
        void Start();
        void Stop();
        Task Receive();

        event EventHandler<OnReceivedEventArgs> OnReceived;
    }
}

using System;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface IReceiver
    {
        void Start();
        void Stop();
        Task Receive();
    }
}

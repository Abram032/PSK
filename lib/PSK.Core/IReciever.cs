using System;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface IReciever
    {
        void Start();
        void Stop();
        Task Recieve();
    }
}

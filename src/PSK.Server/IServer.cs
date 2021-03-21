using System;
using System.Threading.Tasks;

namespace PSK.Server
{
    public interface IServer : IDisposable
    {
        void Start();
        Task Stop();
    }
}

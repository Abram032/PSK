using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface ITransmitter
    {
        void Start();
        void Stop();
        Task Transmit();
    }
}

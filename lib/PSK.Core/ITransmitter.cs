using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Core
{
    public interface ITransmitter : IDisposable
    {
        void Transmit(string data);
    }
}

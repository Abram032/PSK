using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Core.Server
{
    public interface IService
    {
        Task HandleRequest(OnReceivedEventArgs arguments);
    }
}

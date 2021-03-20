using PSK.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Services
{
    public interface IService
    {
        Task<string> HandleRequest(string request);
    }
}

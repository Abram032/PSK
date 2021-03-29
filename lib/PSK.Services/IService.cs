using System;
using System.Threading.Tasks;

namespace PSK.Services
{
    public interface IService
    {
        Task<string> ProcessRequest(string request);
    }
}

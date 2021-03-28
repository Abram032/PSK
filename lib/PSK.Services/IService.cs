using System;
using System.Threading.Tasks;

namespace PSK.Services
{
    public interface IService
    {
        Task<string> ProcessRequest(Guid clientId, string request);
    }
}

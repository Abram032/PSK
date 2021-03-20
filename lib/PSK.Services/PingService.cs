using Newtonsoft.Json;
using PSK.Services.Models;
using System;
using System.Threading.Tasks;

namespace PSK.Services
{
    [Command("ping")]
    public class PingService : IService
    {
        public async Task<string> HandleRequest(string request) => request;
    }
}

using PSK.Services.Models;
using System.Threading.Tasks;

namespace PSK.Services
{
    [Command("ping")]
    public interface IPingService : IService { }

    public class PingService : IPingService
    {
        public async Task<string> ProcessRequest(string request) => request;
    }
}

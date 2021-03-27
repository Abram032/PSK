using Microsoft.Extensions.Options;
using PSK.Core.Options;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Services.Ping
{
    public class PingService : IPingService
    {
        private static Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private readonly PingServiceOptions _options;

        public PingService(IOptionsSnapshot<PingServiceOptions> options)
        {
            _options = options.Value;
        }

        public async Task<string> ProcessRequest(string request)
        {
            var bytes = request.Split(' ').FirstOrDefault();

            if(!int.TryParse(bytes, out int length) || length < 0 || length > _options.MaxDataSize)
            {
                return "Invalid argument or value.\n";
            }

            return $"{new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray())}\n";
        }
    }
}

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSK.Core.Models.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Services.Configure
{
    public class ConfigureServices : IConfigureServices
    {
        public IConfiguration _configuration;
        public ConfigureServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //TODO: Move to System.Text.Json
        public async Task<string> ProcessRequest(string request)
        {
            var decodedRequest = Encoding.UTF8.GetString(Convert.FromBase64String(request));
            var configure = JsonConvert.DeserializeObject<ConfigureRequest>(decodedRequest);

            switch (configure.Command)
            {
                case ConfigureCommand.GetConfig:
                    return await GetConfiguration(configure);
                case ConfigureCommand.UpdateConfig:
                    return await UpdateConfiguration(configure);
                default:
                    return "Unknown command for Configure service";
            }
        }

        private async Task<string> GetConfiguration(ConfigureRequest request)
        {
            var config = await GetConfiguration(request.Type);
            if (config != null)
            {
                return config;
            }

            return "Failed to retrieve configuration!\n";
        }

        private async Task<string> GetConfiguration(Type type)
        {
            var path = "appsettings.json";
            if (!File.Exists(path))
            {
                return null;
            }

            var file = await File.ReadAllTextAsync(path);

            if(type == null)
            {
                return $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(file))}\n";
            }


            var jObject = JsonConvert.DeserializeObject<JObject>(file);
            if (!jObject.TryGetValue(type.Name, out JToken section))
            {
                return null;
            }
            var sectionObject = JsonConvert.DeserializeObject(section.ToString(), type);
            return $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sectionObject)))}\n";
        }

        private async Task<string> UpdateConfiguration(ConfigureRequest request)
        {
            if (await UpdateConfiguration(request.Type, request.Options))
            {
                return "Configuration updated!\n";
            }

            return "Failed to update configuration!\n";
        }

        private async Task<bool> UpdateConfiguration(Type type, string options)
        {
            var path = "appsettings.json";
            if(!File.Exists(path))
            {
                return false;
            }

            var file = await File.ReadAllTextAsync(path);
            var jObject = JsonConvert.DeserializeObject<JObject>(file);
            if(!jObject.TryGetValue(type.Name, out JToken section))
            {
                return false;
            }
            var sectionObject = JsonConvert.DeserializeObject(section.ToString(), type);

            JsonConvert.PopulateObject(options, sectionObject);

            jObject[type.Name] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(path, JsonConvert.SerializeObject(jObject, Formatting.Indented));

            return true;
        }
    }
}

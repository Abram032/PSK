using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSK.Core.Models.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Reflection;
using PSK.Core.Options;

namespace PSK.Services.Configure
{
    public class ConfigureServices : IConfigureServices
    {
        public IConfiguration _configuration;
        public ConfigureServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        //tcp configure get PingServiceOptions -IsActive false -Cos true
        //TODO: Move to System.Text.Json
        //TODO: Implement factory for exceptions
        public async Task<string> ProcessRequest(Guid clientId, string request)
        {
            var command = request.Split(' ').FirstOrDefault();
            var serviceOptions = request.Split(' ').Skip(1).FirstOrDefault();
            var options = request.Split('-').AsEnumerable().Skip(1).Select(o => o.Trim().Split(' '));

            var commandEnum = Enum.Parse(typeof(ConfigureCommand), command, true);

            string response = null;
            switch (commandEnum)
            {
                case ConfigureCommand.Get:
                    response = await GetConfiguration(serviceOptions);
                    break;
                case ConfigureCommand.Update:
                    response = await UpdateConfiguration(serviceOptions, options);
                    break;
                default:
                    response = "Unknown command for Configure service";
                    break;
            }
            return $"configure {response}";
        }

        private Type GetServiceType(string serviceOptions) => 
            Assembly.GetAssembly(typeof(ServerOptions)).GetTypes().Where(t => t.Name == serviceOptions).FirstOrDefault();

        private async Task<string> GetConfiguration(string serviceOptions)
        {
            var type = GetServiceType(serviceOptions);

            if(type != null || serviceOptions == null)
            {
                var config = await GetConfiguration(type);
                if (config != null)
                {
                    return config;
                }
            }
            
            return "Failed to retrieve configuration!";
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
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(file));
            }


            var jObject = JsonConvert.DeserializeObject<JObject>(file);
            if (!jObject.TryGetValue(type.Name, out JToken section))
            {
                return null;
            }
            var sectionObject = JsonConvert.DeserializeObject(section.ToString(), type);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sectionObject, Formatting.Indented)));
        }

        private async Task<string> UpdateConfiguration(string serviceOptions, IEnumerable<string[]> options)
        {
            if(options.Count() == 0 || options.Any(o => o.Length != 2))
            {
                throw new Exception("Invalid arguments count.");
            }

            var type = GetServiceType(serviceOptions);

            if(type == null)
            {
                throw new Exception("Unknown service options.");
            }

            if (await UpdateConfiguration(type, options))
            {
                return "Configuration updated!";
            }

            return "Failed to update configuration!";
        }

        private async Task<bool> UpdateConfiguration(Type type, IEnumerable<string[]> options)
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

            foreach(var option in options)
            {
                var name = option.FirstOrDefault();
                var value = option.LastOrDefault();

                var property = type.GetProperties().Where(p => p.Name == name && p.CanWrite).FirstOrDefault();

                property.SetValue(sectionObject, Convert.ChangeType(value, property.PropertyType));
            }

            jObject[type.Name] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(path, JsonConvert.SerializeObject(jObject, Formatting.Indented));

            return true;
        }
    }
}

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
using PSK.Core.Models.Services.Configure;
using PSK.Core.Models;

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
        //TODO: Implement factory for exceptions
        public async Task<string> ProcessRequest(string data)
        {
            var request = JsonConvert.DeserializeObject<ConfigureRequest>(data);

            Message response;
            switch (request.Command)
            {
                case ConfigureCommand.Get:
                    response = await GetConfiguration(request.ServiceOptions);
                    break;
                case ConfigureCommand.Update:
                    response = await UpdateConfiguration(request.ServiceOptions, request.Options);
                    break;
                default:
                    response = new Message()
                    {
                        Service = Service.Chat,
                        Succeded = false,
                        Error = "Unknown command for Configure service"
                    };
                    break;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
        }

        private Type GetServiceType(string serviceOptions) => 
            Assembly.GetAssembly(typeof(ServerOptions)).GetTypes().Where(t => t.Name == serviceOptions).FirstOrDefault();

        private async Task<Message> GetConfiguration(string serviceOptions)
        {
            var response = new Message
            {
                Service = Service.Configure,
                Succeded = false,
            };

            var type = GetServiceType(serviceOptions);

            if(type == null && serviceOptions != null)
            {
                response.Error = "Failed to retrieve configuration!";
                return response;
            }

            var config = await GetConfiguration(type);

            if (string.IsNullOrEmpty(config))
            {
                response.Error = "Failed to retrieve configuration!";
                return response;
            }

            response.Data = config;
            response.Succeded = true;
            return response;
        }

        private async Task<string> GetConfiguration(Type type)
        {
            var path = "appsettings.json";
            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            var file = await System.IO.File.ReadAllTextAsync(path);

            if(type == null)
            {
                return file;
            }


            var jObject = JsonConvert.DeserializeObject<JObject>(file);
            if (!jObject.TryGetValue(type.Name, out JToken section))
            {
                return null;
            }
            var sectionObject = JsonConvert.DeserializeObject(section.ToString(), type);
            return JsonConvert.SerializeObject(sectionObject, Formatting.Indented);
        }

        private async Task<Message> UpdateConfiguration(string serviceOptions, Dictionary<string, string> options)
        {
            var response = new Message
            {
                Service = Service.Configure,
                Succeded = false,
            };

            if (options.Count() == 0)
            {
                response.Error = "Invalid arguments count.";
                return response;
            }

            var type = GetServiceType(serviceOptions);

            if(type == null)
            {
                response.Error = "Unknown service options.";
                return response;
            }

            if (await UpdateConfiguration(type, options))
            {
                response.Succeded = true;
                response.Data = "Configuration updated!";
                return response;
            }

            response.Error = "Failed to update configuration!";
            return response;
        }

        private async Task<bool> UpdateConfiguration(Type type, Dictionary<string, string> options)
        {
            var path = "appsettings.json";
            if(!System.IO.File.Exists(path))
            {
                return false;
            }

            var file = await System.IO.File.ReadAllTextAsync(path);
            var jObject = JsonConvert.DeserializeObject<JObject>(file);
            if(!jObject.TryGetValue(type.Name, out JToken section))
            {
                return false;
            }
            var sectionObject = JsonConvert.DeserializeObject(section.ToString(), type);

            foreach(var option in options)
            {
                var name = option.Key;
                var value = option.Value;

                var property = type.GetProperties().Where(p => p.Name == name && p.CanWrite).FirstOrDefault();

                property.SetValue(sectionObject, Convert.ChangeType(value, property.PropertyType));
            }

            jObject[type.Name] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(jObject, Formatting.Indented));

            return true;
        }
    }
}

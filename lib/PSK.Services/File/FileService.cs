using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PSK.Core.Models;
using PSK.Core.Models.Services.File;
using PSK.Core.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PSK.Services.File
{
    public class FileService : IFileService
    {
        private readonly FileServiceOptions _options;
        private readonly string basePath;
        public FileService(IOptionsMonitor<FileServiceOptions> options)
        {
            _options = options.CurrentValue;

            basePath = Path.Combine(Environment.CurrentDirectory, _options.BasePath);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
        }

        public async Task<string> ProcessRequest(string data)
        {
            var request = JsonConvert.DeserializeObject<FileRequest>(data);

            Message response;
            switch (request.Command)
            {
                case FileCommand.Get:
                    response = await GetFile(request);
                    break;
                case FileCommand.Put:
                    response = await PutFile(request);
                    break;
                case FileCommand.Delete:
                    response = await DeleteFile(request);
                    break;
                case FileCommand.List:
                    response = await ListFiles(request);
                    break;
                default:
                    response = new Message()
                    {
                        Service = Service.Chat,
                        Succeded = false,
                        Error = "Unknown command for File service"
                    };
                    break;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
        }

        private async Task<Message> GetFile(FileRequest request)
        {
            var message = new Message()
            {
                Service = Service.File,
                Succeded = false,
            };

            var path = Path.Combine(basePath, request.FileName);
            if(!System.IO.File.Exists(path))
            {
                message.Error = "File not found!";
                return message;
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            var data = Convert.ToBase64String(bytes);

            message.Data = data;
            message.Succeded = true;
            message.Headers = new Dictionary<string, string>();
            message.Headers.Add("Filename", request.FileName);
            return message;
        }

        private async Task<Message> PutFile(FileRequest request)
        {
            var message = new Message()
            {
                Service = Service.File,
                Succeded = false,
            };

            var path = Path.Combine(basePath, request.FileName);

            var bytes = Convert.FromBase64String(request.Data);

            await System.IO.File.WriteAllBytesAsync(path, bytes);

            message.Data = $"File {request.FileName} saved.";
            message.Succeded = true;
            return message;
        }

        private async Task<Message> DeleteFile(FileRequest request)
        {
            var message = new Message()
            {
                Service = Service.File,
                Succeded = false,
            };

            var path = Path.Combine(basePath, request.FileName);
            if (!System.IO.File.Exists(path))
            {
                message.Error = "File not found!";
                return message;
            }

            System.IO.File.Delete(path);

            message.Data = $"File {request.FileName} deleted.";
            message.Succeded = true;
            return message;
        }

        private async Task<Message> ListFiles(FileRequest request)
        {
            var files = Directory.GetFiles(basePath);
            var data = $"List of {files.Length} files:\n{string.Join('\n', files)}";

            return new Message()
            {
                Service = Service.File,
                Succeded = true,
                Data = data
            };
        }
    }
}

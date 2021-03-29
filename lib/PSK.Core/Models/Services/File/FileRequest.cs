using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Core.Models.Services.File
{
    public class FileRequest
    {
        public FileCommand Command { get; set; }
        public string FileName { get; set; }
        public string Data { get; set; }
    }
}

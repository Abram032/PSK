using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Core.Models.Services.Configure
{
    public class ConfigureRequest
    {
        public ConfigureCommand Command { get; set; }
        public string ServiceOptions { get; set; }
        public Dictionary<string, string> Options { get; set; }
    }
}

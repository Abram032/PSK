using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Core.Models.Services
{
    public enum ConfigureCommand
    {
        Get,
        Update
    }

    public class ConfigureRequest
    {
        public ConfigureCommand Command { get; set; }
        public Type Type { get; set; }
        public string Options { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PSK.Services.Models
{
    public class Command : Attribute
    {
        public Command(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}

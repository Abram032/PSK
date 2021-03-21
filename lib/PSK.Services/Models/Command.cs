using System;

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

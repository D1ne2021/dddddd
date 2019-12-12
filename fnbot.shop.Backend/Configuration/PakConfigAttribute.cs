using System;

namespace fnbot.shop.Backend.Configuration
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PakConfigAttribute : Attribute
    {
        public PakConfigAttribute(bool requestPaks, params string[] paks)
        {
            RequestPaks = requestPaks;
            if (paks == null || paks.Length == 0)
                Paks = null;
            else
                Paks = paks;
        }

        public bool RequestPaks { get; }
        public string[] Paks { get; }
    }
}

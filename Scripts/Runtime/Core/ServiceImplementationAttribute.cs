using System;

namespace BrunoMikoski.ServicesLocation
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public class ServiceImplementationAttribute : Attribute
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public string Category { get; set; }
    }
}

using System;

namespace BrunoMikoski.ServicesLocation
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class InjectAttribute : Attribute
    {
        public readonly Type ServiceType;


        public InjectAttribute()
        {
        }

        public InjectAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
    public class ServiceImplementationAttribute : Attribute
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public string Category { get; set; }
        public Type[] DependsOn { get; set; }
    }
}

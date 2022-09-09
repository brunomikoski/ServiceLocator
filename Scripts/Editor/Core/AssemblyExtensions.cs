using System;
using System.Linq;
using System.Reflection;

namespace BrunoMikoski.ServicesLocation
{
    public static class AssemblyExtensions
    {
        public static Type[] GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }
    }
}

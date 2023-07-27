using System;
using System.Linq;
using System.Reflection;

namespace BrunoMikoski.ServicesLocation
{
    public static class TypeExtensions
    {
        public static MethodInfo GetInstanceMethod(this Type type, string name)
        {
            return type.GetInstanceMethods().FirstOrDefault(mi => mi.Name == name);
        }
        public static MethodInfo[] GetInstanceMethods(this Type type)
        {
            return type != null
                ? type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                  BindingFlags.FlattenHierarchy)
                : Array.Empty<MethodInfo>();
        }
        
        public static MethodInfo GetInstanceMethod(this Type type, string name, Type[] parameters)
        {
            return type.GetInstanceMethods()
                .FirstOrDefault(mi => mi.Name == name && mi.GetParameters().HasTypes(parameters));
        }
        
        public static bool HasTypes(this ParameterInfo[] parameters, Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            if (parameters.Length != types.Length)
                return false;
            for (int index = 0; index < types.Length; ++index)
            {
                if (types[index] == (Type)null)
                    throw new ArgumentNullException(nameof(types));
                if (parameters[index].ParameterType != types[index])
                    return false;
            }
            return true;
        }
        
    }
}
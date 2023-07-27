using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrunoMikoski.ServicesLocation
{
    public static class ObjectExtensions
    {
        private const string IS_NATIVE_OBJECT_ALIVE = "IsNativeObjectAlive";
        private static Dictionary<string, MethodInfo> nameToMethodInfoCache = new ();

        public static bool IsNativeObjectAlive(this UnityEngine.Object obj)
        {
            return obj.InvokeMethod<bool>(IS_NATIVE_OBJECT_ALIVE);
        }
        
        public static T InvokeMethod<T>(this object obj, string methodName, params object[] list)
        {
            try
            {
                Type type = obj.GetType();
                string key = $"{type.FullName}.{methodName}";

                if (!nameToMethodInfoCache.TryGetValue(key, out MethodInfo method))
                {
                    method = type.GetInstanceMethod(methodName, list.Select(o => o.GetType()).ToArray());
                    if (method == null)
                        return default;
                    
                    nameToMethodInfoCache.Add(key, method);
                }

                return (T)method.Invoke(obj, list);
            }
            catch
            {
                // ignored
            }

            return default;
        }
    }
}
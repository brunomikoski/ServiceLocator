using System.Linq;
using System.Reflection;

namespace BrunoMikoski.ServicesLocation
{
    public static class ObjectExtensions
    {
        public static bool IsNativeObjectAlive(this UnityEngine.Object obj)
        {
            return obj.InvokeMethod<bool>("IsNativeObjectAlive");
        }
        
        public static T InvokeMethod<T>(this object obj, string name, params object[] list)
        {
            try
            {
                MethodInfo method = obj.GetType().GetInstanceMethod(name, list.Select(o => o.GetType()).ToArray());

                if (method == null) return default;

                return (T)method?.Invoke(obj, list);
            }
            catch
            {
                // ignored
            }

            return default;
        }
    }
}
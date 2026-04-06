using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using System.Reflection;

namespace BrunoMikoski.ServicesLocation
{
    public static class SingletonResetter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAllSingletons()
        {
            // Find all types that inherit from SingletonMonoBehaviour<>
            UnityEditor.TypeCache.TypeCollection singletonTypes = TypeCache.GetTypesDerivedFrom(typeof(SingletonMonoBehaviour<>));

            // Reset all instances
            foreach (System.Type type in singletonTypes)
            {
                MethodInfo method = type.GetMethod("ResetSingleton", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                if (method != null)
                {
                    method.Invoke(null, null);
                }
                else
                {
                    Debug.LogError($"ResetAllSingletons: Can't find 'ResetSingleton' method on type '{type.ToString()}'");
                }
            }
        }
    }
}
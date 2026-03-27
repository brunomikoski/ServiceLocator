using UnityEngine;
using UnityEngine.Scripting;

namespace BrunoMikoski.ServicesLocation
{
    [Preserve]
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {

        // This method will be called by the static SingletonResetter script
        public static void ResetSingleton()
        {
            instance = null;
            hasInstance = false;
        }

        private static bool hasInstance;
        private static T instance;
        public static T Instance
        {
            get
            {
                if (!hasInstance)
                {
#if UNITY_6000_0_OR_NEWER
                    instance = FindFirstObjectByType<T>();
#else
                    instance = FindObjectOfType<T>();
#endif
                    if (instance == null)
                    {
                        GameObject obj = new GameObject
                        {
                            name = $"{typeof(T).Name} (Singleton)"
                        };
                        instance = obj.AddComponent<T>();
                    }

                    hasInstance = instance != null;
                }

                return instance;
            }
        }
        
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                hasInstance = true;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
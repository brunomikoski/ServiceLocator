using UnityEngine;
using UnityEngine.Scripting;

namespace BrunoMikoski.ServicesLocation
{
    [Preserve]
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            instance = null;
            hasInstance = false;
        }
#endif

        private static bool hasInstance;
        private static T instance;
        public static T Instance
        {
            get
            {
                if (!hasInstance)
                {
                    instance = FindObjectOfType<T>();
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
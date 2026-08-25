using UnityEngine;
using UnityEngine.Scripting;

namespace BrunoMikoski.ServicesLocation
{
    [Preserve]
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : Component
    {
        static SingletonMonoBehaviour()
        {
            SingletonRegistry.RegisterResetCallback(() =>
            {
                instance = null;
                hasInstance = false;
            });
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
                    instance = FindAnyObjectByType<T>();
#else
                    instance = FindObjectOfType<T>();
#endif
                    if (instance == null)
                        instance = CreateInstance();

                    hasInstance = instance != null;
                }

                return instance;
            }
        }

        private static T CreateInstance()
        {
            string singletonName = $"{typeof(T).Name} (Singleton)";

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject editorHost = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(
                    singletonName, HideFlags.HideAndDontSave);

                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DestroyEditorInstance;
                return editorHost.AddComponent<T>();
            }
#endif

            GameObject host = new GameObject(singletonName);
            return host.AddComponent<T>();
        }

#if UNITY_EDITOR
        private static void DestroyEditorInstance()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= DestroyEditorInstance;

            if (instance != null)
                DestroyImmediate(instance.gameObject);

            instance = null;
            hasInstance = false;
        }
#endif

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

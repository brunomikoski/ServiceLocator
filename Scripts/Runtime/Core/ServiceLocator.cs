using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;
#if UNITASK_ENABLED
using System.Threading;
using Cysharp.Threading.Tasks;
#endif


namespace BrunoMikoski.ServicesLocation
{
    internal static class ServiceLocatorInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            ServiceLocator.IsQuitting = false;
        }
    }

    [Preserve]
    public class ServiceLocator : SingletonMonoBehaviour<ServiceLocator>
    {
        internal static bool IsQuitting { get; set; }

        private readonly Dictionary<Type, object> serviceTypeToInstances = new();

        private readonly Dictionary<Type, List<WeakReference<IServiceObservable>>> serviceTypeToObservables = new();

        private readonly Dictionary<Type, object> servicesWaitingOnDependenciesTobeResolved = new();

        public void RegisterInstance<T>(T serviceInstance)
        {
            Type type = typeof(T);
            RegisterInstanceInternal(type, serviceInstance);
        }

        public void RegisterInstance(Type type, object serviceInstance)
        {
            RegisterInstanceInternal(type, serviceInstance);
        }

        private void RegisterInstanceInternal(Type type, object serviceInstance, bool tryResolveDependencies = true)
        {
            if (!CanRegisterService(type, serviceInstance))
                return;

            if (!IsServiceDependenciesResolved(type))
            {
                servicesWaitingOnDependenciesTobeResolved.Add(type, serviceInstance);
                return;
            }

            serviceTypeToInstances.Add(type, serviceInstance);

            DispatchOnRegistered(type, serviceInstance);
            if (tryResolveDependencies)
                TryResolveDependencies();
        }

        private void DispatchOnRegistered(Type type, object serviceInstance)
        {
            if (serviceInstance is IOnServiceRegistered onRegistered)
                onRegistered.OnRegisteredOnServiceLocator(this);

            DispatchToObservables(type, true);
        }

        private bool CanRegisterService(Type type, object serviceInstance)
        {
            if (HasService(type))
            {
                Debug.LogError($"Service of type {type} is already registered.");
                return false;
            }

            if (serviceInstance is IConditionalService conditionalService)
            {
                if (!conditionalService.CanBeRegistered(this))
                    return false;
            }

            return true;
        }

        public bool HasService<T>()
        {
            return HasService(typeof(T));
        }

        public bool HasService(Type type)
        {
            return serviceTypeToInstances.ContainsKey(type);
        }

        public bool TryGetInstance<T>(out T targetService) where T : class
        {
            Type type = typeof(T);
            if (TryGetRawInstance(type, out object result))
            {
                targetService = result as T;
                return targetService != null;
            }

            targetService = null;
            return false;
        }

        
        public T GetInstance<T>() where T : class
        {
            Type type = typeof(T);
            return (T) GetRawInstance(type);
        }
        
        
        public bool TryGetRawInstance<T>(out T targetInstance) where T : class
        {
            Type type = typeof(T);
            if (TryGetRawInstance(type, out object resultInstance))
            {
                targetInstance = (T) resultInstance;
                return true;
            }

            targetInstance = null;
            return false;
        }
        
        public bool TryGetRawInstance(Type targetType, out object targetInstance)
        {
            if (serviceTypeToInstances.TryGetValue(targetType, out targetInstance))
                return true;

            if (Application.isPlaying)
                return false;

            if (targetType.IsSubclassOf(typeof(Object)))
            {
#if UNITY_6000_0_OR_NEWER
                targetInstance = FindAnyObjectByType(targetType);
#else
                targetInstance = Object.FindObjectOfType(targetType);
#endif
                if (targetInstance != null)
                {
                    serviceTypeToInstances.Add(targetType, targetInstance);
                    return true;
                }
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{targetType} t:Prefab");
                if (guids.Length > 0)
                {
                    targetInstance = UnityEditor.AssetDatabase.LoadAssetAtPath(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]), targetType);
                    
                    if (targetInstance != null)
                    {
                        serviceTypeToInstances.Add(targetType, targetInstance);
                        return true;
                    }
                }
#endif               
                return false;
            }

            targetInstance = Activator.CreateInstance(targetType);
            if (targetInstance != null)
            {
                Debug.LogWarning($"[ServiceLocator] Auto-created instance of {targetType.Name} outside of play mode. " +
                    "This service was not explicitly registered.");
                serviceTypeToInstances.Add(targetType, targetInstance);
                return true;
            }

            return false;
        }

        public object GetRawInstance(Type targetType)
        {
            if (!TryGetRawInstance(targetType, out object targetInstance))
            {
                Debug.LogError(
                    $"The Service {targetType} is not yet registered on the ServiceLocator.");
                return null;
            }
            return targetInstance;
        }

        public void UnregisterAllServices()
        {
            List<object> activeInstances = new List<object>(serviceTypeToInstances.Count);
            foreach (KeyValuePair<Type, object> typeToInstance in serviceTypeToInstances)
                activeInstances.Add(typeToInstance.Value);

            for (int i = activeInstances.Count - 1; i >= 0; i--)
            {
                UnregisterInstance(activeInstances[i]);
            }

            serviceTypeToInstances.Clear();

            if (servicesWaitingOnDependenciesTobeResolved.Count > 0)
            {
                Debug.LogWarning($"{servicesWaitingOnDependenciesTobeResolved.Count} dependencies was waiting to be resolved");
                servicesWaitingOnDependenciesTobeResolved.Clear();
            }

            serviceTypeToObservables.Clear();
        }
        
        public void UnregisterInstance<T>()
        {
            Type type = typeof(T);
            UnregisterInstance(type);
        }

        public void UnregisterInstance<T>(T instance)
        {
            Type type = typeof(T);
            UnregisterInstance(type);
        }

        public void UnregisterInstance(Type targetType)
        {
            if (!serviceTypeToInstances.TryGetValue(targetType, out object serviceInstance)) 
                return;
            
            DispatchOnUnregisteredService(targetType, serviceInstance);
            serviceTypeToInstances.Remove(targetType);
        }

        private void DispatchOnUnregisteredService(Type targetType, object serviceInstance)
        {
            if (serviceInstance is IOnServiceUnregistered onServiceUnregistered)
            {
                onServiceUnregistered.OnUnregisteredFromServiceLocator(this);
            }

            DispatchToObservables(targetType, false);
        }

        private void DispatchToObservables(Type type, bool registered)
        {
            if (!serviceTypeToObservables.TryGetValue(type, out List<WeakReference<IServiceObservable>> observables))
                return;

            List<IServiceObservable> snapshot = ListPool<IServiceObservable>.Get();
            for (int i = observables.Count - 1; i >= 0; i--)
            {
                if (observables[i].TryGetTarget(out IServiceObservable target))
                    snapshot.Add(target);
                else
                    observables.RemoveAt(i);
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                if (registered)
                    snapshot[i].OnServiceRegistered(type);
                else
                    snapshot[i].OnServiceUnregistered(type);
            }

            ListPool<IServiceObservable>.Release(snapshot);
        }

        public void SubscribeToServiceChanges<T>(IServiceObservable observable)
        {
            Type type = typeof(T);
            if (!serviceTypeToObservables.TryGetValue(type, out List<WeakReference<IServiceObservable>> observables))
            {
                observables = new List<WeakReference<IServiceObservable>>();
                serviceTypeToObservables.Add(type, observables);
            }

            for (int i = observables.Count - 1; i >= 0; i--)
            {
                if (!observables[i].TryGetTarget(out IServiceObservable existing))
                    observables.RemoveAt(i);
                else if (ReferenceEquals(existing, observable))
                    return;
            }

            observables.Add(new WeakReference<IServiceObservable>(observable));
        }

        public void UnsubscribeToServiceChanges<T>(IServiceObservable observable)
        {
            Type type = typeof(T);
            if (!serviceTypeToObservables.TryGetValue(type, out List<WeakReference<IServiceObservable>> observables))
                return;

            for (int i = observables.Count - 1; i >= 0; i--)
            {
                if (!observables[i].TryGetTarget(out IServiceObservable existing))
                    observables.RemoveAt(i);
                else if (ReferenceEquals(existing, observable))
                    observables.RemoveAt(i);
            }
        }

        internal int ObservableCount<T>()
        {
            if (!serviceTypeToObservables.TryGetValue(typeof(T), out List<WeakReference<IServiceObservable>> observables))
                return 0;

            int alive = 0;
            for (int i = 0; i < observables.Count; i++)
            {
                if (observables[i].TryGetTarget(out _))
                    alive++;
            }

            return alive;
        }

        private void TryResolveDependencies()
        {
            bool anyNewServiceRegistered = false;
            Dictionary<Type, object> resolvedDependencies = new Dictionary<Type, object>();
            foreach (var typeToInstance in servicesWaitingOnDependenciesTobeResolved)
            {
                if (!IsServiceDependenciesResolved(typeToInstance.Key))
                    continue;

                resolvedDependencies.Add(typeToInstance.Key, typeToInstance.Value);
            }

            foreach (var resolvedTypeToObj in resolvedDependencies)
            {
                servicesWaitingOnDependenciesTobeResolved.Remove(resolvedTypeToObj.Key);
                RegisterInstanceInternal(resolvedTypeToObj.Key, resolvedTypeToObj.Value, false);
                anyNewServiceRegistered = true;
            }

            if (anyNewServiceRegistered)
                TryResolveDependencies();
        }

        private bool IsServiceDependenciesResolved(Type targetType)
        {
            object[] serviceAttributeObjects = targetType.GetCustomAttributes(
                typeof(ServiceImplementationAttribute), true);

            if (serviceAttributeObjects.Length == 0)
                return true;

            for (int i = 0; i < serviceAttributeObjects.Length; i++)
            {
                ServiceImplementationAttribute serviceAttribute =
                    (ServiceImplementationAttribute) serviceAttributeObjects[i];

                if (serviceAttribute.DependsOn == null || serviceAttribute.DependsOn.Length == 0)
                    continue;

                for (int j = 0; j < serviceAttribute.DependsOn.Length; j++)
                {
                    Type dependencyType = serviceAttribute.DependsOn[j];
                    if (!HasService(dependencyType))
                        return false;
                }
            }

            return true;
        }

        public void BeforeSceneUnload(Scene targetScene)
        {
            List<GameObject> roots = ListPool<GameObject>.Get();
            List<ServicesReporterBase> reporters = ListPool<ServicesReporterBase>.Get();

            targetScene.GetRootGameObjects(roots);
            foreach (GameObject root in roots)
            {
                root.GetComponentsInChildren(false, reporters);
                foreach (ServicesReporterBase reporter in reporters)
                    reporter.UnregisterServices();
            }

            ListPool<ServicesReporterBase>.Release(reporters);
            ListPool<GameObject>.Release(roots);
        }

#if UNITASK_ENABLED

        public async UniTask WaitForServiceAsync<T>(CancellationToken token = default) where T : class
        {
            await WaitForServiceAsync(typeof(T), token);
        }

        public async UniTask WaitForServiceAsync(Type targetType, CancellationToken token = default)
        {
            await UniTask.WaitUntil(() => HasService(targetType), cancellationToken: token);
        }
#endif

        private void OnApplicationQuit()
        {
            IsQuitting = true;
        }
    }
}

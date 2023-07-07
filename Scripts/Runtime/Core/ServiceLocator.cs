using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITASK_ENABLED
using System.Threading;
using Cysharp.Threading.Tasks;
#endif


namespace BrunoMikoski.ServicesLocation
{
    public class ServiceLocator 
    {
        private static ServiceLocator instance;
        public static ServiceLocator Instance
        {
            get
            {
                if (instance == null)
                    instance = new ServiceLocator();
                return instance;
            }
        }

        private Dictionary<Type, object> typeToInstances = new();

        private Dictionary<Type, List<IServiceObservable>> typeToObservables = new();

        private List<object> waitingOnDependenciesTobeResolved = new();
        
        private Dictionary<List<Type>, Action> servicesListToCallback = new();
        
        private HashSet<object> injectedObjects = new();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStaticReferences()
        {
            instance = null;
        }
        
        public void RegisterInstance<T>(T instance)
        {
            Type type = typeof(T);
            RegisterInstance(type, instance);
        }

        private void RegisterInstance(Type type, object instance)
        {
            if (!CanRegisterService(type, instance))
                return;

            if (!IsServiceDependenciesResolved(type))
            {
                if (!waitingOnDependenciesTobeResolved.Contains(instance))
                    waitingOnDependenciesTobeResolved.Add(instance);
                
                return;
            }
            
            typeToInstances.Add(type, instance);
            TryResolveDependencies();
            DispatchOnRegistered(type, instance);
        }

        private void DispatchOnRegistered(Type type, object instance)
        {
            if (instance is IOnServiceRegistered onRegistered)
            {
                onRegistered.OnRegisteredOnServiceLocator(this);
            }

            if (typeToObservables.TryGetValue(type, out List<IServiceObservable> observables))
            {
                for (int i = 0; i < observables.Count; i++)
                    observables[i].OnServiceRegistered(type);
            }
        }

        private bool CanRegisterService(Type type, object instance)
        {
            if (HasService(type))
            {
                Debug.LogError($"Service of type {type} is already registered.");
                return false;
            }

            if (instance is IConditionalService conditionalService)
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
            return typeToInstances.ContainsKey(type);
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
            if (typeToInstances.TryGetValue(targetType, out targetInstance))
                return true;

            if (Application.isPlaying)
                return false;

            if (targetType.IsSubclassOf(typeof(Object)))
            {
                targetInstance = Object.FindObjectOfType(targetType);
                if (targetInstance != null)
                {
                    typeToInstances.Add(targetType, targetInstance);
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
                        typeToInstances.Add(targetType, targetInstance);
                        return true;
                    }
                }
#endif               
                return false;
            }

            targetInstance = Activator.CreateInstance(targetType);
            if (targetInstance != null)
            {
                typeToInstances.Add(targetType, targetInstance);
                return true;
            }

            return false;
        }

        public object GetRawInstance(Type targetType)
        {
            if (!TryGetRawInstance(targetType, out object targetInstance))
            {
                Debug.LogError(
                    $"The Service {targetType} is not yet registered on the ServiceLocator, " +
                    $"consider using the Async Inject to wait for the service, or adding the Callback when Injecting");
                return null;
            }
            return targetInstance;
        }
        

        public void UnregisterAllServices()
        {
            List<object> activeInstances = new List<object>(typeToInstances.Count);
            foreach (KeyValuePair<Type, object> typeToInstance in typeToInstances)
                activeInstances.Add(typeToInstance.Value);

            for (int i = activeInstances.Count - 1; i >= 0; i--)
            {
                UnregisterInstance(activeInstances[i]);
            }
            
            typeToInstances.Clear();

            if (waitingOnDependenciesTobeResolved.Count > 0)
            {
                Debug.LogWarning($"{waitingOnDependenciesTobeResolved.Count} dependencies was waiting to be resolved");
                waitingOnDependenciesTobeResolved.Clear();
                typeToObservables.Clear();
            }
        }
        
        public void UnregisterInstance<T>()
        {
            Type type = typeof(T);
            UnregisterInstance(type);
        }
        
        public void UnregisterInstance<T>(T instance)
        {
            Type type = instance.GetType();
            UnregisterInstance(type);
        }

        public void UnregisterInstance(Type targetType)
        {
            if (!typeToInstances.TryGetValue(targetType, out object serviceInstance)) 
                return;
            
            DispatchOnUnregisteredService(targetType, serviceInstance);
            typeToInstances.Remove(targetType);
        }

        private void DispatchOnUnregisteredService(Type targetType, object serviceInstance)
        {
            if (serviceInstance is IOnServiceUnregistered onServiceUnregistered)
            {
                onServiceUnregistered.OnUnregisteredFromServiceLocator(this);
            }
            
            if (typeToObservables.TryGetValue(targetType, out List<IServiceObservable> observables))
            {
                for (int i = 0; i < observables.Count; i++)
                    observables[i].OnServiceUnregistered(targetType);
            }
        }

        public void SubscribeToServiceChanges<T>(IServiceObservable observable)
        {
            Type type = typeof(T);
            if (!typeToObservables.ContainsKey(type))
                typeToObservables.Add(type, new List<IServiceObservable>());

            if (!typeToObservables[type].Contains(observable))
                typeToObservables[type].Add(observable);
        }
        
        public void UnsubscribeToServiceChanges<T>(IServiceObservable observable)
        {
            Type type = typeof(T);
            if (!typeToObservables.TryGetValue(type, out List<IServiceObservable> observables))
                return;

            observables.Remove(observable);
        }

        private void TryResolveDependencies()
        {
            for (int i = waitingOnDependenciesTobeResolved.Count - 1; i >= 0; i--)
            {
                object waitingObject = waitingOnDependenciesTobeResolved[i];

                Type targetType = waitingObject.GetType();
                if (!IsServiceDependenciesResolved(targetType)) 
                    continue;
                
                waitingOnDependenciesTobeResolved.Remove(waitingObject);
                RegisterInstance(targetType, waitingObject);
            }

            List<Type>[] items = servicesListToCallback.Keys.ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                List<Type> item = items[i];
                if (!HasAllServices(item))
                    continue;

                servicesListToCallback[item].Invoke();
                servicesListToCallback.Remove(item);
            }
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

        public void Inject(object targetObject, Action callback = null)
        {
            if (injectedObjects.Contains(targetObject))
            {
                Debug.LogWarning($"Trying to inject {targetObject} that was already injected, skipping");
                return;
            }
            
            bool allResolved = DependenciesUtility.Inject(targetObject);
            if (allResolved)
            {
                injectedObjects.Add(targetObject);
                return;
            }
           
            if (callback == null)
            {
                throw new Exception(
                    $"{targetObject.GetType().Name} has unresolved dependencies and no callback was provided to handle it");
            }
               
            List<Type> unresolvedDependencies = DependenciesUtility.GetUnresolvedDependencies(targetObject);
               
            AddServicesRegisteredCallback(unresolvedDependencies, () =>
            {
                DependenciesUtility.Inject(targetObject);
                callback();
            });
        }

#if UNITASK_ENABLED

        public async UniTask InjectAsync(object script, CancellationToken token = default)
        {
            bool allResolved = DependenciesUtility.Inject(script);
            if (allResolved)
                return;
            
            List<Type> unresolvedDependencies = DependenciesUtility.GetUnresolvedDependencies(script);

            if (token == default)
            {
                if (script is MonoBehaviour unityObject)
                    token = unityObject.GetCancellationTokenOnDestroy();
            }

            List<UniTask> dependenciesTasks = new List<UniTask>();

            for (int i = 0; i < unresolvedDependencies.Count; i++)
            {
                Type unresolvedDependency = unresolvedDependencies[i];
                dependenciesTasks.Add(WaitForServiceAsync(unresolvedDependency, token));
            }

            await UniTask.WhenAll(dependenciesTasks);

            DependenciesUtility.Inject(script);
        }
        
        
        public async UniTask WaitForServiceAsync<T>(CancellationToken token = default) where T : class
        {
            await WaitForServiceAsync(typeof(T), token);
        }
        
        public async UniTask WaitForServiceAsync(Type targetType, CancellationToken token = default)
        {
            await UniTask.WaitUntil(() => HasService(targetType), cancellationToken: token);
        }
#endif
        private void AddServicesRegisteredCallback(List<Type> services, Action callback)
        {
            if (HasAllServices(services))
                callback?.Invoke();

            servicesListToCallback.Add(services, callback);
        }

        private bool HasAllServices(List<Type> services)
        {
            for (int i = 0; i < services.Count; i++)
            {
                if (!HasService(services[i]))
                    return false;
            }

            return true;
        }

    }
}

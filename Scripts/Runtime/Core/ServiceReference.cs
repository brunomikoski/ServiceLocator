using System;
using Object = System.Object;
#if UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.ServicesLocation
{
    public struct ServiceReference<T> : IServiceObservable where T : class
    {
        private T instance;
        public T Reference
        {
            get
            {
                if (IsNullOrDestroyed(instance))
                {
                    instance = ServiceLocator.Instance.GetInstance<T>();
                    if (instance != null)
                        ServiceLocator.Instance.SubscribeToServiceChanges<T>(this);
                }
                return instance;
            }
        }

        public bool Exists => ServiceLocator.Instance.HasService<T>();
        public T CachedReference => instance;

        private bool IsNullOrDestroyed(Object obj) {
 
            if (ReferenceEquals(obj, null)) 
                return true;
 
            if (obj is UnityEngine.Object o) 
                return o == null;
 
            return false;
        }

        public static implicit operator T(ServiceReference<T> serviceReference)
        {
            return serviceReference.Reference;
        }

        public void ClearCache()
        {
            instance = null;
        }

        void IServiceObservable.OnServiceRegistered(Type targetType)
        {
            T newServiceRegistered = ServiceLocator.Instance.GetInstance<T>();
            if (Equals(newServiceRegistered, instance))
                return;

            instance = newServiceRegistered;
        }

        void IServiceObservable.OnServiceUnregistered(Type targetType)
        {
            ClearCache();
        }

#if UNITASK_ENABLED
        public async UniTask WaitForServiceBeAvailableAsync()
        {
            await ServiceLocator.Instance.WaitForServiceAsync<T>();
        }
#endif
    }
}

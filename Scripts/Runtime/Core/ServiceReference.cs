using System;
using UnityEngine;
#if UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceReference<T> : IServiceObservable where T : class
    {
        private bool loadedOnce;
        private bool hasCachedReference;
        private T reference;
        public T Reference
        {
            get
            {
                if (!TryLoadReference()) 
                    return null;
                
                return reference;
            }
        }

        private bool TryLoadReference()
        {
            if (hasCachedReference) 
                return true;
            
            if (ServiceLocator.IsQuitting)
                return false;
            
            loadedOnce = true;

            hasCachedReference = ServiceLocator.Instance.TryGetInstance(out reference);
            if (hasCachedReference)
            {
                ServiceLocator.Instance.UnsubscribeToServiceChanges<T>(this);
                ServiceLocator.Instance.SubscribeToServiceChanges<T>(this);
            }

            return hasCachedReference;
        }

        /// <summary>
        /// This method will check if the service exist, and if its a Object if its not pending to be destroyed.
        /// Its expensive so don't use it on a Update loop
        /// </summary>
        public bool Exists
        {
            get
            {
                if (ServiceLocator.IsQuitting)
                    return false;
                
                if (ServiceLocator.Instance.HasService<T>())
                {
                    return HasValidCachedReference();
                }

                return false;
            }
        }

        private bool HasValidCachedReference()
        {
            if (!loadedOnce)
            {
                if (!TryLoadReference())
                    return false;
            }

            if (!hasCachedReference)
                return false;

            if (reference == null)
                return false;

            if (ReferenceEquals(reference, null))
                return false;

            if (reference is UnityEngine.Object unityObj)
            {
                if (unityObj == null)
                    return false;

                if (reference is null)
                    return false;
            }

            return true;
        }

        public static implicit operator T(ServiceReference<T> serviceReference)
        {
            return serviceReference.Reference;
        }

        public void ClearCache()
        {
            reference = null;
            hasCachedReference = false;
        }
        
        void IServiceObservable.OnServiceRegistered(Type targetType)
        {
            if (!ServiceLocator.Instance.TryGetInstance(out T newReference))
                return;
            
            if (Equals(newReference, reference))
                return;
        
            reference = newReference;
            hasCachedReference = true;
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

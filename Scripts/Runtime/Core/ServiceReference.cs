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

      public bool HasValidCachedReference()
        {
            if (!loadedOnce)
            {
                if (!TryLoadReference())
                {
                    Debug.Log("[ServiceReference] Failed to load reference.");
                    return false;
                }
            }
            
            if (!hasCachedReference)
            {
                Debug.Log("[ServiceReference] No cached reference.");
                return false;
            }
        
            if (reference == null)
            {
                Debug.Log("[ServiceReference] Reference is null.");
                return false;
            }
        
            if (ReferenceEquals(reference, null))
            {
                Debug.Log("[ServiceReference] ReferenceEquals check failed (reference is null).");
                return false;
            }
        
            if (reference is UnityEngine.Object unityObj)
            {
                if (unityObj == null)
                {
                    Debug.Log("[ServiceReference] UnityEngine.Object reference is null (Unity native object destroyed).");
                    return false;
                }
        
                if (reference is null)
                {
                    Debug.Log("[ServiceReference] Unity native object is not alive.");
                    return false;
                }
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

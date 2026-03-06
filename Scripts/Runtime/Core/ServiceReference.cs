using System;
using System.Collections;
using System.Collections.Generic;
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
        
        private List<Action> waitingForServiceToBeAvailableCallbacks = new();

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
        /// Check if service Exist independently of the cached reference.
        /// </summary>
        public bool Exists
        {
            get
            {
                if (ServiceLocator.IsQuitting)
                    return false;
                
                return ServiceLocator.Instance.HasService<T>();
            }
        }

        /// <summary>
        /// Check if the service exists and still have a valid cached reference.
        /// </summary>
        public bool HasCachedReference
        {
            get
            {
                if (ServiceLocator.IsQuitting)
                    return false;

                if (!ServiceLocator.Instance.HasService<T>())
                    return false;
                
                return HasValidCachedReference();
            }
        }

        private bool HasValidCachedReference()
        {
            if (!hasCachedReference)
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

            foreach (Action callback in waitingForServiceToBeAvailableCallbacks)
            {
                callback.Invoke();
            }
            
            waitingForServiceToBeAvailableCallbacks.Clear();
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
        public void WhenServiceGetsRegistered(Action callback)
        {
            if (Exists)
            {
                callback.Invoke();
                return;
            }

            if (waitingForServiceToBeAvailableCallbacks.Contains(callback))
                return;

            waitingForServiceToBeAvailableCallbacks.Add(callback);
        }
    }
}

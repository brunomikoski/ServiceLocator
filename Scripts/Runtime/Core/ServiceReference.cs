using System;
using UnityEngine;
#if UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceReference<T> : IServiceObservable where T : class
    {
        private int lastFrameCheckedForNativeAlive;

        private bool hasCachedReference;
        private T reference;
        public T Reference
        {
            get
            {
                if (!hasCachedReference)
                {
                    if (ServiceLocator.IsQuitting)
                        return null;

                    hasCachedReference = ServiceLocator.Instance.TryGetInstance(out reference);
                    if (hasCachedReference)
                    {
                        ServiceLocator.Instance.UnsubscribeToServiceChanges<T>(this);
                        ServiceLocator.Instance.SubscribeToServiceChanges<T>(this);
                    }
                }
                return reference;
            }
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

                if (hasCachedReference && ServiceLocator.Instance.HasService<T>())
                {
                    return reference != null && !reference.Equals(null);
                }
 
                return ServiceLocator.Instance.HasService<T>();
            }
        }

        private bool IsNullOrDestroyed(System.Object obj)
        {
            if (ReferenceEquals(obj, null)) 
                return true;
           
            if(obj is UnityEngine.Object unityObj)
            {
                if ((obj as UnityEngine.Object) == null) 
                    return true;

                if (lastFrameCheckedForNativeAlive != Time.frameCount)
                {
                    lastFrameCheckedForNativeAlive = Time.frameCount;
                    return !unityObj.IsNativeObjectAlive();
                }
            }

            return false;
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

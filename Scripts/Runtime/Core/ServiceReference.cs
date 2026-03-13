using System;
using System.Collections;
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
        
        private bool _subscribedToServiceChanges;

        private Action onWhenServiceGetsRegistered;
        public event Action OnWhenServiceGetsRegistered
        {
            add
            {
                if (Exists)
                {
                    value.Invoke();
                }
                SubscribeToServiceChanges();
                onWhenServiceGetsRegistered += value;
            }
            remove => onWhenServiceGetsRegistered -= value;
        }
        
        
        private Action onWhenServiceGetsUnregistered;
        public event Action OnWhenServiceGetsUnregistered
        {
            add
            {
                if (Exists)
                {
                    value.Invoke();
                }
                SubscribeToServiceChanges();
                onWhenServiceGetsUnregistered += value;
            }
            remove => onWhenServiceGetsUnregistered -= value;
        }

        ~ServiceReference()
        {
            _subscribedToServiceChanges = false;
            ServiceLocator.Instance.UnsubscribeToServiceChanges<T>(this);
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
                SubscribeToServiceChanges();
                onWhenServiceGetsRegistered?.Invoke();
            }

            return hasCachedReference;
        }

        private void SubscribeToServiceChanges()
        {
            if (_subscribedToServiceChanges)
                return;
            
            _subscribedToServiceChanges = true;
            ServiceLocator.Instance.SubscribeToServiceChanges<T>(this);
        }
        
        private void UnsubscribeFromServiceChanges()
        {
            if (!_subscribedToServiceChanges)
                return;
            
            _subscribedToServiceChanges = false;
            ServiceLocator.Instance.UnsubscribeToServiceChanges<T>(this);
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
            if (targetType != typeof(T))
                return;
            
            if (!ServiceLocator.Instance.TryGetInstance(out T newReference))
                return;

            if (!Equals(newReference, reference) && reference != null)
            {
                ClearCache();
                return;
            }

            hasCachedReference = false;
            TryLoadReference();
        }
        
        void IServiceObservable.OnServiceUnregistered(Type targetType)
        {
            if (targetType != typeof(T))
                return;
            
            ClearCache();
            onWhenServiceGetsUnregistered?.Invoke();
        }

#if UNITASK_ENABLED
        public async UniTask WaitForServiceBeAvailableAsync()
        {
            await ServiceLocator.Instance.WaitForServiceAsync<T>();
        }
#endif
        public IEnumerator WaitForServiceBeAvailableEnumerator()
        {
            while (!Exists)
            {
                yield return null;
            }
        }

        public void WhenServiceBecomeAvailable(Action callback)
        {
            ServiceLocator.Instance.StartCoroutine(WaitForServiceBeAvailableEnumeratorWithCallback(callback));
        }

        private IEnumerator WaitForServiceBeAvailableEnumeratorWithCallback(Action callback)
        {
            yield return WaitForServiceBeAvailableEnumerator();
            callback.Invoke();
        }
    }
}

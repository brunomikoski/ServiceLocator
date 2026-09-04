using System;
using System.Collections;
#if UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceReference<T> : IServiceObservable, IDisposable where T : class
    {
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

        private bool subscribedToServiceChanges;

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
                SubscribeToServiceChanges();
                onWhenServiceGetsUnregistered += value;
            }
            remove => onWhenServiceGetsUnregistered -= value;
        }

        private bool TryLoadReference()
        {
            if (hasCachedReference)
                return true;

            if (ServiceLocator.IsQuitting)
                return false;

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
            subscribedToServiceChanges = true;
            ServiceLocator.Instance.SubscribeToServiceChanges<T>(this);
        }

        private void UnsubscribeFromServiceChanges()
        {
            if (!subscribedToServiceChanges)
                return;

            subscribedToServiceChanges = false;

            if (ServiceLocator.IsQuitting)
                return;

            ServiceLocator.Instance.UnsubscribeToServiceChanges<T>(this);
        }


        /// <summary>
        /// Returns true if the service is currently registered in the locator, independently of
        /// this reference's cache.
        ///
        /// Has no side effects (no resolve, no subscribe) and works even if you never accessed
        /// <see cref="Reference"/> — so it's the right check for "is the service available right
        /// now?" without triggering a lazy load. Contrast with <see cref="HasCachedReference"/>,
        /// which additionally requires that this instance has already cached a still-valid reference.
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
        /// Returns true only if the service is registered AND this reference has already
        /// lazily cached it through a prior <see cref="Reference"/> access, and that cache is
        /// still valid (non-null, and not a destroyed UnityEngine.Object).
        ///
        /// Unlike <see cref="Reference"/>, this has no side effects — it does not resolve the
        /// service or subscribe to changes. So if you never accessed <see cref="Reference"/>
        /// beforehand, this returns false even when the service is available; the reference
        /// simply was never cached yet.
        ///
        /// Best used in teardown paths (OnDestroy / OnDisable) where you want to act only if the
        /// service was actually used, without forcing a resolve during shutdown.
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

        /// <summary>
        /// Eagerly releases this reference from the ServiceLocator: unsubscribes from service-change
        /// notifications, clears the cache, and drops all registered/unregistered delegates.
        ///
        /// Optional — the locator now holds observers weakly, so a reference is also reclaimed
        /// automatically once its owner is garbage collected. Call this from OnDestroy /
        /// OnUnregisteredFromServiceLocator when you want deterministic, immediate release. Safe to
        /// call multiple times.
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromServiceChanges();
            ClearCache();
            onWhenServiceGetsRegistered = null;
            onWhenServiceGetsUnregistered = null;
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

            onWhenServiceGetsUnregistered?.Invoke();
            ClearCache();
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

        public void WhenServiceBecomesAvailable(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            OnWhenServiceGetsRegistered += ServiceAvailable;
            return;

            void ServiceAvailable()
            {
                callback.Invoke();
                OnWhenServiceGetsRegistered -= ServiceAvailable;
            }
        }
    }
}

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BrunoMikoski.ServicesLocation.Tests
{
    public class ServiceReferenceLeakTests
    {
        // Plain POCO service so registration doesn't depend on any Unity object lifecycle.
        private class DummyService
        {
        }

        [TearDown]
        public void TearDown()
        {
            // Clears both serviceTypeToInstances and serviceTypeToObservables; the singleton itself
            // persists across the play session (DontDestroyOnLoad), which is fine.
            ServiceLocator.Instance.UnregisterAllServices();
        }

        [Test]
        public void Dispose_RemovesObserver_Immediately()
        {
            ServiceLocator.Instance.RegisterInstance(new DummyService());

            ServiceReference<DummyService> reference = new();
            _ = reference.Reference; // forces the lazy resolve, which subscribes to service changes

            Assert.AreEqual(1, ServiceLocator.Instance.ObservableCount<DummyService>(),
                "Reading Reference should have subscribed exactly one observer.");

            reference.Dispose();

            Assert.AreEqual(0, ServiceLocator.Instance.ObservableCount<DummyService>(),
                "Dispose() should have unsubscribed the observer immediately.");
        }

        [UnityTest]
        public IEnumerator AbandonedReferences_AreCollected_NotLeaked()
        {
            ServiceLocator.Instance.RegisterInstance(new DummyService());

            const int churn = 500;
            SubscribeThrowawayReferences(churn);

            // Nothing roots the references created above (the locator holds them only weakly now),
            // so a full collection must reclaim them.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            yield return null;

            // Drive a dispatch cycle: this both exercises the prune path and proves dispatch is
            // unfazed by dead observers.
            ServiceLocator.Instance.UnregisterInstance<DummyService>();
            ServiceLocator.Instance.RegisterInstance(new DummyService());

            int alive = ServiceLocator.Instance.ObservableCount<DummyService>();
            Assert.Less(alive, 50,
                $"Expected abandoned ServiceReferences to be garbage collected, but {alive} of {churn} " +
                "are still subscribed — the observable list is leaking.");
        }

        // Kept out-of-line so no stack slot in the test method roots the references; once this
        // returns, every reference it created is eligible for collection.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubscribeThrowawayReferences(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ServiceReference<DummyService> reference = new();
                _ = reference.Reference;
            }
        }
    }
}

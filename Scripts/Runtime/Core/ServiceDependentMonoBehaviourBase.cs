using System;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public abstract class ServiceDependentMonoBehaviourBase : MonoBehaviour, IDependsOnServices
    {
        public abstract Type[] DependsOnServices { get; }
        public abstract void OnServicesDependenciesResolved();

        protected virtual void Awake()
        {
            ServiceLocator.Instance.ResolveDependencies(this);
        }
    }
}

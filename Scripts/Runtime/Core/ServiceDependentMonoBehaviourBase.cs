using System;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public abstract class ServiceDependentMonoBehaviourBase : MonoBehaviour, IDependsOnServices
    {
        public abstract void OnServicesDependenciesResolved();

        protected virtual void Awake()
        {
            ServiceLocator.Instance.ResolveDependencies(this);
        }
    }
}

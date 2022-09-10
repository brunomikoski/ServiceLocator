using System;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public abstract class ServiceDependentMonoBehaviourBase : MonoBehaviour, IDependsOnService
    {
        public abstract void OnServicesDependenciesResolved();

        protected virtual void Awake()
        {
            ServiceLocator.Instance.ResolveDependencies(this);
        }
    }
}

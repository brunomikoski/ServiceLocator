using System;

namespace BrunoMikoski.ServicesLocation
{
    public abstract class ServiceDependentBase : IDependsOnExplicitServices
    {
        public abstract void OnServicesDependenciesResolved();
        public abstract Type[] DependsOnServices{ get; }
        
        protected ServiceDependentBase()
        {
            ServiceLocator.Instance.ResolveDependencies(this);
        }
    }
}

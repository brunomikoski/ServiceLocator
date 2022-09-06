using System;

namespace BrunoMikoski.ServicesLocation
{
    /// <summary>
    /// Useful for when you have a service that is only available based on certain condition,
    /// for instance only available on certain platforms
    /// </summary>
    public interface IConditionalService
    {
        bool CanBeRegistered(ServiceLocator serviceLocator);
    }
    
    public interface IOnServiceRegistered
    {
        void OnRegisteredOnServiceLocator(ServiceLocator serviceLocator);
    }

    public interface IOnServiceUnregistered
    {
        void OnUnregisteredFromServiceLocator(ServiceLocator serviceLocator);
    }

    public interface IServiceObservable
    {
        void OnServiceRegistered(Type targetType);
        void OnServiceUnregistered(Type targetType);
    }
    
    public interface IDependsOnServices
    {
        Type[] DependsOnServices { get; }
        void OnServicesDependenciesResolved();
    }
}

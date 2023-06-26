using System;

namespace BrunoMikoski.ServicesLocation
{
    public class MissingServiceException : ApplicationException 
    {
        public readonly Type ServiceType;

        public MissingServiceException(Type serviceType) 
        {
            ServiceType = serviceType;
        }
    }
}
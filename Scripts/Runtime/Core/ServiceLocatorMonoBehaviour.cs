using System;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceLocatorMonoBehaviour : MonoBehaviour
    {
        private void OnApplicationQuit()
        {
            ServiceLocator.SetIsQuitting();
        }
    }
}
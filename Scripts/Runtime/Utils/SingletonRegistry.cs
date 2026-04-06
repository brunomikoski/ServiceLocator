using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    internal static class SingletonRegistry
    {
        private static readonly List<Action> ResetCallbacks = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            for (int i = 0; i < ResetCallbacks.Count; i++)
                ResetCallbacks[i].Invoke();
        }

        internal static void RegisterResetCallback(Action callback)
        {
            ResetCallbacks.Add(callback);
        }
    }
}

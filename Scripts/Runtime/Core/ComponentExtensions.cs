using System;
#if UNITASK_ENABLED
using System.Threading;
using Cysharp.Threading.Tasks;
#endif
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public static partial class ComponentExtensions 
    {
        public static void Inject(this Component script, Action onDependenciesResolved = null)
        {
            ServiceLocator.Instance.Inject(script, onDependenciesResolved);
        }
        
#if UNITASK_ENABLED
        public static async UniTask InjectAsync(this Component script, CancellationToken token)
        {
            await ServiceLocator.Instance.InjectAsync(script, token);
        }
#endif
    }
}
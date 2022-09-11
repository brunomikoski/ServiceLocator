using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    [Serializable]
    public class DependencyCache
    {
        [SerializeField]
        private TypeToDependencies[] dependencyCache = new TypeToDependencies[0];

        public void Add(Type type, HashSet<Type> dependencies)
        {
            Array.Resize(ref dependencyCache, dependencyCache.Length + 1);
            dependencyCache[dependencyCache.Length - 1] = new TypeToDependencies(type, dependencies);
        }

        public void Parse()
        {
            foreach (TypeToDependencies typeToDependencies in dependencyCache)
            {
                typeToDependencies.Parse();
            }
        }

        public Type[] GetDependencies(Type targetType)
        {
            for (int i = 0; i < dependencyCache.Length; i++)
            {
                TypeToDependencies typeToDependencies = dependencyCache[i];
                if (typeToDependencies.Type != targetType)
                    continue;

                return typeToDependencies.Dependencies;
            }

            return null;
        }
    }
}

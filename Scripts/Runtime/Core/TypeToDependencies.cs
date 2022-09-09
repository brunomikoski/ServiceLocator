using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    [Serializable]
    public class TypeToDependencies
    {
        [SerializeField]
        private string typeFullName;
        [SerializeField]
        private string[] dependenciesNames;

        private Type type;
        private Type[] dependencies;

        public TypeToDependencies(Type type, HashSet<Type> dependencies)
        {
            typeFullName = $"{type.Assembly.FullName}:{type.FullName}";
            dependenciesNames = new string[dependencies.Count];
            int count = 0;
            foreach (Type dependency in dependencies)
            {
                dependenciesNames[count] = $"{dependency.Assembly.FullName}:{dependency.FullName}";
                count++;
            }
        }

        public void Parse()
        {
            string[] split = typeFullName.Split(":");

            type = Assembly.Load(split[0]).GetType(split[1]);

            dependencies = new Type[dependenciesNames.Length];
            for (int i = 0; i < dependenciesNames.Length; i++)
            {
                split = dependenciesNames[i].Split(":");

                dependencies[i] = Assembly.Load(split[0]).GetType(split[1]);
            }
        }
    }
}

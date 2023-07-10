using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class InjectedMemberData
    {
        public object OwnerObject;
        public MemberInfo MemberInfo;

        public InjectedMemberData(object targetOwner, MemberInfo memberInfo)
        {
            OwnerObject = targetOwner;
            MemberInfo = memberInfo;
        }
    }

    public static class DependenciesUtility
    {
        private const BindingFlags FLAGS = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;

        private static Dictionary<Type, List<Type>> typeToDependencyListCache = new();
        private static Dictionary<Type, Dictionary<MemberInfo, Type>> typeToFieldToTypeDependencyCache = new();
        private static Dictionary<Type, List<InjectedMemberData>> injectedObjects = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStaticReferences()
        {
            typeToDependencyListCache.Clear();
            typeToFieldToTypeDependencyCache.Clear();
            injectedObjects.Clear();
        }

        internal static bool Inject(object targetObject)
        {
            bool allResolved = UpdateDependencies(targetObject);
            if (targetObject is IOnInjected onInjected)
                onInjected.OnInjected();
            
            return allResolved;
        }

        private static List<Type> GetDependencies(object targetObject)
        {
            Type objectType = targetObject.GetType();

            if (typeToDependencyListCache.TryGetValue(objectType, out List<Type> dependencies))
                return dependencies;
            
            UpdateDependencies(targetObject, false);

            typeToDependencyListCache.TryGetValue(objectType, out dependencies);
                
            if (dependencies == null)
                dependencies = new List<Type>();
            
            return dependencies;
        }

        internal static List<Type> GetUnresolvedDependencies(object targetObject)
        {
            List<Type> dependencies = GetDependencies(targetObject);
            List<Type> unresolvedDependencies = new List<Type>();
            for (int i = 0; i < dependencies.Count; i++)
            {
                Type dependency = dependencies[i];

                if (ServiceLocator.Instance.HasService(dependency))
                    continue;
                
                unresolvedDependencies.Add(dependency);
            }

            return unresolvedDependencies;
        }

        private static bool UpdateDependencies(object targetObject, bool injectServices = true)
        {
            bool allDependenciesResolved = true;
            Type type = targetObject.GetType();
            if (typeToFieldToTypeDependencyCache.TryGetValue(type, out Dictionary<MemberInfo, Type> fieldToTypeDependencyCache))
            {
                foreach (var fieldToType in fieldToTypeDependencyCache)
                {
                    Type requiredType = fieldToType.Value;
                    if (!ServiceLocator.Instance.TryGetRawInstance(requiredType, out object service))
                    {
                        allDependenciesResolved = false;
                        continue;
                    }

                    if (injectServices)
                    {
                        MemberInfo memberInfo = fieldToType.Key;
                        
                        if (memberInfo is FieldInfo fieldInfo)
                            fieldInfo.SetValue(targetObject, service);
                        else if (memberInfo is PropertyInfo propertyInfo)
                            propertyInfo.SetValue(targetObject, service);


                        if (!injectedObjects.ContainsKey(requiredType))
                            injectedObjects.Add(requiredType, new List<InjectedMemberData>());
                        
                        injectedObjects[requiredType].Add(new InjectedMemberData(targetObject, memberInfo));
                    }
                }
                return allDependenciesResolved;
            }

            List<Type> dependencies = new List<Type>();
            fieldToTypeDependencyCache = new Dictionary<MemberInfo, Type>();
            FieldInfo[] fields = targetObject.GetType().GetFields(FLAGS);
            foreach (FieldInfo field in fields) 
            {
                InjectAttribute[] attrs = (InjectAttribute[])field.GetCustomAttributes(typeof(InjectAttribute), false);
                if (attrs.Length != 0) 
                {
                    Type serviceType = attrs[0].ServiceType ?? field.FieldType;
                    fieldToTypeDependencyCache.Add(field, serviceType);
                    dependencies.Add(serviceType);
                    
                    if (!ServiceLocator.Instance.TryGetRawInstance(serviceType, out object service))
                    {
                        allDependenciesResolved = false;
                        continue;
                    }

                    if (injectServices)
                    {
                        field.SetValue(targetObject, service);
                        
                        if (!injectedObjects.ContainsKey(serviceType))
                            injectedObjects.Add(serviceType, new List<InjectedMemberData>());
                        
                        injectedObjects[serviceType].Add(new InjectedMemberData(targetObject, field));
                    }
                }
            }
            
            PropertyInfo[] properties = targetObject.GetType().GetProperties(FLAGS);
            foreach (PropertyInfo prop in properties) 
            {
                InjectAttribute[] attrs = (InjectAttribute[])prop.GetCustomAttributes(typeof(InjectAttribute), false);
                if (attrs.Length != 0)
                {
                    Type serviceType = attrs[0].ServiceType ?? prop.PropertyType;
                    fieldToTypeDependencyCache.Add(prop, serviceType);
                    dependencies.Add(serviceType);
                    object service = ServiceLocator.Instance.GetRawInstance(serviceType);
                    if (service == null)
                    {
                        allDependenciesResolved = false;
                        continue;
                    }

                    if (injectServices)
                    {
                        prop.SetValue(targetObject, service);
                        
                        if (!injectedObjects.ContainsKey(serviceType))
                            injectedObjects.Add(serviceType, new List<InjectedMemberData>());
                        
                        injectedObjects[serviceType].Add(new InjectedMemberData(targetObject, prop));
                    }
                }
            }

            typeToFieldToTypeDependencyCache.Add(type, fieldToTypeDependencyCache);
            typeToDependencyListCache.Add(type, dependencies);
            return allDependenciesResolved;
        }

        public static void RefreshInjectedMembers(Type serviceType, object serviceInstance)
        {
            if (!injectedObjects.TryGetValue(serviceType, out List<InjectedMemberData> injectedMemberDatas))
                return;

            for (int i = injectedMemberDatas.Count - 1; i >= 0; i--)
            {
                InjectedMemberData injectedMemberData = injectedMemberDatas[i];
                MemberInfo memberInfo = injectedMemberData.MemberInfo;
                object targetObject = injectedMemberData.OwnerObject;

                if (targetObject == null)
                {
                    injectedMemberDatas.RemoveAt(i);
                    continue;
                }
                
                if (memberInfo is FieldInfo fieldInfo)
                    fieldInfo.SetValue(targetObject, serviceInstance);
                else if (memberInfo is PropertyInfo propertyInfo)
                    propertyInfo.SetValue(targetObject, serviceInstance);
                
                Debug.Log("Refreshed injected member " + memberInfo.Name + " of type " + serviceType.Name + " on object " + targetObject.GetType().Name);
            }
        }
    }
}
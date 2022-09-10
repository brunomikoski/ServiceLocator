using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public static class LazyServicesDependencyGenerator
    {
        private static Dictionary<string, Type> typeNameToType = new Dictionary<string, Type>();

        [MenuItem("Tools/ServiceLocator/Generate Lazy Link File")]
        public static void GenerateLinkFile()
        {
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ILazyServiceDependency>();

            DependencyCache dependencyCache = new DependencyCache();
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Instance | BindingFlags.Public |
                                                           BindingFlags.Static | BindingFlags.NonPublic);

                HashSet<Type> dependencies = new HashSet<Type>();

                for (int j = 0; j < memberInfos.Length; j++)
                {
                    MemberInfo info = memberInfos[j];
                    if (info.MemberType != MemberTypes.Field)
                        continue;

                    FieldInfo fieldInfo = ((FieldInfo)info);

                    if (!fieldInfo.FieldType.IsGenericType)
                        continue;

                    if (fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(ServiceReference<>))
                    {
                        Type fieldType = fieldInfo.FieldType.GetGenericArguments()[0];

                        dependencies.Add(fieldType);
                    }
                }


                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                       BindingFlags.Static | BindingFlags.NonPublic);

                for (int j = 0; j < methods.Length; j++)
                {
                    MethodInfo methodInfo = methods[j];
                    MethodBody methodBody = methodInfo.GetMethodBody();

                    if (methodBody == null)
                        continue;
                    
                    for (int k = 0; k < methodBody.LocalVariables.Count; k++)
                    {
                        LocalVariableInfo localVariableInfo = methodBody.LocalVariables[k];
                        if (localVariableInfo == null || localVariableInfo.LocalType == null || !localVariableInfo.LocalType.IsGenericType)
                            continue;
                            
                        if (localVariableInfo.LocalType.GetGenericTypeDefinition() == typeof(ServiceReference<>))
                        {
                            Type fieldType = localVariableInfo.LocalType.GetGenericArguments()[0];

                            dependencies.Add(fieldType);
                        }
                    }
                }

                string[] assetGUIDs = AssetDatabase.FindAssets($"t:TextAsset {type.Name}");
                if (assetGUIDs.Length == 1)
                {
                    TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(assetGUIDs[0]));

                    LookForDependencies(textAsset, ref dependencies,
                        $"{ServiceLocatorSettings.GetInstance().ServicesFileName}.",
                        new[] { '.', ';', ' ', '(' });
                    
                    DependencySearchPattern[] patterns = ServiceLocatorSettings.GetInstance().SearchPatterns;
                    for (int k = 0; k < patterns.Length; k++)
                    {
                        DependencySearchPattern searchPattern = patterns[k];
                        LookForDependencies(textAsset, ref dependencies, searchPattern.StartsWith, searchPattern.ClosingCharacters);
                    }
                }

                if (dependencies.Count > 0)
                {
                    dependencyCache.Add(type, dependencies);
                }
            }

            string json = JsonUtility.ToJson(dependencyCache);

            string assetPath = "Assets/Resources/ServiceLocatorAOTDependencies.json";
            File.WriteAllText(assetPath, json);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
       
        private static void LookForDependencies(TextAsset textAsset, ref HashSet<Type> dependencies,
            string targetString, char[] closingCharacters)
        {
            string textAssetText = textAsset.text;
            
            int lastFoundIndex = textAssetText.IndexOf(targetString, StringComparison.OrdinalIgnoreCase);
            while (lastFoundIndex > -1)
            {
                int endOfUsage =
                    textAssetText.IndexOfAny(closingCharacters, lastFoundIndex + targetString.Length);

                int startOfServiceName = lastFoundIndex + targetString.Length;
                string nameOfService = textAssetText.Substring(startOfServiceName, endOfUsage - startOfServiceName);

                if (TryGetServiceByName(nameOfService, out Type dependency))
                    dependencies.Add(dependency);

                int startIndex = startOfServiceName + endOfUsage - startOfServiceName;
                lastFoundIndex = textAssetText.IndexOf(targetString, startIndex, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool TryGetServiceByName(string nameOfService, out Type resultType)
        {
            if (typeNameToType.TryGetValue(nameOfService, out resultType))
                return resultType != null;
            
            IList<Type> types = AppDomain.CurrentDomain.GetAllTypes(AssembliesType.PlayerWithoutTestAssemblies);

            int matchCount = 0;
            for (int i = types.Count - 1; i >= 0; i--)
            {
                Type availableType = types[i];
                if (availableType.Name.IndexOf(nameOfService, StringComparison.Ordinal) > -1)
                {
                    matchCount++;
                    resultType = availableType;
                }
            }

            if (matchCount == 1)
            {
                typeNameToType.Add(nameOfService, resultType);
                return true;
            }

            typeNameToType.Add(nameOfService, null);
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public static class ServiceLocatorCodeGenerator
    {
        private const string AUTO_GENERATION_MENU_ITEM_NAME =
            "Tools/ServiceLocator/Auto Generate Services File On Code Compilation";

        private const string GENERATE_AOT_DEPENDENCIES =
            "Tools/ServiceLocator/Generate AOT Dependencies";

        private static bool AutoGenerateWhenScriptsReload
        {
            get => EditorPrefs.GetBool(nameof(AutoGenerateWhenScriptsReload), false);
            set => EditorPrefs.SetBool(nameof(AutoGenerateWhenScriptsReload), value);
        }

        private static Dictionary<string, List<ServiceImplementationAttribute>> categoryToAttributesList =
            new Dictionary<string, List<ServiceImplementationAttribute>>();

        private static Dictionary<string, Type> nameToTypeCache = new Dictionary<string, Type>();

        [MenuItem(AUTO_GENERATION_MENU_ITEM_NAME)]
        private static void AutoGenerateToggle()
        {
            AutoGenerateWhenScriptsReload = !AutoGenerateWhenScriptsReload;
        }

        [MenuItem(AUTO_GENERATION_MENU_ITEM_NAME, true)]
        private static bool AutoGenerateToggleValidation()
        {
            Menu.SetChecked(AUTO_GENERATION_MENU_ITEM_NAME, AutoGenerateWhenScriptsReload);
            return true;
        }

        [MenuItem(GENERATE_AOT_DEPENDENCIES)]
        public static void GenerateAOTDependencisFile()
        {
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<IDependsOnService>();

            DependencyCache dependencyCache = new DependencyCache();
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];

                if (typeof(IDependsOnExplicitServices).IsAssignableFrom(type))
                    continue;
                
                MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Instance | BindingFlags.Public |
                                                           BindingFlags.Static | BindingFlags.NonPublic);

                HashSet<Type> dependencies = new HashSet<Type>();

                for (int j = 0; j < memberInfos.Length; j++)
                {
                    MemberInfo info = memberInfos[j];
                    if (info.MemberType != MemberTypes.Field)
                        continue;

                    FieldInfo fieldInfo = ((FieldInfo) info);

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
                        if (localVariableInfo == null || localVariableInfo.LocalType == null ||
                            !localVariableInfo.LocalType.IsGenericType)
                            continue;

                        if (localVariableInfo.LocalType.GetGenericTypeDefinition() == typeof(ServiceReference<>))
                        {
                            Type fieldType = localVariableInfo.LocalType.GetGenericArguments()[0];

                            dependencies.Add(fieldType);
                        }
                    }
                }

                if (ServiceLocatorSettings.GetInstance().UseDeepDependencySearch)
                {
                    string[] assetGUIDs = AssetDatabase.FindAssets($"t:TextAsset {type.Name}");
                    if (assetGUIDs.Length == 1)
                    {
                        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(assetGUIDs[0]));

                        string[] regex = ServiceLocatorSettings.GetInstance().DeepDependencySearchRegex;
                        for (int j = 0; j < regex.Length; j++)
                        {
                            LookForMatches(textAsset, regex[j], ref dependencies);
                        }
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
        
        private static int CompareTypes(ServiceImplementationAttribute a, ServiceImplementationAttribute b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }

        private static void LookForMatches(TextAsset classAsset, string regexSearch, ref HashSet<Type> dependencies)
        {
            Regex regex = new Regex(regexSearch);
            MatchCollection matches = regex.Matches(classAsset.text);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                if (!match.Success)
                    continue;

                for (int j = 0; j < match.Groups.Count; j++)
                {
                    Group matchGroup = match.Groups[j];
                    if (TryGetServiceByName(matchGroup.Value, out Type dependency))
                        dependencies.Add(dependency);
                }
            }
        }

        /// <summary>
        /// Only try to generate the new Services file if there's a file available with some content on it, to avoid
        /// refreshing database code generation issues.
        /// </summary>
        [DidReloadScripts]
        private static void DidScripsReload()
        {
            if (Application.isPlaying || !Application.isEditor || Application.isBatchMode)
                return;

            if (!AutoGenerateWhenScriptsReload)
                return;

            GenerateServicesClass();
        }

        [MenuItem("Tools/ServiceLocator/Generate Services File")]
        internal static bool GenerateServicesClass()
        {
            categoryToAttributesList.Clear();
            ServiceLocatorSettings serviceLocatorSettings = ServiceLocatorSettings.GetInstance();
            string servicesClassName = serviceLocatorSettings.ServicesFileName;
            string referencesClassName = serviceLocatorSettings.ReferenceClassName;
            string targetScriptFolder = serviceLocatorSettings.GeneratedScriptsFolderPath;


            if (!Directory.Exists(targetScriptFolder))
                Directory.CreateDirectory(targetScriptFolder);

            string assetPath = Path.Combine(targetScriptFolder, servicesClassName + ".cs");
            TypeCache.TypeCollection allServicesTypes =
                TypeCache.GetTypesWithAttribute<ServiceImplementationAttribute>();

            if (allServicesTypes.Count == 0)
                return false;

            for (int i = 0; i < allServicesTypes.Count; i++)
            {
                Type servicesType = allServicesTypes[i];
                object[] customAttributes =
                    servicesType.GetCustomAttributes(typeof(ServiceImplementationAttribute), false);

                for (int j = 0; j < customAttributes.Length; j++)
                {
                    ServiceImplementationAttribute serviceImplementationAttribute =
                        (ServiceImplementationAttribute) customAttributes[j];
                    if (serviceImplementationAttribute.Type == null)
                        serviceImplementationAttribute.Type = servicesType;

                    string name = "";

                    if (string.IsNullOrEmpty(serviceImplementationAttribute.Name))
                        name = GetName(serviceImplementationAttribute);
                    else
                        name = serviceImplementationAttribute.Name;

                    serviceImplementationAttribute.Name = name;


                    string category = "";
                    if (!string.IsNullOrEmpty(serviceImplementationAttribute.Category))
                        category = serviceImplementationAttribute.Category;

                    if (!categoryToAttributesList.ContainsKey(category))
                        categoryToAttributesList.Add(category, new List<ServiceImplementationAttribute>());

                    categoryToAttributesList[category].Add(serviceImplementationAttribute);
                }
            }

            StringBuilder output = new StringBuilder();

            output.AppendLine("using BrunoMikoski.ServicesLocation;");
            output.AppendLine();
            output.AppendLine("namespace BrunoMikoski.ServicesLocation");
            output.AppendLine("{");
            output.AppendLine($"    public static class {servicesClassName}");
            output.AppendLine("    {");

            foreach (KeyValuePair<string, List<ServiceImplementationAttribute>> categoryToList in
                     categoryToAttributesList)
            {
                List<ServiceImplementationAttribute> implementations = categoryToList.Value;
                implementations.Sort(CompareTypes);

                if (string.IsNullOrEmpty(categoryToList.Key))
                {
                    output.AppendLine($"        public static class {referencesClassName}");
                    output.AppendLine("        {");
                    for (int i = 0; i < implementations.Count; i++)
                    {
                        ServiceImplementationAttribute implementation = implementations[i];
                        output.AppendLine(
                            $"            public static ServiceReference <{implementation.Type.FullName}> {implementation.Name};");
                    }

                    output.AppendLine("        }");
                    output.AppendLine();

                    for (int i = 0; i < categoryToList.Value.Count; i++)
                    {
                        ServiceImplementationAttribute implementation = implementations[i];
                        output.AppendLine(
                            $"        public static {implementation.Type.FullName} {implementation.Name} => {referencesClassName}.{implementation.Name}.Reference;");
                    }
                }
                else
                {
                    output.AppendLine($"        public static class {categoryToList.Key}");
                    output.AppendLine("        {");
                    output.AppendLine($"            public static class {referencesClassName}");
                    output.AppendLine("            {");
                    for (int i = 0; i < implementations.Count; i++)
                    {
                        ServiceImplementationAttribute implementation = implementations[i];
                        output.AppendLine(
                            $"                public static ServiceReference <{implementation.Type.FullName}> {implementation.Name};");
                    }

                    output.AppendLine("            }");
                    output.AppendLine();

                    for (int i = 0; i < categoryToList.Value.Count; i++)
                    {
                        ServiceImplementationAttribute implementation = implementations[i];
                        output.AppendLine(
                            $"            public static {implementation.Type.FullName} {implementation.Name} => {referencesClassName}.{implementation.Name}.Reference;");
                    }

                    output.AppendLine("        }");
                }
            }

            output.AppendLine("    }");
            output.AppendLine("}");
            output.AppendLine();

            string newFileContents = output.ToString();

            string currentFileContents = "";

            try
            {
                currentFileContents = File.ReadAllText(assetPath);
            }
            catch
            {
                // ignored
            }

            if (currentFileContents.Length != newFileContents.Length)
            {
                File.WriteAllText(assetPath, output.ToString());
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                return true;
            }

            return true;
        }

        internal static string GetName(ServiceImplementationAttribute serviceImplementationAttribute)
        {
            if (!string.IsNullOrEmpty(serviceImplementationAttribute.Name))
                return serviceImplementationAttribute.Name;

            string name = serviceImplementationAttribute.Type.Name;

            const string serviceStr = "Service";
            if (name.EndsWith(serviceStr))
            {
                name = name.Substring(0, name.Length - serviceStr.Length);
            }

            if (serviceImplementationAttribute.Type.IsInterface)
            {
                if (name.StartsWith("I"))
                {
                    if (char.IsUpper(name[1]))
                    {
                        name = name.Substring(1);
                    }
                }
            }

            return name.FirstToUpper();
        }

        private static bool TryGetServiceByName(string nameOfService, out Type resultType)
        {
            if (nameToTypeCache.TryGetValue(nameOfService, out resultType))
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
                nameToTypeCache.Add(nameOfService, resultType);
                return true;
            }

            nameToTypeCache.Add(nameOfService, null);
            return false;
        }
    }
}
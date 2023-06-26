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
        private const string GENERATE_AOT_DEPENDENCIES = "Tools/ServiceLocator/Generate AOT Dependencies";
        private const string GENERATE_STATIC_FILE = "Tools/ServiceLocator/Generate Services File";

        private static Dictionary<string, List<ServiceImplementationAttribute>> categoryToAttributesList = new();

        private static Dictionary<string, Type> nameToTypeCache = new();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStaticReferences()
        {
            categoryToAttributesList.Clear();
            nameToTypeCache.Clear();
        }

        private static int CompareTypes(ServiceImplementationAttribute a, ServiceImplementationAttribute b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Only try to generate the new Services file if there's a file available with some content on it, to avoid
        /// refreshing database code generation issues.
        /// </summary>
        [DidReloadScripts(1000)]
        private static void DidScripsReload()
        {
            if (Application.isPlaying || !Application.isEditor || Application.isBatchMode)
                return;

            if (ServiceLocatorSettings.GetInstance().GenerateStaticFileOnScriptReload)
            {
                GenerateServicesClass();
            }
        }

        [MenuItem(GENERATE_STATIC_FILE)]
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
    }
}
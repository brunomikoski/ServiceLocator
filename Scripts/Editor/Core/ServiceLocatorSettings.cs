using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceLocatorProjectSettingRegister
    {
        internal const string PROJECT_SERVICE_LOCATOR_PATH = "Project/ServiceLocator";
        private static DefaultAsset SCRIPTS_FOLDER;
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            SettingsProvider provider = new(PROJECT_SERVICE_LOCATOR_PATH, SettingsScope.Project)
            {
                label = "Service Locator",
                guiHandler = (searchContext) =>
                {
                    ServiceLocatorSettings settings = ServiceLocatorSettings.Instance;

                    if(SCRIPTS_FOLDER == null && !string.IsNullOrEmpty(settings.GeneratedScriptsFolderPath))
                        SCRIPTS_FOLDER = AssetDatabase.LoadAssetAtPath<DefaultAsset>(settings.GeneratedScriptsFolderPath);

                    using (EditorGUI.ChangeCheckScope changeCheck = new())
                    {
                        EditorGUILayout.BeginVertical("Box");
                        EditorGUILayout.LabelField("Static Access File", EditorStyles.boldLabel);
                        EditorGUILayout.Space();
                        EditorGUI.indentLevel++;

                        DefaultAsset newFolder = EditorGUILayout.ObjectField("Default Scripts Folder", SCRIPTS_FOLDER, typeof(DefaultAsset), false) as DefaultAsset;

                        
                        settings.ServicesFileName = EditorGUILayout.TextField("Services FileName", settings.ServicesFileName);
                        settings.ReferenceClassName = EditorGUILayout.TextField("Reference Class Name", settings.ReferenceClassName);
                        settings.GenerateStaticFileOnScriptReload = EditorGUILayout.Toggle("Auto Generate", settings.GenerateStaticFileOnScriptReload);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.LabelField("Services Names", EditorStyles.boldLabel);
                        EditorGUILayout.Space();
                        EditorGUI.indentLevel++;
                        settings.StripServiceFromNames = EditorGUILayout.Toggle("Strip 'Service'", settings.StripServiceFromNames);
                        settings.StripIFromNames = EditorGUILayout.Toggle("Strip 'I'", settings.StripIFromNames);
                        EditorGUI.indentLevel--;

                        if (changeCheck.changed)
                        {
                            settings.GeneratedScriptsFolderPath = AssetDatabase.GetAssetPath(newFolder);
                            ServiceLocatorServicesTable.Reload();
                            settings.Save();
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
            
                    if (SCRIPTS_FOLDER == null)
                    {
                        EditorGUILayout.HelpBox($"When no folder is specified, a new folder will be created at Assets/Generated/{settings.GeneratedScriptsFolderPath}",
                            MessageType.Info);
                    }
                    
                    ServiceLocatorServicesTable.DrawGeneratorWindow();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Editor", "Service", "Locator", "Service Locator" })
            };

            return provider;
        }
        
        
    }


    [Serializable]
    public class ServiceLocatorSettings
    {
        private const string STORAGE_PATH = "ProjectSettings/ServiceLocatorSettings.json";
        private const string DEFAULT_CODE_GENERATION_FOLDER_PATH = "Assets/Generated/Scripts";


        private static ServiceLocatorSettings instance;
        public static ServiceLocatorSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    if (File.Exists(STORAGE_PATH))
                    {
                        // Load settings from file.
                        string json = File.ReadAllText(STORAGE_PATH);
                        instance = JsonUtility.FromJson<ServiceLocatorSettings>(json);
                    }
                    else
                    {
                        // Create new settings instance if file doesn't exist.
                        instance = new ServiceLocatorSettings();
                    }
                }
                return instance;
            }
        }
        
        public static void Show()
        {
            SettingsService.OpenProjectSettings(ServiceLocatorProjectSettingRegister.PROJECT_SERVICE_LOCATOR_PATH);
        }
        
        [SerializeField]
        private string generatedScriptsFolderPath = $"{DEFAULT_CODE_GENERATION_FOLDER_PATH}";
        public string GeneratedScriptsFolderPath
        {
            get
            {
                if(string.IsNullOrEmpty(generatedScriptsFolderPath))
                    return DEFAULT_CODE_GENERATION_FOLDER_PATH;
                return generatedScriptsFolderPath;
            }
            set => generatedScriptsFolderPath = value;
        }

        [SerializeField]
        private string servicesFileName = "Services";
        public string ServicesFileName
        {
            get => servicesFileName;
            set => servicesFileName = value;
        }

        [SerializeField]
        private string referenceClassName = "Ref";
        public string ReferenceClassName
        {
            get => referenceClassName;
            set => referenceClassName = value;
        }

        [SerializeField] 
        private bool generateStaticFileOnScriptReload;


        public bool GenerateStaticFileOnScriptReload
        {
            get => generateStaticFileOnScriptReload;
            set => generateStaticFileOnScriptReload = value;
        }

        [SerializeField]
        private List<string> ignoredServicesWhenGenerating = new();

        
        [SerializeField] 
        private bool stripServiceFromNames = true;
        public bool StripServiceFromNames
        {
            get => stripServiceFromNames;
            set => stripServiceFromNames = value;
        }
        
        [SerializeField] 
        private bool stripIFromNames = true;
        public bool StripIFromNames
        {
            get => stripIFromNames;
            set => stripIFromNames = value;
        }
        

        public void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(STORAGE_PATH, json);
        }

        public bool IsServiceEnabled(ServiceImplementationAttribute serviceAttribute)
        {
            return !ignoredServicesWhenGenerating.Contains(serviceAttribute.Type.FullName);
        }

        public void SetServiceEnabled(ServiceImplementationAttribute serviceAttribute, bool isEnabled)
        {
            if (isEnabled)
            {
                if (ignoredServicesWhenGenerating.Remove(serviceAttribute.Type.FullName))
                    Save();
            }
            else
            {
                if (!ignoredServicesWhenGenerating.Contains(serviceAttribute.Type.FullName))
                {
                    ignoredServicesWhenGenerating.Add(serviceAttribute.Type.FullName);
                    Save();
                }
            }
        }
    }
}

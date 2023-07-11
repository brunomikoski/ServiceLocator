using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceLocatorProjectSettingRegister
    {
        private static DefaultAsset scriptsFolder;
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            SettingsProvider provider = new("Project/ServiceLocator", SettingsScope.Project)
            {
                label = "Service Locator",
                guiHandler = (searchContext) =>
                {
                    ServiceLocatorSettings settings = ServiceLocatorSettings.Instance;

                    if(scriptsFolder == null && !string.IsNullOrEmpty(settings.GeneratedScriptsFolderPath))
                        scriptsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(settings.GeneratedScriptsFolderPath);

                    using (EditorGUI.ChangeCheckScope changeCheck = new())
                    {
                        EditorGUILayout.BeginVertical("Box");
                        EditorGUILayout.LabelField("Static Access File", EditorStyles.boldLabel);
                        EditorGUILayout.Space();

                        DefaultAsset newFolder = EditorGUILayout.ObjectField("Default Scripts Folder", scriptsFolder, typeof(DefaultAsset), false) as DefaultAsset;

                        
                        settings.ServicesFileName = EditorGUILayout.TextField("Services FileName", settings.ServicesFileName);
                        settings.ReferenceClassName = EditorGUILayout.TextField("Reference Class Name", settings.ReferenceClassName);
                        settings.GenerateStaticFileOnScriptReload = EditorGUILayout.Toggle("Generate Static File On Script Reload", settings.GenerateStaticFileOnScriptReload);

                        if (changeCheck.changed)
                        {
                            settings.GeneratedScriptsFolderPath = AssetDatabase.GetAssetPath(newFolder);
                            settings.Save();
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
            
                    if (scriptsFolder == null)
                    {
                        EditorGUILayout.HelpBox($"When no folder is specified, a new folder will be created at Assets/Generated/{settings.GeneratedScriptsFolderPath}",
                            MessageType.Info);
                    }

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


        public void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(STORAGE_PATH, json);
        }
    }
}

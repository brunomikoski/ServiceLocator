using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceLocatorSettings : ScriptableObjectForPreferences<ServiceLocatorSettings>
    {
        private const string DEFAULT_CODE_GENERATION_FOLDER_PATH = "Assets/Generated/Scripts";
        private const string DEFAULT_SERVICES_NAME = "Services";

        [SerializeField]
        private string generatedScriptsFolderPath;
        public string GeneratedScriptsFolderPath
        {
            get
            {
                if(string.IsNullOrEmpty(generatedScriptsFolderPath))
                    return DEFAULT_CODE_GENERATION_FOLDER_PATH;
                return generatedScriptsFolderPath;
            }
        }

        [SerializeField]
        private string servicesFileName = "Services";
        public string ServicesFileName => servicesFileName;
        
        
        [SerializeField]
        private string referenceClassName = "Ref";
        public string ReferenceClassName => referenceClassName;

        [SerializeField] 
        private bool useDeepDependencySearch;
        public bool UseDeepDependencySearch => useDeepDependencySearch;

        [SerializeField] 
        private string[] deepDependencySearchRegex = new string[]
        {
            @"Services[\s]*\.[\s]*([\w+]+)",
            @"ServiceLocator[\s]*\.[\s]*Instance[\s]*\.[\s]*GetInstance<([\w+]+)"
        };
        public string[] DeepDependencySearchRegex => deepDependencySearchRegex;

        
        private static readonly GUIContent deepSearchGUIContent = new GUIContent(
            "Deep Search",
            "When using Deep Search, the service locator will try to find the Class TextAsset and parse it based " +
            "on the search regex to find within dependencies");
        
 
        [SettingsProvider]
        private static SettingsProvider SettingsProvider()
        {
            return CreateSettingsProvider("Service Locator", OnSettingsGUI);
        }

        private static void OnSettingsGUI(SerializedObject serializedObject)
        {
            SerializedProperty defaultGeneratedScriptsFolder = serializedObject.FindProperty(nameof(generatedScriptsFolderPath));
            DefaultAsset defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(defaultGeneratedScriptsFolder
                .stringValue);

            SerializedProperty servicesFileNameSerializedProperty = serializedObject.FindProperty(nameof(servicesFileName));

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DefaultAsset newFolder = EditorGUILayout.ObjectField("Default Scripts Folder", defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
                if (changeCheck.changed)
                {
                    defaultGeneratedScriptsFolder.stringValue = AssetDatabase.GetAssetPath(newFolder);
                    defaultGeneratedScriptsFolder.serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.PropertyField(servicesFileNameSerializedProperty);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(referenceClassName)));
                SerializedProperty deepSearchSP = serializedObject.FindProperty(nameof(useDeepDependencySearch));
                EditorGUILayout.PropertyField(deepSearchSP, deepSearchGUIContent);
                
                if (deepSearchSP.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(deepDependencySearchRegex)),deepSearchGUIContent);
    
                    if (!string.Equals(servicesFileNameSerializedProperty.stringValue, DEFAULT_SERVICES_NAME, StringComparison.Ordinal))
                    {
                        EditorGUILayout.HelpBox($"You might need to update the Deep Dependency Regex to match your new services filename",
                            MessageType.Warning);
                    }
                }

                if (changeCheck.changed)
                    serializedObject.ApplyModifiedProperties();
            }
            
            if (defaultAsset == null)
            {
                EditorGUILayout.HelpBox($"When no folder is specified, a new folder will be created at Assets/Generated/{servicesFileNameSerializedProperty.stringValue}",
                    MessageType.Info);
            }

            EditorGUILayout.Space();

        }

        private void Changed()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}

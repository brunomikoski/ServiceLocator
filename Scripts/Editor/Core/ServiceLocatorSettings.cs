using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceLocatorSettings : ScriptableObjectForPreferences<ServiceLocatorSettings>
    {
        private const string DEFAULT_CODE_GENERATION_FOLDER_PATH = "Assets/Generated/Scripts";

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
        }

        [SerializeField]
        private string servicesFileName = "Services";
        public string ServicesFileName => servicesFileName;
        
        [SerializeField]
        private string referenceClassName = "Ref";
        public string ReferenceClassName => referenceClassName;

        [SerializeField] 
        private bool generateStaticFileOnScriptReload;
        public bool GenerateStaticFileOnScriptReload => generateStaticFileOnScriptReload;

 
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

            using (EditorGUI.ChangeCheckScope changeCheck = new())
            {
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Static Access File", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                DefaultAsset newFolder = EditorGUILayout.ObjectField("Default Scripts Folder", defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
                if (changeCheck.changed)
                {
                    defaultGeneratedScriptsFolder.stringValue = AssetDatabase.GetAssetPath(newFolder);
                    defaultGeneratedScriptsFolder.serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.PropertyField(servicesFileNameSerializedProperty);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(referenceClassName)));


                EditorGUILayout.EndVertical();

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

    }
}

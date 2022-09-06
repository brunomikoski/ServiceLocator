using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    public class ServiceLocatorSettings : ScriptableObjectForPreferences<ServiceLocatorSettings>
    {
        [SerializeField]
        private string generatedScriptsFolderPath;
        public string GeneratedScriptsFolderPath => generatedScriptsFolderPath;

        [SerializeField]
        private string servicesFileName = "Services";
        public string ServicesFileName => servicesFileName;

        [SerializeField]
        private string referenceClassName = "Ref";
        public string ReferenceClassName => referenceClassName;

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

          

            using (EditorGUI.ChangeCheckScope changeCheck = new EditorGUI.ChangeCheckScope())
            {
                DefaultAsset newFolder = EditorGUILayout.ObjectField("Default Scripts Folder", defaultAsset, typeof(DefaultAsset), false) as DefaultAsset;
                if (changeCheck.changed)
                {
                    defaultGeneratedScriptsFolder.stringValue = AssetDatabase.GetAssetPath(newFolder);
                    defaultGeneratedScriptsFolder.serializedObject.ApplyModifiedProperties();
                }
            }

            SerializedProperty servicesFileNameSerializedProperty = serializedObject.FindProperty(nameof(servicesFileName));
            EditorGUILayout.PropertyField(servicesFileNameSerializedProperty);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(referenceClassName)));
            
            if (defaultAsset == null)
            {
                EditorGUILayout.HelpBox($"When no folder is specified, a new folder will be created at Assets/Generated/{servicesFileNameSerializedProperty.stringValue}",
                    MessageType.Info);
            }

            EditorGUILayout.Space();

        }

        public void OverrideCodeGenerationFolderPath(string targetScriptFolder)
        {
            generatedScriptsFolderPath = targetScriptFolder;
            Changed();
        }
        
        private void Changed()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}

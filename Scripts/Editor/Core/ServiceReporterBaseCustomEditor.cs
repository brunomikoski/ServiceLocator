using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    [CustomEditor(typeof(ServicesReporterBase), true)]
    public sealed class ServiceReporterBaseCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Services File Options", EditorStyles.toolbarButton))
            {
                ServiceLocatorSettings.Show();
            }

            if (GUILayout.Button("Generate Static Services File", EditorStyles.toolbarButton))
            {
                ServiceLocatorCodeGenerator.GenerateServicesClass();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

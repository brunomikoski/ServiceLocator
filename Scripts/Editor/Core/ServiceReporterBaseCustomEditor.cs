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
            if (GUILayout.Button("Options", EditorStyles.miniButtonLeft))
            {
                ServiceLocatorSettings.Show();
            }

            if (GUILayout.Button("Generate Static Services File", EditorStyles.miniButtonRight))
            {
                ServiceLocatorCodeGenerator.GenerateServicesClass();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

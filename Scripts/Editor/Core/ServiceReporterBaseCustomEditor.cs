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
            
            if (GUILayout.Button("Generate Static Services File"))
                ServiceLocatorCodeGenerator.GenerateServicesClass();
        }
    }
}

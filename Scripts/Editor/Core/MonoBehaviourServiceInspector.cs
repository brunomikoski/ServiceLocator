using System;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace BrunoMikoski.ServicesLocation
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoBehaviourServiceInspector : Editor
    {
        private bool displayHelper;

        private StringBuilder displayString = new StringBuilder("");

        private void OnEnable()
        {
            Type type = target.GetType();
            object[] customAttributes = type.GetCustomAttributes(typeof(ServiceImplementationAttribute), false);
            if (type.IsValueType || type.IsEnum || customAttributes.Length == 0)
                return;

            for (int i = 0; i < customAttributes.Length; i++)
            {
                ServiceImplementationAttribute serviceImplementationAttribute = (ServiceImplementationAttribute)customAttributes[i];
                if (serviceImplementationAttribute.Type == null)
                    serviceImplementationAttribute.Type = type;
                
                string displayName = "";
                    
                if (string.IsNullOrEmpty(serviceImplementationAttribute.Name))
                    displayName = ServicesCodeGenerator.GetName(serviceImplementationAttribute);
                else
                    displayName = serviceImplementationAttribute.Name;

                serviceImplementationAttribute.Name = displayName;
                
                string category = "";
                if (!string.IsNullOrEmpty(serviceImplementationAttribute.Category))
                    category = $"{serviceImplementationAttribute.Category}.";

                displayString.Append($"Accessible by Services.{category}{displayName}");
                displayHelper = true;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!displayHelper)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(displayString.ToString(), MessageType.Info);
        }
    }
}

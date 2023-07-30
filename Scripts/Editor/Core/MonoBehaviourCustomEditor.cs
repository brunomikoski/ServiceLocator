using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace BrunoMikoski.ServicesLocation
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoBehaviourCustomEditor : Editor
    {
        private static Dictionary<Type, string> typeToDisplayInfo = new();

        private string displayString;

        public void OnEnable()
        {
            Type type = target.GetType();

            if (typeToDisplayInfo.TryGetValue(type, out displayString)) 
                return;
            
            object[] customAttributes = type.GetCustomAttributes(typeof(ServiceImplementationAttribute), false);
            if (type.IsValueType || type.IsEnum || customAttributes.Length == 0)
                return;

            for (int i = 0; i < customAttributes.Length; i++)
            {
                ServiceImplementationAttribute serviceImplementationAttribute = (ServiceImplementationAttribute)customAttributes[i];
                
                ServiceLocatorCodeGenerator.UpdateServiceImplementationAttribute(serviceImplementationAttribute, type);
                
                string displayName = "";
                    
                if (string.IsNullOrEmpty(serviceImplementationAttribute.Name))
                    displayName = ServiceLocatorCodeGenerator.GetName(serviceImplementationAttribute);
                else
                    displayName = serviceImplementationAttribute.Name;
                
                displayString = $"Accessible by Services.{serviceImplementationAttribute.Category}{displayName}";
                typeToDisplayInfo.Add(type, displayString);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if(string.IsNullOrEmpty(displayString))
                return;

            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(displayString, MessageType.Info);
        }
    }
}

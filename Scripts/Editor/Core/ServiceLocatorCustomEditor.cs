using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.ServicesLocation
{
    [CustomEditor(typeof(ServiceLocator))]
    public sealed class ServiceLocatorCustomEditor : Editor
    {
        private static FieldInfo _serviceTypeToInstancesField;

        private void OnEnable()
        {
            if (_serviceTypeToInstancesField == null)
                _serviceTypeToInstancesField = typeof(ServiceLocator)
                    .GetField("serviceTypeToInstances", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public override void OnInspectorGUI()
        {
            DrawRegisteredServices();
        }

        private void DrawRegisteredServices()
        {
            EditorGUILayout.LabelField($"Registered Services", EditorStyles.boldLabel);

            if (_serviceTypeToInstancesField == null)
            {
                EditorGUILayout.HelpBox("Could not access ServiceLocator internal registry.", MessageType.Warning);
                return;
            }

            ServiceLocator locator = (ServiceLocator)target;
            Dictionary<Type, object> map = _serviceTypeToInstancesField.GetValue(locator) as Dictionary<Type, object>;

            if (map == null || map.Count == 0)
            {
                string msg = EditorApplication.isPlaying
                    ? "No services registered."
                    : "Enter Play Mode to see registered services.";
                EditorGUILayout.HelpBox(msg, MessageType.Info);
                return;
            }

            EditorGUI.indentLevel++;

            IOrderedEnumerable<KeyValuePair<Type, object>> iOrderedEnumerable = map.ToArray().OrderBy(kv => kv.Key.FullName);
            foreach (KeyValuePair<Type, object> kv in iOrderedEnumerable)
            {
                Type serviceType = kv.Key;
                object instance = kv.Value;

                EditorGUILayout.BeginHorizontal();

                string serviceTypeName = GetTypeDisplayName(serviceType);
                string instanceTypeName = instance != null ? GetTypeDisplayName(instance.GetType()) : "(null)";

                EditorGUILayout.LabelField(new GUIContent(serviceTypeName), new GUIContent(instanceTypeName));

                if (instance is UnityEngine.Object unityObject)
                {
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        EditorGUIUtility.PingObject(unityObject);
                        Selection.activeObject = unityObject;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        private static string GetTypeDisplayName(Type type)
        {
            if (type == null)
                return "(null)";

            if (type.IsGenericType)
            {
                string genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
                string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeDisplayName));
                return $"{genericTypeName}<{genericArgs}>";
            }

            return type.Name;
        }
    }
}

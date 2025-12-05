using System;
using UnityEditor;
using UnityEngine;

namespace AngryKoala.Data
{
    public abstract class SRDataOptionsEditor<TData> : Editor where TData : ScriptableObject
    {
        private SerializedProperty _scriptProperty;
        private SerializedProperty _srOptionsProperty;

        private void OnEnable()
        {
            _scriptProperty = serializedObject.FindProperty("m_Script");
            _srOptionsProperty = serializedObject.FindProperty("_srDebuggerOptions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_scriptProperty != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(_scriptProperty);
                EditorGUI.EndDisabledGroup();
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script" ||
                    iterator.propertyPath == "_srDebuggerOptions")
                {
                    continue;
                }

                DrawDataPropertyWithSRDebugger(iterator.Copy());
            }
            
            if (_srOptionsProperty != null && _srOptionsProperty.arraySize > 0)
            {
                if (GUILayout.Button("Reset SR Options Sort Orders"))
                {
                    SRDataOptionsUtility.ResetSortOrderForData((ScriptableObject)target);
                    serializedObject.Update();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDataPropertyWithSRDebugger(SerializedProperty property)
        {
            bool isSupported = IsSupportedType(property);

            if (!isSupported)
            {
                EditorGUILayout.PropertyField(property, includeChildren: true);
                return;
            }

            string propertyName = SRDataOptionsUtility.GetPropertyName(property.name);
            SerializedProperty entryProperty = FindOrCreateEntryProperty(propertyName);

            if (entryProperty == null)
            {
                EditorGUILayout.PropertyField(property, includeChildren: true);
                return;
            }

            SerializedProperty settingsProperty = entryProperty.FindPropertyRelative("_settings");
            if (settingsProperty == null)
            {
                EditorGUILayout.PropertyField(property, includeChildren: true);
                return;
            }

            SerializedProperty enabledProperty = settingsProperty.FindPropertyRelative("_enabled");
            SerializedProperty categoryProperty = settingsProperty.FindPropertyRelative("_category");
            SerializedProperty displayNameProperty = settingsProperty.FindPropertyRelative("_displayName");
            SerializedProperty sortProperty = settingsProperty.FindPropertyRelative("_sort");
            SerializedProperty numberRangeProperty = settingsProperty.FindPropertyRelative("_numberRange");
            SerializedProperty incrementProperty = settingsProperty.FindPropertyRelative("_increment");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(property, includeChildren: true);

                bool enabled = enabledProperty.boolValue;
                bool newEnabled = EditorGUILayout.ToggleLeft("Add to SROptions", enabled);

                if (newEnabled != enabled)
                {
                    enabledProperty.boolValue = newEnabled;
                }

                if (enabledProperty.boolValue)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(categoryProperty, new GUIContent("Category"));
                    EditorGUILayout.PropertyField(displayNameProperty, new GUIContent("Display Name"));
                    EditorGUILayout.PropertyField(sortProperty, new GUIContent("Sort"));

                    if (IsNumeric(property))
                    {
                        SerializedProperty rangeEnabledProperty =
                            numberRangeProperty.FindPropertyRelative("_enabled");
                        SerializedProperty minProperty =
                            numberRangeProperty.FindPropertyRelative("_min");
                        SerializedProperty maxProperty =
                            numberRangeProperty.FindPropertyRelative("_max");

                        bool rangeEnabled = rangeEnabledProperty.boolValue;
                        bool newRangeEnabled =
                            EditorGUILayout.ToggleLeft("Use Number Range", rangeEnabled);

                        if (newRangeEnabled != rangeEnabled)
                        {
                            rangeEnabledProperty.boolValue = newRangeEnabled;
                        }

                        if (rangeEnabledProperty.boolValue)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(minProperty, new GUIContent("Min"));
                            EditorGUILayout.PropertyField(maxProperty, new GUIContent("Max"));
                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.PropertyField(
                            incrementProperty,
                            new GUIContent("Increment"));
                    }

                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private SerializedProperty FindOrCreateEntryProperty(string propertyName)
        {
            if (_srOptionsProperty == null)
            {
                return null;
            }

            for (int i = 0; i < _srOptionsProperty.arraySize; i++)
            {
                SerializedProperty element = _srOptionsProperty.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = element.FindPropertyRelative("_propertyName");

                if (nameProperty != null &&
                    string.Equals(nameProperty.stringValue, propertyName, StringComparison.Ordinal))
                {
                    return element;
                }
            }

            int newIndex = _srOptionsProperty.arraySize;
            _srOptionsProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newElement = _srOptionsProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty newNameProperty = newElement.FindPropertyRelative("_propertyName");

            if (newNameProperty != null)
            {
                newNameProperty.stringValue = propertyName;
            }

            return newElement;
        }

        private bool IsSupportedType(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsNumeric(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Integer ||
                   property.propertyType == SerializedPropertyType.Float;
        }
    }
}
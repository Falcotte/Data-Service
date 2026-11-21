using UnityEditor;
using UnityEngine;

namespace AngryKoala.Data
{
    [CustomEditor(typeof(DataContainer), editorForChildClasses: true)]
    public sealed class DataContainerEditor : Editor
    {
#if SRDEBUGGER
        private SerializedProperty _srOptionsProperty;

        private GUIContent _categoryContent;
        private GUIContent _displayNameContent;
        private GUIContent _sortContent;
        private GUIContent _minContent;
        private GUIContent _maxContent;
        private GUIContent _incrementContent;
#endif

        private const string _scriptPropertyPath = "m_Script";
        private const string _srOptionsPropertyPath = "_srDataOptions";

        private void OnEnable()
        {
#if SRDEBUGGER
            if (EditorUtility.IsPersistent(target))
            {
                SRDataOptionsUtility.SyncOptions((DataContainer)target);
            }

            _srOptionsProperty = serializedObject.FindProperty(_srOptionsPropertyPath);
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == _scriptPropertyPath || iterator.propertyPath == _srOptionsPropertyPath)
                {
                    continue;
                }

                DrawDataProperty(iterator.Copy());
            }

#if SRDEBUGGER
            DrawResetSortOrderButton();
#endif

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            SerializedProperty scriptProperty = serializedObject.FindProperty(_scriptPropertyPath);

            if (scriptProperty == null)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(scriptProperty);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDataProperty(SerializedProperty property)
        {
#if SRDEBUGGER
            if (TryDrawWithOptions(property))
            {
                return;
            }
#endif

            EditorGUILayout.PropertyField(property, includeChildren: true);
        }

#if SRDEBUGGER
        private bool TryDrawWithOptions(SerializedProperty property)
        {
            if (_srOptionsProperty == null || !IsSupportedType(property))
            {
                return false;
            }

            SerializedProperty entryProperty =
                FindOrCreateEntryProperty(DataContainerFields.GetPropertyName(property.name));
            SerializedProperty settingsProperty = entryProperty?.FindPropertyRelative("_settings");

            if (settingsProperty == null)
            {
                return false;
            }

            SerializedProperty enabledProperty = settingsProperty.FindPropertyRelative("_enabled");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.PropertyField(property, includeChildren: true);

                enabledProperty.boolValue = EditorGUILayout.ToggleLeft("Add to SROptions", enabledProperty.boolValue);

                if (enabledProperty.boolValue)
                {
                    EditorGUI.indentLevel++;

                    _categoryContent ??= new GUIContent("Category");
                    _displayNameContent ??= new GUIContent("Display Name");
                    _sortContent ??= new GUIContent("Sort");

                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("_category"),
                        _categoryContent);
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("_displayName"),
                        _displayNameContent);
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("_sort"), _sortContent);

                    if (IsNumericType(property))
                    {
                        DrawNumberSettings(settingsProperty);
                    }

                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();

            return true;
        }

        private void DrawNumberSettings(SerializedProperty settingsProperty)
        {
            SerializedProperty numberRangeProperty = settingsProperty.FindPropertyRelative("_numberRange");
            SerializedProperty rangeEnabledProperty = numberRangeProperty.FindPropertyRelative("_enabled");

            rangeEnabledProperty.boolValue =
                EditorGUILayout.ToggleLeft("Use Number Range", rangeEnabledProperty.boolValue);

            if (rangeEnabledProperty.boolValue)
            {
                _minContent ??= new GUIContent("Min");
                _maxContent ??= new GUIContent("Max");

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(numberRangeProperty.FindPropertyRelative("_min"), _minContent);
                EditorGUILayout.PropertyField(numberRangeProperty.FindPropertyRelative("_max"), _maxContent);
                EditorGUI.indentLevel--;
            }

            _incrementContent ??= new GUIContent("Increment");

            EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("_increment"), _incrementContent);
        }

        private void DrawResetSortOrderButton()
        {
            if (_srOptionsProperty == null || _srOptionsProperty.arraySize == 0)
            {
                return;
            }

            if (!GUILayout.Button("Reset SR Options Sort Orders"))
            {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            SRDataOptionsUtility.ResetSortOrder((DataContainer)target);
            serializedObject.Update();
        }

        private SerializedProperty FindOrCreateEntryProperty(string propertyName)
        {
            for (int i = 0; i < _srOptionsProperty.arraySize; i++)
            {
                SerializedProperty element = _srOptionsProperty.GetArrayElementAtIndex(i);

                if (element.FindPropertyRelative("_propertyName").stringValue == propertyName)
                {
                    return element;
                }
            }

            int newIndex = _srOptionsProperty.arraySize;
            _srOptionsProperty.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newElement = _srOptionsProperty.GetArrayElementAtIndex(newIndex);
            ApplyEntryDefaults(newElement, propertyName, newIndex);

            return newElement;
        }

        private void ApplyEntryDefaults(SerializedProperty entryProperty, string propertyName, int sortOrder)
        {
            // A new array element is a copy of the last one, so every value is set.
            entryProperty.FindPropertyRelative("_propertyName").stringValue = propertyName;

            SerializedProperty settingsProperty = entryProperty.FindPropertyRelative("_settings");
            SerializedProperty numberRangeProperty = settingsProperty.FindPropertyRelative("_numberRange");

            settingsProperty.FindPropertyRelative("_enabled").boolValue = false;
            settingsProperty.FindPropertyRelative("_category").stringValue =
                SRDataOptionsUtility.ToDisplayName(target.GetType().Name);
            settingsProperty.FindPropertyRelative("_displayName").stringValue =
                SRDataOptionsUtility.ToDisplayName(propertyName);
            settingsProperty.FindPropertyRelative("_sort").intValue = sortOrder;
            settingsProperty.FindPropertyRelative("_increment").doubleValue = 1d;

            numberRangeProperty.FindPropertyRelative("_enabled").boolValue = false;
            numberRangeProperty.FindPropertyRelative("_min").doubleValue = 0d;
            numberRangeProperty.FindPropertyRelative("_max").doubleValue = 1d;
        }

        private static bool IsSupportedType(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Enum:
                {
                    return true;
                }

                default:
                {
                    return false;
                }
            }
        }

        private static bool IsNumericType(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Integer ||
                   property.propertyType == SerializedPropertyType.Float;
        }
#endif
    }
}

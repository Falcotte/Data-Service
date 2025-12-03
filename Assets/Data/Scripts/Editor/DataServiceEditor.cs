using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AngryKoala.Data
{
    [CustomEditor(typeof(DataService))]
    public class DataServiceEditor : Editor
    {
        private DataService _dataService;
        
        private SerializedProperty _scriptProperty;
        private SerializedProperty _autoRegisterProperty;
        private SerializedProperty _playerDataProperty;
        private SerializedProperty _initialPlayerDataProperty;
        private SerializedProperty _serializationFormatProperty;
        private SerializedProperty _useEncryptionProperty;
        private SerializedProperty _encryptionPasswordProperty;

        private void OnEnable()
        {
            SetSerializedProperties();
        }
        
        private void SetSerializedProperties()
        {
            _scriptProperty = serializedObject.FindProperty("m_Script");
            _autoRegisterProperty = serializedObject.FindProperty("_autoRegister");
            _playerDataProperty = serializedObject.FindProperty("_playerData");
            _initialPlayerDataProperty = serializedObject.FindProperty("_initialPlayerData");
            _serializationFormatProperty = serializedObject.FindProperty("_serializationFormat");
            _useEncryptionProperty = serializedObject.FindProperty("_useEncryption");
            _encryptionPasswordProperty = serializedObject.FindProperty("_encryptionPassword");
        }

        public override void OnInspectorGUI()
        {
            if (_dataService == null)
            {
                _dataService = (DataService)target;
            }
            
            serializedObject.Update();
            
            if (_scriptProperty != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(_scriptProperty);
                EditorGUI.EndDisabledGroup();
            }
            
            EditorGUILayout.PropertyField(_autoRegisterProperty);
            EditorGUILayout.PropertyField(_playerDataProperty);
            EditorGUILayout.PropertyField(_initialPlayerDataProperty);
            EditorGUILayout.PropertyField(_serializationFormatProperty);
            EditorGUILayout.PropertyField(_useEncryptionProperty);
            
            if (_useEncryptionProperty is { boolValue: true })
            {
                EditorGUILayout.PropertyField(_encryptionPasswordProperty);
            }
            
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                if (GUILayout.Button("Load Player Data"))
                {
                    _dataService.LoadPlayerData();
                    EditorUtility.SetDirty(_dataService.PlayerData);
                }

                if (GUILayout.Button("Save Player Data"))
                {
                    _dataService.SavePlayerData();
                }

                if (GUILayout.Button("Reset Player Data"))
                {
                    if (EditorUtility.DisplayDialog(
                            "Reset To Initial Defaults",
                            "This will overwrite PlayerData with the InitialPlayerData values.\n\nAre you sure?",
                            "Yes", "No"))
                    {
                        _dataService.ResetPlayerData();

                        EditorUtility.SetDirty(_dataService.PlayerData);
                    }
                }
            }

            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Open Data Folder"))
            {
                string folderPath = !string.IsNullOrEmpty(GetDataPath())
                    ? GetDataPath()
                    : Application.persistentDataPath;

                OpenSaveFolder(folderPath);
            }
        }

        private string GetDataPath()
        {
            if (_dataService == null)
            {
                return null;
            }

            const BindingFlags bindingFlags =
                BindingFlags.Instance | BindingFlags.NonPublic;

            PropertyInfo propertyInfo =
                typeof(DataService).GetProperty("_dataPath", bindingFlags);

            if (propertyInfo == null || propertyInfo.PropertyType != typeof(string))
            {
                Debug.LogWarning(
                    "Could not find private string property _dataPath. " +
                    "Falling back to Application.persistentDataPath.");

                return null;
            }

            object value = propertyInfo.GetValue(_dataService);

            if (value is string path && string.IsNullOrWhiteSpace(path) == false)
            {
                return path;
            }

            Debug.LogWarning(
                "_dataPath returned null or empty. " +
                "Falling back to Application.persistentDataPath.");

            return null;
        }

        private void OpenSaveFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            folderPath = folderPath.Replace("/", "\\");

#if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", folderPath);
#elif UNITY_EDITOR_OSX
            Process.Start("open", $"\"{folderPath}\"");
#else
            Process.Start("xdg-open", folderPath);
#endif
        }
    }
}
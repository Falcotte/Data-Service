using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AngryKoala.Data
{
    public static class SRDataOptionsUtility
    {
        private static readonly HashSet<Type> _supportedTypes = new()
        {
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(byte),
            typeof(sbyte),
            typeof(float),
            typeof(double),
            typeof(string)
        };

        public static void SyncOptionsForDataObject(
            ScriptableObject data,
            List<SRDebuggerOptionEntry> entries)
        {
            if (data == null || entries == null)
            {
                return;
            }

            string dataTypeName = data.GetType().Name;
            HashSet<string> validPropertyNames = new HashSet<string>(StringComparer.Ordinal);

            FieldInfo[] fields = data.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            int orderIndex = 0;

            foreach (FieldInfo field in fields)
            {
                if (!field.IsPrivate)
                {
                    continue;
                }

                if (field.GetCustomAttribute<SerializeField>() == null)
                {
                    continue;
                }

                if (field.GetCustomAttribute<HideInInspector>() != null)
                {
                    continue;
                }

                Type fieldType = field.FieldType;
                bool isSupported =
                    _supportedTypes.Contains(fieldType) ||
                    fieldType.IsEnum;

                if (!isSupported)
                {
                    continue;
                }

                string propertyName = GetPropertyName(field.Name);
                validPropertyNames.Add(propertyName);

                SRDebuggerOptionEntry entry = entries.Find(e =>
                    string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal));

                bool isNewEntry = false;

                if (entry == null)
                {
                    entry = new SRDebuggerOptionEntry
                    {
                        PropertyName = propertyName
                    };

                    entries.Add(entry);
                    isNewEntry = true;
                    EditorUtility.SetDirty(data);
                }

                EnsureEntryDefaults(entry, dataTypeName, propertyName, orderIndex, isNewEntry);

                orderIndex++;
            }

            entries.RemoveAll(e => !validPropertyNames.Contains(e.PropertyName));
        }

        public static string GetPropertyName(string fieldName)
        {
            string cleanedFieldName = fieldName.TrimStart('_');

            if (string.IsNullOrEmpty(cleanedFieldName))
            {
                return fieldName;
            }

            if (cleanedFieldName.Length == 1)
            {
                return cleanedFieldName.ToUpperInvariant();
            }

            return char.ToUpperInvariant(cleanedFieldName[0]) + cleanedFieldName.Substring(1);
        }

        public static string ToDisplayName(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(identifier.Length * 2);

            for (int i = 0; i < identifier.Length; i++)
            {
                char current = identifier[i];

                if (i == 0)
                {
                    builder.Append(char.ToUpperInvariant(current));
                    continue;
                }

                char previous = identifier[i - 1];

                bool isCurrentUpper = char.IsUpper(current);
                bool isPreviousUpper = char.IsUpper(previous);

                if (isCurrentUpper && !isPreviousUpper)
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static void EnsureEntryDefaults(
            SRDebuggerOptionEntry entry,
            string dataTypeName,
            string propertyName,
            int orderIndex,
            bool isNewEntry)
        {
            SRDebuggerOptionSettings settings = entry.Settings;

            if (settings == null)
            {
                settings = new SRDebuggerOptionSettings();
                entry.Settings = settings;
            }

            if (string.IsNullOrEmpty(settings.Category) ||
                string.Equals(settings.Category, "Data", StringComparison.Ordinal))
            {
                settings.Category = ToDisplayName(dataTypeName);
            }

            if (string.IsNullOrEmpty(settings.DisplayName))
            {
                settings.DisplayName = ToDisplayName(propertyName);
            }

            // Default sort only when new/zero so manual tweaks are respected
            if (isNewEntry || settings.Sort == 0)
            {
                settings.Sort = orderIndex;
            }
        }

        public static void ResetSortOrderForData(ScriptableObject data)
        {
            if (data == null)
            {
                return;
            }

            FieldInfo optionsField = data.GetType().GetField(
                "_srDebuggerOptions",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (optionsField == null)
            {
                return;
            }

            if (optionsField.GetValue(data) is not List<SRDebuggerOptionEntry> entries ||
                entries == null)
            {
                return;
            }

            FieldInfo[] fields = data.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            int orderIndex = 0;

            foreach (FieldInfo field in fields)
            {
                if (!field.IsPrivate)
                {
                    continue;
                }

                if (field.GetCustomAttribute<SerializeField>() == null)
                {
                    continue;
                }

                if (field.GetCustomAttribute<HideInInspector>() != null)
                {
                    continue;
                }

                Type fieldType = field.FieldType;
                bool isSupported =
                    _supportedTypes.Contains(fieldType) ||
                    fieldType.IsEnum;

                if (!isSupported)
                {
                    continue;
                }

                string propertyName = GetPropertyName(field.Name);

                SRDebuggerOptionEntry entry = entries.Find(e =>
                    string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal));

                if (entry != null && entry.Settings != null)
                {
                    entry.Settings.Sort = orderIndex;
                }

                orderIndex++;
            }

            EditorUtility.SetDirty(data);
        }
    }
}
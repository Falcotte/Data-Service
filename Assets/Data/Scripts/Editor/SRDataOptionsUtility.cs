using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace AngryKoala.Data
{
    /// <summary>
    /// Keeps a container asset's debug option settings in step with the fields of its class.
    /// </summary>
    public static class SRDataOptionsUtility
    {
        private const string _defaultCategory = "Data";

        /// <summary>
        /// Adds an entry for every supported field that has none, removes entries of fields that are gone, and
        /// marks the asset changed when something was added or removed.
        /// </summary>
        public static void SyncOptions(DataContainer container)
        {
            if (container == null)
            {
                return;
            }

            List<SRDataOptionEntry> entries = container.SRDataOptionList;
            string containerTypeName = container.GetType().Name;

            var fields = new List<FieldInfo>();
            DataContainerFields.GetOptionFields(container.GetType(), fields);

            var validPropertyNames = new HashSet<string>(StringComparer.Ordinal);
            bool hasChanged = false;

            for (int i = 0; i < fields.Count; i++)
            {
                string propertyName = DataContainerFields.GetPropertyName(fields[i].Name);
                validPropertyNames.Add(propertyName);

                SRDataOptionEntry entry = FindEntry(entries, propertyName);
                bool isNewEntry = entry == null;

                if (isNewEntry)
                {
                    entry = new SRDataOptionEntry
                    {
                        PropertyName = propertyName
                    };

                    entries.Add(entry);
                    hasChanged = true;
                }

                hasChanged |= EnsureEntryDefaults(entry, containerTypeName, propertyName, sortOrder: i, isNewEntry);
            }

            int removedCount = entries.RemoveAll(entry => !validPropertyNames.Contains(entry.PropertyName));

            if (hasChanged || removedCount > 0)
            {
                EditorUtility.SetDirty(container);
            }
        }

        /// <summary>
        /// Puts every entry's sort value back to the position of its field in the class.
        /// </summary>
        public static void ResetSortOrder(DataContainer container)
        {
            if (container == null)
            {
                return;
            }

            List<SRDataOptionEntry> entries = container.SRDataOptionList;

            var fields = new List<FieldInfo>();
            DataContainerFields.GetOptionFields(container.GetType(), fields);

            for (int i = 0; i < fields.Count; i++)
            {
                SRDataOptionEntry entry = FindEntry(entries, DataContainerFields.GetPropertyName(fields[i].Name));

                if (entry != null && entry.Settings != null)
                {
                    entry.Settings.Sort = i;
                }
            }

            EditorUtility.SetDirty(container);
        }

        public static string ToDisplayName(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(identifier.Length * 2);

            for (int i = 0; i < identifier.Length; i++)
            {
                char current = identifier[i];

                if (i == 0)
                {
                    builder.Append(char.ToUpperInvariant(current));
                    continue;
                }

                if (char.IsUpper(current) && !char.IsUpper(identifier[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private static SRDataOptionEntry FindEntry(List<SRDataOptionEntry> entries, string propertyName)
        {
            foreach (SRDataOptionEntry entry in entries)
            {
                if (entry != null && string.Equals(entry.PropertyName, propertyName, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool EnsureEntryDefaults(SRDataOptionEntry entry, string containerTypeName,
            string propertyName, int sortOrder, bool isNewEntry)
        {
            bool hasChanged = false;

            if (entry.Settings == null)
            {
                entry.Settings = new SRDataOptionSettings();
                hasChanged = true;
            }

            SRDataOptionSettings settings = entry.Settings;

            if (string.IsNullOrEmpty(settings.Category) ||
                string.Equals(settings.Category, _defaultCategory, StringComparison.Ordinal))
            {
                settings.Category = ToDisplayName(containerTypeName);
                hasChanged = true;
            }

            if (string.IsNullOrEmpty(settings.DisplayName))
            {
                settings.DisplayName = ToDisplayName(propertyName);
                hasChanged = true;
            }

            if (isNewEntry)
            {
                settings.Sort = sortOrder;
            }

            return hasChanged;
        }
    }
}

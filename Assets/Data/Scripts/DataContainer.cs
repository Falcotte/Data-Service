using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data
{
    /// <summary>
    /// Base class of a game's data classes. The asset holds the defaults; the data service hands out a runtime copy.
    /// </summary>
    public abstract class DataContainer : ScriptableObject
    {
        [SerializeField] [HideInInspector] private List<SRDataOptionEntry> _srDataOptions = new();
        public IReadOnlyList<SRDataOptionEntry> SRDataOptions => _srDataOptions;

        internal List<SRDataOptionEntry> SRDataOptionList => _srDataOptions;

        internal int CurrentVersion => Version;

        private const string _emptyOptionsJson = "\"_srDataOptions\":[]";

        /// <summary>
        /// Raise it when a field is renamed, removed or changes meaning, and handle older saves in Migrate.
        /// </summary>
        protected virtual int Version => 1;

        /// <summary>
        /// Called after a save written by an older version was loaded. The raw JSON is passed because the value
        /// of a renamed or removed field is not loaded into the container.
        /// </summary>
        protected virtual void Migrate(int savedVersion, string savedJson)
        {
        }

        internal void MigrateInternal(int savedVersion, string savedJson) => Migrate(savedVersion, savedJson);

        // The container's values as JSON, without the debug option settings.
        internal string ToSaveJson()
        {
            List<SRDataOptionEntry> options = _srDataOptions;
            _srDataOptions = new List<SRDataOptionEntry>();

            string json;

            try
            {
                json = JsonUtility.ToJson(this, prettyPrint: false);
            }
            finally
            {
                _srDataOptions = options;
            }

            return RemoveEmptyOptions(json);
        }

        internal void CopyValuesFrom(DataContainer source)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source, prettyPrint: false), this);
        }

        private static string RemoveEmptyOptions(string json)
        {
            int index = json.IndexOf(_emptyOptionsJson, StringComparison.Ordinal);

            if (index < 0)
            {
                return json;
            }

            int length = _emptyOptionsJson.Length;
            int end = index + length;

            if (end < json.Length && json[end] == ',')
            {
                length++;
            }
            else if (index > 0 && json[index - 1] == ',')
            {
                index--;
                length++;
            }

            return json.Remove(index, length);
        }
    }
}

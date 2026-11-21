using System;
using UnityEngine;

namespace AngryKoala.Data
{
    // What a data file holds: one header line, then the container's JSON.
    internal sealed class DataFileContent
    {
        public int Version { get; }

        public string DefaultsChecksum { get; }

        public string Json { get; }

        private const char _separator = '\n';

        public DataFileContent(int version, string defaultsChecksum, string json)
        {
            Version = version;
            DefaultsChecksum = defaultsChecksum ?? string.Empty;
            Json = json ?? string.Empty;
        }

        public string ToText()
        {
            var header = new Header
            {
                Version = Version,
                DefaultsChecksum = DefaultsChecksum
            };

            return JsonUtility.ToJson(header, prettyPrint: false) + _separator + Json;
        }

        public static bool TryParse(string text, out DataFileContent content)
        {
            content = null;

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int separatorIndex = text.IndexOf(_separator);

            if (separatorIndex <= 0 || separatorIndex == text.Length - 1)
            {
                return false;
            }

            Header header;

            try
            {
                header = JsonUtility.FromJson<Header>(text.Substring(0, separatorIndex));
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (header == null)
            {
                return false;
            }

            content = new DataFileContent(header.Version, header.DefaultsChecksum, text.Substring(separatorIndex + 1));

            return true;
        }

        [Serializable]
        private sealed class Header
        {
            public int Version;
            public string DefaultsChecksum;
        }
    }
}

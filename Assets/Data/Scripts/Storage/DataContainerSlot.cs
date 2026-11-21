using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AngryKoala.Data
{
    // One container of the data service: the asset with the defaults, the copy that holds the current values,
    // and what was last read from or written to its file.
    internal sealed class DataContainerSlot
    {
        public DataContainer Source { get; }

        public DataContainer Container { get; }

        public DataPersistence Persistence { get; }

        public string FileName { get; }

        public bool Obfuscate { get; }

        public bool HasFile { get; private set; }

        private string _lastJson;
        private int _loadedVersion;
        private bool _wasMigrated;
        private bool _hasUnreadableFile;

        private static readonly DataFileKind[] _fileKinds = { DataFileKind.Current, DataFileKind.Backup };

        private DataContainerSlot(DataContainer source, DataContainer container, DataFileAttribute attribute)
        {
            Source = source;
            Container = container;
            Persistence = attribute.Persistence;
            FileName = string.IsNullOrEmpty(attribute.FileName) ? source.GetType().Name : attribute.FileName;
            Obfuscate = attribute.Obfuscate;
        }

        // Makes the copy of an asset. Returns false with a reason when the asset's class cannot be a container.
        public static bool TryCreate(DataContainer source, out DataContainerSlot slot, out string error)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            slot = null;
            error = null;

            Type type = source.GetType();
            var attribute = type.GetCustomAttribute<DataFileAttribute>(inherit: false);

            if (attribute == null)
            {
                error = $"{type.Name} has no [DataFile] attribute, so the data service cannot tell whether to save it.";
                return false;
            }

            string fileName = string.IsNullOrEmpty(attribute.FileName) ? type.Name : attribute.FileName;

            if (!DataStorage.IsValidFileName(fileName))
            {
                error = $"{type.Name} has the file name \"{fileName}\", which cannot be used as a file name.";
                return false;
            }

            DataContainer container = UnityEngine.Object.Instantiate(source);
            container.name = source.name;

            slot = new DataContainerSlot(source, container, attribute);
            slot._lastJson = container.ToSaveJson();

            return true;
        }

        // Build-only containers use a file in development builds, so that tuning done on a device survives a restart.
        public bool UsesFile(bool isDevelopmentBuild)
        {
            return Persistence switch
            {
                DataPersistence.Saved => true,
                DataPersistence.BuildOnly => isDevelopmentBuild,
                _ => throw new ArgumentOutOfRangeException(nameof(Persistence))
            };
        }

        public bool NeedsWrite()
        {
            bool isMissingItsFile = Persistence == DataPersistence.Saved && !HasFile;

            return isMissingItsFile || !string.Equals(Container.ToSaveJson(), _lastJson, StringComparison.Ordinal);
        }

        // Starts from the defaults and reads the file, falling back to the backup. Deletes nothing: when the
        // result is DefaultsChanged the caller decides whether the outdated file goes.
        public DataLoadResult Load(DataStorage storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            DataLoadResult result = LoadFromFiles(storage);

            _hasUnreadableFile = result == DataLoadResult.Backup || result == DataLoadResult.Unreadable;

            bool needsRewrite = result == DataLoadResult.Backup || _wasMigrated;
            _lastJson = needsRewrite ? null : Container.ToSaveJson();

            return result;
        }

        private DataLoadResult LoadFromFiles(DataStorage storage)
        {
            ResetToDefaults();

            HasFile = false;
            _wasMigrated = false;

            DataLoadResult result = DataLoadResult.NoFile;

            foreach (DataFileKind kind in _fileKinds)
            {
                DataReadResult readResult = storage.Read(FileName, kind, Obfuscate, out DataFileContent content);

                if (readResult == DataReadResult.Missing)
                {
                    continue;
                }

                if (readResult == DataReadResult.Read)
                {
                    if (IsMadeFromOtherDefaults(content))
                    {
                        return DataLoadResult.DefaultsChanged;
                    }

                    if (TryApply(content))
                    {
                        HasFile = true;
                        return kind == DataFileKind.Current ? DataLoadResult.Current : DataLoadResult.Backup;
                    }
                }

                result = DataLoadResult.Unreadable;
            }

            return result;
        }

        public void Save(DataStorage storage)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            // A save made by a newer build keeps its version number, so that build does not migrate it twice.
            int version = Math.Max(_loadedVersion, Container.CurrentVersion);

            string json = Container.ToSaveJson();
            var content = new DataFileContent(version, GetDefaultsChecksum(), json);

            // A current file that could not be read must not replace the backup, which may be the last good save.
            DataReplaceRule replaceRule =
                _hasUnreadableFile ? DataReplaceRule.SetAside : DataReplaceRule.KeepAsBackup;

            storage.Write(FileName, Obfuscate, content, replaceRule);

            HasFile = true;
            _hasUnreadableFile = false;
            _lastJson = json;
        }

        public void ResetToDefaults()
        {
            Container.CopyValuesFrom(Source);
            _loadedVersion = 0;
        }

        // Used after the file was deleted, so that the next save compares against the current values.
        public void MarkFileDeleted()
        {
            HasFile = false;
            _hasUnreadableFile = false;
            _lastJson = Container.ToSaveJson();
        }

        public void DestroyContainer()
        {
            if (Container == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(Container);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(Container);
            }
        }

        private bool IsMadeFromOtherDefaults(DataFileContent content)
        {
            if (Persistence != DataPersistence.BuildOnly)
            {
                return false;
            }

            return !string.Equals(content.DefaultsChecksum, GetDefaultsChecksum(), StringComparison.Ordinal);
        }

        private bool TryApply(DataFileContent content)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(content.Json, Container);
            }
            catch (ArgumentException)
            {
                ResetToDefaults();
                return false;
            }

            _loadedVersion = content.Version;

            if (content.Version < Container.CurrentVersion)
            {
                Migrate(content);
            }
            else if (content.Version > Container.CurrentVersion)
            {
                Debug.LogWarning(
                    $"{FileName} was saved by a newer version of the game ({content.Version}, this build has " +
                    $"{Container.CurrentVersion}). It was loaded as it is.", Source);
            }

            return true;
        }

        private void Migrate(DataFileContent content)
        {
            try
            {
                Container.MigrateInternal(content.Version, content.Json);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Source.GetType().Name}.Migrate threw while bringing a save from version {content.Version} " +
                    $"to {Container.CurrentVersion}.", Source);
                Debug.LogException(exception, Source);
            }

            _wasMigrated = true;
        }

        // A build-only container remembers which defaults its saved values were made from. Saved containers
        // keep their values whatever the defaults become.
        private string GetDefaultsChecksum()
        {
            if (Persistence != DataPersistence.BuildOnly)
            {
                return string.Empty;
            }

            using MD5 hashAlgorithm = MD5.Create();
            byte[] hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(Source.ToSaveJson()));

            var builder = new StringBuilder(hash.Length * 2);

            foreach (byte value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using AngryKoala.Services;
using UnityEngine;

namespace AngryKoala.Data
{
    public sealed class DataService : BaseService<IDataService>, IDataService
    {
        [SerializeField] [Tooltip("One asset per data class. Each asset holds the defaults of its class.")]
        private List<DataContainer> _containers = new();

        internal List<DataContainer> ContainerAssets => _containers;

        [SerializeField] [Tooltip("Text the key for obfuscated containers is made from.")]
        private string _obfuscationKey;

        internal string ObfuscationKey
        {
            get => _obfuscationKey;
            set => _obfuscationKey = value;
        }

        [SerializeField] [Tooltip("Save changed containers when the app is paused, quits or the service is destroyed.")]
        private bool _saveOnPause = true;

        internal bool SaveOnPause
        {
            get => _saveOnPause;
            set => _saveOnPause = value;
        }

        [SerializeField] [Tooltip("Log every load and save.")]
        private bool _verbose;

        internal string FolderPath
        {
            get => _folderPath ?? DefaultFolderPath;
            set => _folderPath = value;
        }

        internal bool IsDevelopmentBuild
        {
            get => _isDevelopmentBuild ?? Debug.isDebugBuild;
            set => _isDevelopmentBuild = value;
        }

        internal static string DefaultFolderPath => Path.Combine(Application.persistentDataPath, _folderName);

        private readonly List<DataContainerSlot> _slots = new();
        private readonly Dictionary<Type, DataContainerSlot> _slotsByType = new();
        private readonly HashSet<string> _fileNames = new(StringComparer.OrdinalIgnoreCase);

        private DataStorage _storage;

        private string _folderPath;
        private bool? _isDevelopmentBuild;

        private const string _folderName = "Data";

        protected override void Awake()
        {
            if (IsSecondInstance())
            {
                Debug.LogError(
                    "DataService: another data service is already registered. This one loads and saves nothing, " +
                    "so that two services cannot write over each other's files.", this);
                return;
            }

            // Loading comes before registering, so that whoever finds the service finds it loaded.
            LoadContainers();

            base.Awake();
        }

        private bool IsSecondInstance()
        {
            return _autoRegister && ServiceLocator.TryGet(out IDataService registeredService) &&
                   !ReferenceEquals(registeredService, this);
        }

        private void LoadContainers()
        {
            _storage = new DataStorage(FolderPath, _obfuscationKey);

            for (int i = 0; i < _containers.Count; i++)
            {
                DataContainer source = _containers[i];

                if (source == null)
                {
                    Debug.LogError($"DataService: entry {i} of the container list is empty.", this);
                    continue;
                }

                AddSlot(source);
            }
        }

        private void AddSlot(DataContainer source)
        {
            Type type = source.GetType();

            if (_slotsByType.ContainsKey(type))
            {
                Debug.LogError(
                    $"DataService: {type.Name} is in the container list twice. {source.name} was left out.", this);
                return;
            }

            if (!DataContainerSlot.TryCreate(source, out DataContainerSlot slot, out string error))
            {
                Debug.LogError($"DataService: {error}", this);
                return;
            }

            if (!_fileNames.Add(slot.FileName))
            {
                Debug.LogError(
                    $"DataService: {type.Name} uses the file name \"{slot.FileName}\", which another container " +
                    "already uses. It was left out.", this);
                slot.DestroyContainer();
                return;
            }

            if (slot.Obfuscate && !_storage.CanObfuscate)
            {
                Debug.LogError(
                    $"DataService: {type.Name} asks for obfuscation, but the service has no obfuscation key. " +
                    "It is saved as plain text.", this);
            }

            _slots.Add(slot);
            _slotsByType.Add(type, slot);

            if (slot.UsesFile(IsDevelopmentBuild))
            {
                LoadSlot(slot);
            }
        }

        private void LoadSlot(DataContainerSlot slot)
        {
            string containerName = slot.Source.GetType().Name;
            DataLoadResult result;

            // Whatever goes wrong with one container's files, the others still load and the service still registers.
            try
            {
                result = slot.Load(_storage);
            }
            catch (Exception exception)
            {
                Debug.LogError($"DataService: {containerName} could not be loaded. The defaults are used.", this);
                Debug.LogException(exception, this);

                slot.ResetToDefaults();
                return;
            }

            switch (result)
            {
                case DataLoadResult.NoFile:
                {
                    LogVerbose($"{containerName}: no saved file, using the defaults.");
                    break;
                }

                case DataLoadResult.Current:
                {
                    LogVerbose($"{containerName} loaded.");
                    break;
                }

                case DataLoadResult.Backup:
                {
                    Debug.LogWarning(
                        $"DataService: the saved file of {containerName} is missing or cannot be read. " +
                        "The backup was loaded.", this);
                    break;
                }

                case DataLoadResult.Unreadable:
                {
                    Debug.LogWarning(
                        $"DataService: neither the saved file of {containerName} nor its backup can be read. " +
                        "The defaults are used.", this);
                    break;
                }

                case DataLoadResult.DefaultsChanged:
                {
                    Debug.Log(
                        $"DataService: the defaults of {containerName} have changed since its values were saved. " +
                        "The saved values were dropped.", this);

                    DeleteFile(slot);
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(result));
                }
            }
        }

        public T Get<T>() where T : DataContainer
        {
            if (TryGet(out T container))
            {
                return container;
            }

            Debug.LogError($"DataService: {typeof(T).Name} is not in the container list.", this);

            return null;
        }

        public bool TryGet<T>(out T container) where T : DataContainer
        {
            if (_slotsByType.TryGetValue(typeof(T), out DataContainerSlot slot))
            {
                container = (T)slot.Container;
                return true;
            }

            container = null;
            return false;
        }

        public void Save<T>() where T : DataContainer
        {
            if (TryGetSlot<T>(out DataContainerSlot slot))
            {
                SaveSlot(slot);
            }
        }

        public void SaveAll()
        {
            foreach (DataContainerSlot slot in _slots)
            {
                SaveSlot(slot);
            }
        }

        public void Reset<T>() where T : DataContainer
        {
            if (TryGetSlot<T>(out DataContainerSlot slot))
            {
                ResetSlot(slot);
            }
        }

        public void ResetAll()
        {
            foreach (DataContainerSlot slot in _slots)
            {
                ResetSlot(slot);
            }
        }

        public void GetContainers(List<DataContainer> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            foreach (DataContainerSlot slot in _slots)
            {
                results.Add(slot.Container);
            }
        }

        #region Internal Methods

        // For the inspector, which has a runtime copy in hand and no type argument.
        internal void SaveInternal(DataContainer container)
        {
            if (container != null && _slotsByType.TryGetValue(container.GetType(), out DataContainerSlot slot))
            {
                SaveSlot(slot);
            }
        }

        internal void ResetInternal(DataContainer container)
        {
            if (container != null && _slotsByType.TryGetValue(container.GetType(), out DataContainerSlot slot))
            {
                ResetSlot(slot);
            }
        }

        #endregion

        private bool TryGetSlot<T>(out DataContainerSlot slot) where T : DataContainer
        {
            if (_slotsByType.TryGetValue(typeof(T), out slot))
            {
                return true;
            }

            Debug.LogError($"DataService: {typeof(T).Name} is not in the container list.", this);

            return false;
        }

        private void SaveSlot(DataContainerSlot slot)
        {
            if (!slot.UsesFile(IsDevelopmentBuild) || !slot.NeedsWrite())
            {
                return;
            }

            string containerName = slot.Source.GetType().Name;

            try
            {
                slot.Save(_storage);
            }
            catch (Exception exception)
            {
                Debug.LogError($"DataService: {containerName} could not be saved.", this);
                Debug.LogException(exception, this);
                return;
            }

            LogVerbose($"{containerName} saved.");
        }

        private void ResetSlot(DataContainerSlot slot)
        {
            slot.ResetToDefaults();

            switch (slot.Persistence)
            {
                case DataPersistence.Saved:
                {
                    SaveSlot(slot);
                    break;
                }

                case DataPersistence.BuildOnly:
                {
                    if (slot.UsesFile(IsDevelopmentBuild))
                    {
                        DeleteFile(slot);
                    }

                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(slot.Persistence));
                }
            }

            LogVerbose($"{slot.Source.GetType().Name} reset to its defaults.");
        }

        private void DeleteFile(DataContainerSlot slot)
        {
            try
            {
                _storage.Delete(slot.FileName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"DataService: the saved file of {slot.Source.GetType().Name} could not be deleted.",
                    this);
                Debug.LogException(exception, this);
                return;
            }

            slot.MarkFileDeleted();
        }

        private void LogVerbose(string message)
        {
            if (_verbose)
            {
                Debug.Log($"DataService: {message}", this);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveChanged();
            }
        }

        private void OnApplicationQuit()
        {
            SaveChanged();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            SaveChanged();

            foreach (DataContainerSlot slot in _slots)
            {
                slot.DestroyContainer();
            }

            _slots.Clear();
            _slotsByType.Clear();
            _fileNames.Clear();
        }

        private void SaveChanged()
        {
            if (_saveOnPause && _storage != null)
            {
                SaveAll();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AngryKoala.Data
{
    [CustomEditor(typeof(DataService))]
    public sealed class DataServiceEditor : Editor
    {
        private SerializedProperty _scriptProperty;
        private SerializedProperty _autoRegisterProperty;
        private SerializedProperty _containersProperty;
        private SerializedProperty _obfuscationKeyProperty;
        private SerializedProperty _saveOnPauseProperty;
        private SerializedProperty _verboseProperty;

        private readonly List<ContainerView> _views = new();
        private readonly List<DataContainer> _runtimeContainers = new();
        private readonly List<(string message, MessageType type)> _problems = new();
        private readonly HashSet<Type> _expandedTypes = new();

        private GUIStyle _noteStyle;
        private int? _signature;

        private const string _scriptPropertyPath = "m_Script";
        private const string _srOptionsPropertyPath = "_srDataOptions";

        // The button row below the notes brings about 6 px of margin of its own, so the space above the notes
        // is that much larger for the two gaps to look the same.
        private const float _spaceAboveNotes = 10f;
        private const float _spaceBelowNotes = 4f;

        private void OnEnable()
        {
            _scriptProperty = serializedObject.FindProperty(_scriptPropertyPath);
            _autoRegisterProperty = serializedObject.FindProperty("_autoRegister");
            _containersProperty = serializedObject.FindProperty("_containers");
            _obfuscationKeyProperty = serializedObject.FindProperty("_obfuscationKey");
            _saveOnPauseProperty = serializedObject.FindProperty("_saveOnPause");
            _verboseProperty = serializedObject.FindProperty("_verbose");

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            DestroyViews();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_scriptProperty);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(_autoRegisterProperty);
            EditorGUILayout.PropertyField(_containersProperty);
            EditorGUILayout.PropertyField(_obfuscationKeyProperty);
            EditorGUILayout.PropertyField(_saveOnPauseProperty);
            EditorGUILayout.PropertyField(_verboseProperty);

            serializedObject.ApplyModifiedProperties();

            // Views are only rebuilt on the layout event, so that the controls of a frame stay the same.
            if (Event.current.type == EventType.Layout)
            {
                RefreshIfChanged();
            }

            DrawProblems();
            DrawCurrentValues();
            DrawFolderButtons();
        }

        // The runtime values change while the game runs.
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            DestroyViews();
            Repaint();
        }

        private void RefreshIfChanged()
        {
            if (Application.isPlaying)
            {
                ((DataService)target).GetContainers(_runtimeContainers);
            }
            else
            {
                _runtimeContainers.Clear();
            }

            int signature = ComputeSignature();

            if (_signature == signature && AreViewsAlive())
            {
                return;
            }

            DestroyViews();
            BuildProblems();

            if (Application.isPlaying)
            {
                BuildPlayModeViews();
            }
            else
            {
                BuildEditModeViews();
            }

            _signature = signature;
        }

        private int ComputeSignature()
        {
            var hash = new HashCode();

            hash.Add(Application.isPlaying);
            hash.Add(_obfuscationKeyProperty.stringValue, StringComparer.Ordinal);

            for (int i = 0; i < _containersProperty.arraySize; i++)
            {
                UnityEngine.Object asset = _containersProperty.GetArrayElementAtIndex(i).objectReferenceValue;

                hash.Add(asset);

                // Editing a default asset changes its dirty count, so that its view is built again.
                if (!Application.isPlaying && asset != null)
                {
                    hash.Add(EditorUtility.GetDirtyCount(asset));
                }
            }

            foreach (DataContainer container in _runtimeContainers)
            {
                hash.Add(container);
            }

            return hash.ToHashCode();
        }

        private bool AreViewsAlive()
        {
            foreach (ContainerView view in _views)
            {
                if (view.Container == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void DestroyViews()
        {
            foreach (ContainerView view in _views)
            {
                view.SerializedObject.Dispose();

                // Only the temporary copies of the edit mode are ours to destroy.
                view.Slot?.DestroyContainer();
            }

            _views.Clear();
            _signature = null;
        }

        private void BuildProblems()
        {
            _problems.Clear();

            bool hasKey = !string.IsNullOrEmpty(_obfuscationKeyProperty.stringValue);
            var seenTypes = new HashSet<Type>();
            var seenFileNames = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _containersProperty.arraySize; i++)
            {
                var asset = _containersProperty.GetArrayElementAtIndex(i).objectReferenceValue as DataContainer;

                if (asset == null)
                {
                    _problems.Add(($"Entry {i} of the container list is empty.", MessageType.Error));
                    continue;
                }

                Type type = asset.GetType();

                if (!seenTypes.Add(type))
                {
                    _problems.Add(($"{type.Name} is in the container list twice. {asset.name} is left out.",
                        MessageType.Error));
                    continue;
                }

                // The slot knows why a class cannot be a container. Its copy is only needed for that.
                if (!DataContainerSlot.TryCreate(asset, out DataContainerSlot slot, out string error))
                {
                    _problems.Add((error, MessageType.Error));
                    continue;
                }

                string fileName = slot.FileName;
                slot.DestroyContainer();

                if (seenFileNames.TryGetValue(fileName, out Type otherType))
                {
                    _problems.Add(($"{type.Name} and {otherType.Name} have the file name \"{fileName}\", which is " +
                                   $"the same when the case is ignored. The service refuses {type.Name} at start.",
                        MessageType.Error));
                    continue;
                }

                seenFileNames.Add(fileName, type);

                var attribute = type.GetCustomAttribute<DataFileAttribute>(inherit: false);

                if (attribute.Obfuscate && !hasKey)
                {
                    _problems.Add(($"{type.Name} asks for obfuscation, but the obfuscation key is empty. " +
                                   "It is saved as plain text.", MessageType.Warning));
                }

                if (TryFindObjectReference(asset, out string propertyPath))
                {
                    _problems.Add((GetObjectReferenceMessage(type, attribute.Persistence, propertyPath),
                        MessageType.Warning));
                }
            }
        }

        private void BuildPlayModeViews()
        {
            foreach (DataContainer container in _runtimeContainers)
            {
                if (container == null)
                {
                    continue;
                }

                var attribute = container.GetType().GetCustomAttribute<DataFileAttribute>(inherit: false);

                if (attribute == null)
                {
                    continue;
                }

                _views.Add(new ContainerView(null, container, GetLabel(container.GetType(), attribute.Persistence),
                    attribute.Persistence));
            }
        }

        private void BuildEditModeViews()
        {
            DataStorage storage = CreateStorage();
            var seenTypes = new HashSet<Type>();

            for (int i = 0; i < _containersProperty.arraySize; i++)
            {
                var asset = _containersProperty.GetArrayElementAtIndex(i).objectReferenceValue as DataContainer;

                if (asset == null || !seenTypes.Add(asset.GetType()))
                {
                    continue;
                }

                if (!DataContainerSlot.TryCreate(asset, out DataContainerSlot slot, out _))
                {
                    continue;
                }

                // Not HideAndDontSave: that includes NotEditable, which draws every field of the copy disabled.
                slot.Container.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

                var view = new ContainerView(slot, slot.Container, GetLabel(asset.GetType(), slot.Persistence),
                    slot.Persistence);

                LoadView(view, storage);
                _views.Add(view);
            }
        }

        private void LoadView(ContainerView view, DataStorage storage)
        {
            try
            {
                view.Status = GetStatus(view.Slot.Load(storage));
            }
            catch (Exception exception)
            {
                view.Status = "The saved file could not be read.";

                Debug.LogError($"DataService: the saved file of {view.Container.GetType().Name} could not be read.",
                    target);
                Debug.LogException(exception, target);
            }

            view.RefreshNeedsWrite();
        }

        private void DrawProblems()
        {
            foreach ((string message, MessageType type) in _problems)
            {
                EditorGUILayout.HelpBox(message, type);
            }
        }

        private void DrawCurrentValues()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Values", EditorStyles.boldLabel);

            if (_views.Count == 0)
            {
                _noteStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    alignment = TextAnchor.MiddleLeft
                };

                EditorGUILayout.LabelField(
                    Application.isPlaying ? "The service holds no containers." : "No container in the list can be shown.",
                    _noteStyle);
                return;
            }

            foreach (ContainerView view in _views)
            {
                DrawView(view);
            }
        }

        private void DrawView(ContainerView view)
        {
            if (view.Container == null)
            {
                return;
            }

            Type type = view.Container.GetType();
            bool isExpanded = _expandedTypes.Contains(type);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // One level in, so the foldout arrow sits inside the box and not on its border.
                EditorGUI.indentLevel++;

                bool newExpanded = EditorGUILayout.Foldout(isExpanded, view.Label, toggleOnLabelClick: true);

                if (newExpanded != isExpanded)
                {
                    if (newExpanded)
                    {
                        _expandedTypes.Add(type);
                    }
                    else
                    {
                        _expandedTypes.Remove(type);
                    }
                }

                if (newExpanded)
                {
                    DrawViewFields(view);

                    if (view.Slot != null)
                    {
                        DrawViewNotes(view);
                    }

                    DrawViewButtons(view);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawViewFields(ContainerView view)
        {
            view.SerializedObject.Update();

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();

            SerializedProperty iterator = view.SerializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == _scriptPropertyPath || iterator.propertyPath == _srOptionsPropertyPath)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, includeChildren: true);
            }

            bool hasChanged = EditorGUI.EndChangeCheck();

            EditorGUI.indentLevel--;

            view.SerializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (hasChanged)
            {
                view.RefreshNeedsWrite();
            }
        }

        private void DrawViewNotes(ContainerView view)
        {
            _noteStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };

            // The notes line up with the field labels above them, with the same air above and below. The text is
            // centred in its line: a mini label otherwise sits at the top of it, which made the gap above look smaller.
            EditorGUILayout.Space(_spaceAboveNotes);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField(view.Status, _noteStyle);

            if (view.Persistence == DataPersistence.BuildOnly)
            {
                EditorGUILayout.LabelField(
                    "Its values are saved in development builds and the Editor only, and dropped when the " +
                    "asset's defaults change.", _noteStyle);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(_spaceBelowNotes);
        }

        private void DrawViewButtons(ContainerView view)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (view.Slot == null)
                {
                    if (GUILayout.Button("Save"))
                    {
                        ((DataService)target).SaveInternal(view.Container);
                    }

                    if (GUILayout.Button("Reset"))
                    {
                        ResetAfterConfirmation(view);
                    }
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(!view.NeedsWrite);

                    if (GUILayout.Button("Save"))
                    {
                        SaveView(view);
                    }

                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button("Reload"))
                    {
                        ReloadView(view);
                    }

                    if (GUILayout.Button("Reset"))
                    {
                        ResetAfterConfirmation(view);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SaveView(ContainerView view)
        {
            try
            {
                view.Slot.Save(CreateStorage());
                view.Status = "Saved.";
            }
            catch (Exception exception)
            {
                Debug.LogError($"DataService: {view.Container.GetType().Name} could not be saved.", target);
                Debug.LogException(exception, target);
            }

            view.RefreshNeedsWrite();
        }

        private void ReloadView(ContainerView view)
        {
            view.Slot.ResetToDefaults();
            LoadView(view, CreateStorage());
        }

        private void ResetView(ContainerView view)
        {
            try
            {
                DataStorage storage = CreateStorage();

                view.Slot.ResetToDefaults();

                switch (view.Persistence)
                {
                    case DataPersistence.Saved:
                    {
                        view.Slot.Save(storage);
                        break;
                    }

                    case DataPersistence.BuildOnly:
                    {
                        storage.Delete(view.Slot.FileName);
                        view.Slot.MarkFileDeleted();
                        break;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(view.Persistence));
                    }
                }

                view.Status = "Reset to the defaults.";
            }
            catch (Exception exception)
            {
                Debug.LogError($"DataService: {view.Container.GetType().Name} could not be reset.", target);
                Debug.LogException(exception, target);
            }

            view.RefreshNeedsWrite();
        }

        private void ResetAfterConfirmation(ContainerView view)
        {
            bool isConfirmed = EditorUtility.DisplayDialog($"Reset {view.Container.GetType().Name}",
                "This replaces the current values with the defaults of the asset and updates the saved file. " +
                "Are you sure?", "Yes", "No");

            if (isConfirmed)
            {
                if (view.Slot == null)
                {
                    ((DataService)target).ResetInternal(view.Container);
                }
                else
                {
                    ResetView(view);
                }
            }

            // The dialog ran inside a layout group, so the rest of this event must not be drawn.
            GUIUtility.ExitGUI();
        }

        private void DrawFolderButtons()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Open Data Folder"))
                {
                    OpenDataFolder();
                }

                EditorGUI.BeginDisabledGroup(Application.isPlaying);

                if (GUILayout.Button("Delete Saved Files"))
                {
                    DeleteSavedFiles();
                }

                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OpenDataFolder()
        {
            string folderPath = DataService.DefaultFolderPath;

            try
            {
                Directory.CreateDirectory(folderPath);
                EditorUtility.RevealInFinder(folderPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("DataService: the data folder could not be opened.", target);
                Debug.LogException(exception, target);
            }
        }

        private void DeleteSavedFiles()
        {
            bool isConfirmed = EditorUtility.DisplayDialog("Delete Saved Files",
                "This deletes the saved file, the backup and the temporary file of every container in the " +
                "list. This cannot be undone. Are you sure?", "Delete", "Cancel");

            if (isConfirmed)
            {
                DeleteViewFiles();
            }

            // The dialog ran inside a layout group, so the rest of this event must not be drawn.
            GUIUtility.ExitGUI();
        }

        private void DeleteViewFiles()
        {
            DataStorage storage = CreateStorage();

            foreach (ContainerView view in _views)
            {
                if (view.Slot == null)
                {
                    continue;
                }

                try
                {
                    storage.Delete(view.Slot.FileName);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"DataService: the saved files of {view.Container.GetType().Name} could not be deleted.",
                        target);
                    Debug.LogException(exception, target);
                }
            }

            // The views are rebuilt by the next layout event.
            _signature = null;
            Repaint();
        }

        private DataStorage CreateStorage()
        {
            return new DataStorage(DataService.DefaultFolderPath, _obfuscationKeyProperty.stringValue);
        }

        private static bool TryFindObjectReference(DataContainer asset, out string propertyPath)
        {
            propertyPath = null;

            using var serializedAsset = new SerializedObject(asset);

            SerializedProperty iterator = serializedAsset.GetIterator();

            // Visible properties only: every object has hidden references of Unity's own.
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                    iterator.propertyPath != _scriptPropertyPath)
                {
                    propertyPath = iterator.propertyPath;
                    return true;
                }
            }

            return false;
        }

        private static string GetObjectReferenceMessage(Type type, DataPersistence persistence, string propertyPath)
        {
            return persistence switch
            {
                DataPersistence.Saved =>
                    $"{type.Name} is saved and has a reference to a Unity object ({propertyPath}). " +
                    "Such a reference is written as an id that means nothing in the next session.",
                DataPersistence.BuildOnly =>
                    $"{type.Name} is build-only and has a reference to a Unity object ({propertyPath}). " +
                    "Its checksum changes every session, so the saved tuning is dropped at every start.",
                _ => throw new ArgumentOutOfRangeException(nameof(persistence))
            };
        }

        private static string GetLabel(Type type, DataPersistence persistence)
        {
            string kind = persistence switch
            {
                DataPersistence.Saved => "saved",
                DataPersistence.BuildOnly => "tuning",
                _ => throw new ArgumentOutOfRangeException(nameof(persistence))
            };

            return $"{type.Name} ({kind})";
        }

        private static string GetStatus(DataLoadResult result)
        {
            return result switch
            {
                DataLoadResult.NoFile => "No saved file, showing the defaults.",
                DataLoadResult.Current => "Loaded from the saved file.",
                DataLoadResult.Backup => "The saved file could not be read, the backup was loaded.",
                DataLoadResult.Unreadable => "Neither the saved file nor its backup can be read, showing the defaults.",
                DataLoadResult.DefaultsChanged =>
                    "The defaults changed since the values were saved. The saved values are outdated and will be " +
                    "dropped at the next Play.",
                _ => throw new ArgumentOutOfRangeException(nameof(result))
            };
        }

        private sealed class ContainerView
        {
            public DataContainerSlot Slot { get; }

            public DataContainer Container { get; }

            public SerializedObject SerializedObject { get; }

            public string Label { get; }

            public DataPersistence Persistence { get; }

            public string Status { get; set; }

            public bool NeedsWrite { get; private set; }

            public ContainerView(DataContainerSlot slot, DataContainer container, string label,
                DataPersistence persistence)
            {
                Slot = slot;
                Container = container;
                SerializedObject = new SerializedObject(container);
                Label = label;
                Persistence = persistence;
            }

            // Asking the slot compares the whole container as JSON, so it is not done on every event.
            public void RefreshNeedsWrite() => NeedsWrite = Slot != null && Slot.NeedsWrite();
        }
    }
}

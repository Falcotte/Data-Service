# Angry Koala Data

A data service for Unity. The game defines its own data classes as ScriptableObjects. The service loads each class from a file when the game starts, hands out a runtime copy, and saves the copy when it changes. Each class has its defaults in an asset. Writes go to a temporary file first and a backup is kept, so a damaged file does not stop the others from loading. The Data Inspector shows and edits the current values in Play Mode and in the Editor. The module depends on the Services module to find the service.

**Use it for**

- Player progress, such as level, lives, score and currency, that must be kept between sessions.
- Settings, such as music, sound and vibration.
- Tuning values that are set in the build, and that can be changed on a device in development builds for testing.

**Do not use it for**

- Cloud saves, profiles, or several save slots. The module keeps one save per game.
- Data that a server must check. The saves are on the device and can be edited.
- Plain values with no save needs. Use a plain field.

## Install

Add the package to the project's `Packages/manifest.json`. The Services module must be listed too:

```json
"com.angrykoala.data": "https://github.com/Falcotte/Data-Service.git?path=/Assets/Data#v2.0.0",
"com.angrykoala.services": "https://github.com/Falcotte/Service-Locator.git?path=/Assets/Services#v2.0.0"
```

Unity does not resolve git dependencies between packages. Each AngryKoala module must be listed in the project's own `manifest.json`.

Minimum Unity version: 6000.3 (from `package.json`). Developed on Unity 6000.6.4f1.

## Public interface

```csharp
public interface IDataService : IService
{
    // The runtime copy of a container. Logs an error and returns null when the type is not in the list.
    T Get<T>() where T : DataContainer;

    // The runtime copy of a container, or false when the type is not in the list. Logs nothing.
    bool TryGet<T>(out T container) where T : DataContainer;

    // Writes the container when its values changed since the last write or load, or when a saved container has no file yet.
    void Save<T>() where T : DataContainer;

    // Calls Save for every container.
    void SaveAll();

    // Copies the asset's defaults over the runtime copy, which stays the same object, then saves. For a build-only
    // container in a development build, its tuning file is deleted instead.
    void Reset<T>() where T : DataContainer;

    // Calls Reset for every container.
    void ResetAll();

    // Clears the list and fills it with the runtime copies. Throws ArgumentNullException on a null list.
    void GetContainers(List<DataContainer> results);
}

public abstract class DataContainer : ScriptableObject
{
    // Raise it when a field is renamed, removed or changes meaning. Defaults to 1.
    protected virtual int Version => 1;

    // Called after a save written by an older version was loaded. savedJson is the raw JSON of that save.
    protected virtual void Migrate(int savedVersion, string savedJson);

    // The debug option settings of the fields, as set in the Inspector. Used by the SRDebugger page.
    public IReadOnlyList<SRDataOptionEntry> SRDataOptions { get; }
}

public enum DataPersistence
{
    // Read and written on the device in every build.
    Saved,

    // Read and written on the device only in development builds and the Editor. Never in release builds.
    BuildOnly
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DataFileAttribute : Attribute
{
    // Every class that derives from DataContainer needs this attribute.
    public DataFileAttribute(DataPersistence persistence);

    public DataPersistence Persistence { get; }

    // The file name without extension. Defaults to the class name.
    public string FileName { get; set; }

    // Encrypts the file with the Obfuscation Key of the service.
    public bool Obfuscate { get; set; }
}

// DataService is the component: a MonoBehaviour that extends BaseService<IDataService>.
// Its Inspector fields are Containers (the list of assets), Obfuscation Key, Save On Pause (default on) and Verbose.

// Editor only (AngryKoala.Data.Editor):
public static class SRDataOptionsUtility
{
    // Adds an entry for each supported field that has none, and removes entries of fields that are gone.
    public static void SyncOptions(DataContainer container);

    // Puts the sort value of every entry back to the position of its field in the class.
    public static void ResetSortOrder(DataContainer container);

    // Turns an identifier such as "bestScore" into "Best Score".
    public static string ToDisplayName(string identifier);
}
```

## Example

A player's progress, saved on the device. The game defines the class, creates one asset of it, and adds the asset to the `DataService` list in the scene.

```csharp
using AngryKoala.Data;
using UnityEngine;

[DataFile(DataPersistence.Saved)]
[CreateAssetMenu(menuName = "My Game/Data/Player Data")]
public sealed partial class PlayerData : DataContainer
{
    [SerializeField] private int _level = 1;
    [SerializeField] private int _lives = 5;
    [SerializeField] private int _score;
    [SerializeField] private int _currency;
}
```

```csharp
using AngryKoala.Data;
using AngryKoala.Services;
using UnityEngine;

public sealed class ScoreCounter : MonoBehaviour
{
    private IDataService _dataService;
    private PlayerData _playerData;

    private void Start()
    {
        _dataService = ServiceLocator.Get<IDataService>();
        _playerData = _dataService.Get<PlayerData>();
    }

    public void AddPoints(int points)
    {
        _playerData.Score += points;
        _dataService.Save<PlayerData>();
    }
}
```

`Score` is a property generated from `_score`. Run **Angry Koala > Data > Generate Properties** after writing the class (see Property generator below).

## Rules

**Containers and the list**

- One asset per class holds the defaults. The service makes a runtime copy of each asset in `Awake`. It never writes to the asset.
- A class without `[DataFile]`, an empty entry in the list, a second entry of the same class, a file name that is not valid, and a file name another container already uses (ignoring case) are each logged as an error and skipped. Nothing throws at start, and a problem with one container does not stop the others.
- One data service at a time. A second one that wakes while another is registered logs an error and loads and saves nothing, so two services cannot write over each other's files.
- The module does not keep the service across scene loads. Put the service where it should live. When the service is destroyed, it deregisters, saves the changed containers and destroys its runtime copies. Code that outlives the service must ask the next service for its containers again.
- A field that the saved file does not have keeps the asset's default. A new field therefore gets its default in an old save.
- `Reset` keeps the same runtime object, so a reference you hold stays valid.

**Loading and finding the service**

- The service loads every container in `Awake`, and registers only after loading. Whoever finds the service finds the saved values already in place.
- Resolve it in `Start` or later with `ServiceLocator.Get<IDataService>()`. For code that may run earlier, use `ServiceLocator.WhenRegistered<IDataService>(...)`.
- `Get<T>` of a class that is not in the list logs an error and returns null. `TryGet<T>` returns false without logging.

**Saved and build-only**

- `Saved` containers are read and written in every build.
- `BuildOnly` containers are never read or written in a release build. In development builds and the Editor they are saved and loaded like the others, so tuning changed on a device survives a restart.
- A build-only file remembers which defaults its values were made from. When the asset's defaults have changed since (a new build, or an edit to the asset), the saved tuning is dropped and the new defaults are used. This is logged as an ordinary message, not a warning.
- Saved values are kept when the defaults change. Only a field the file does not have takes the new default.

**Saving**

- `Save<T>()` and `SaveAll()` write only when something changed. A change is any difference in the saved JSON, so a changed list or a changed nested object counts. A saved container with no file yet is always written.
- Containers are saved when the app pauses, when it quits, and when the service is destroyed. Turn this off with the Save On Pause field.
- A change made through code, or on the SRDebugger page, is saved this way like any other change.

**Files**

- Files are in `Application.persistentDataPath/Data/`. Each container has `<FileName>.dat` (the file), `<FileName>.bak` (the backup), and a `.tmp` file while it is being written.
- A file has two lines. The first is a small header with the version and, for build-only containers, the checksum of the defaults. The second is the container's JSON.
- A write goes to the `.tmp` file. The current file then becomes the `.bak` file, and the `.tmp` file becomes the current file.
- A read uses the current file. If it is missing or unreadable, the backup is used. If both fail, the defaults are used. One warning says which happened. Other files are not affected.
- A file that could not be read never replaces the backup. The next save sets it aside as `<FileName>.corrupt`, where it stays until the next unreadable file or until the saved files are deleted.

**Versions and Migrate**

- Raise `Version` when a field is renamed, removed or changes meaning.
- When a save has a lower version, `Migrate(savedVersion, savedJson)` is called after loading, and the next save writes the new version. The raw JSON is passed because the value of a renamed or removed field is not loaded into the container. `JsonUtility` ignores `[FormerlySerializedAs]`.
- A save from a newer version loads as it is, with a warning, and keeps its version number when it is written again. Fields only the newer version has are lost in that write.
- When you rename a class that has saves, set `FileName` to the old name. The file keeps its name.

**Obfuscation**

- Set `Obfuscate = true` on the attribute, and the Obfuscation Key on the service. The file is then encrypted with AES, with a new random starting block for each save.
- Obfuscation keeps a curious player out. It is not security. The key is in the built game, so a determined player can read the saves.
- Changing the Obfuscation Key, or turning `Obfuscate` on or off for a class with saves, makes those saves unreadable. The game then falls back to the defaults, with a warning, and the next save sets the old file aside as `<FileName>.corrupt`.
- A class that asks for obfuscation with an empty key is logged as an error at start, and it is saved as plain text.

**Where the module stops**

- It saves what `JsonUtility` can write: numbers, strings, enums, serializable classes and structs, and lists and arrays of those.
- It does not save dictionaries. A saved container should not reference assets or scene objects. Such a reference is written as an id that means nothing in the next session, and the Inspector warns about it.
- There is one save per game. No slots, profiles or cloud sync.

**Logging**

- Warnings and errors are always logged. "Loaded" and "saved" lines appear only when Verbose is on.

## What it needs

- Unity 6000.3 or later.
- The Services module, `com.angrykoala.services` 2.0.0, listed in the project's `manifest.json`.
- Tests need `com.unity.test-framework`.
- The SRDebugger page needs SRDebugger in the project (see SRDebugger page below). SRDebugger is a paid asset and is not part of this repository. SRDebugger itself needs the Unity UI package and the built-in Animation module. Version 1.12.1 does not compile on Unity 6000.6; 1.13.2 does.

## Data Inspector

Select the `DataService` in the scene. The Inspector shows:

- A **Problems** box at the top when the list has an empty entry, a class twice, a class without `[DataFile]`, a file name that is not valid or is used twice, obfuscation with no key, or a container with a Unity object reference.
- **Current Values**, one foldout per container.
  - In Play Mode, the runtime copy, live. Each container has **Save** and **Reset**.
  - In Edit Mode, the saved file of each container, read into a temporary copy. You can edit it and press **Save**, which writes the file, so the next Play Mode starts with it. **Reload** reads the file again. **Reset** writes the defaults. If there is no file yet, the defaults are shown.
- **Open Data Folder** and **Delete Saved Files**. Delete Saved Files removes each container's file, backup and temporary file, after a confirmation. It is disabled in Play Mode.

The default assets stay editable in the Project window. The current values are edited on the service.

## Container Inspector

Each container asset has the normal fields of its class, drawn by one editor in the module. The editor is used for every class that derives from `DataContainer`, so a game writes no editor of its own.

When the project defines the scripting define `SRDEBUGGER`, each field also has an **Add to SROptions** tick box. When it is ticked, the category, display name, sort value, number range and increment appear below it. A **Reset SR Options Sort Orders** button puts the sort values back in field order. These settings are kept in the asset and are left out of the save file. They are found by the field's name, so a renamed field starts with fresh settings.

## Property generator

Run **Angry Koala > Data > Generate Properties**. It writes `<Class>.Properties.Generated.cs` beside every `partial` class that derives from `DataContainer` and is under `Assets`. Each private serialized field gets a public property, with the leading underscore removed and the first letter capitalised. The property has a plain getter and setter. Saving compares the JSON, so the setter does not need to mark anything.

- Hidden fields and non-partial classes get no property. The log says how many classes were skipped.
- A property you wrote by hand is never written again.
- A field is skipped, with a warning, when its property would not compile: the name is not a valid identifier, equals the class name, is already taken by another field, or hides a member of `DataContainer` such as `Version`.
- The generator reads the compiled class. After removing or renaming a field, the old generated property may stop the project from compiling. Delete the generated file, let Unity compile, and run the generator again.
- Do not edit a generated file. It is overwritten on the next run.

## SRDebugger page

Needs SRDebugger in the project, and the scripting define `SRDEBUGGER`. SRDebugger is a paid asset and is not in this repository.

- While a data service is registered, each ticked field appears on the SRDebugger options page, under its category. The category defaults to the class name.
- Only private serialized fields of these types are shown: `bool`, the integer types, `float`, `double`, `string` and enums.
- No code is generated for the page. Ticking a field in the Container Inspector is enough. The page is built when the service registers.
- If no field is ticked, the module does not start SRDebugger.

## Demo

The demo is in `Assets/Demo`, outside the module. Open `Assets/Demo/Scenes/DemoScene.unity`. It has one `DataService` with three containers, in the namespace `AngryKoala.Data.Demo`:

- `PlayerData`, `Saved`: level, score, lives and currency.
- `GameData`, `BuildOnly`: maximum life count and life replenish duration, in minutes.
- `SettingsData`, `Saved`: music, sound and vibration.

The demo has no script that reads these values. Use it to try the Inspectors and the save files.

## Tests

- Play Mode tests are in `Assets/Data/Tests/Runtime`: 37 cases for the service, covering loading, saving, pause, quit, reset, migration, obfuscation, unreadable files and errors in the list.
- Edit Mode tests are in `Assets/Data/Tests/Editor`: 40 cases for file storage, the property generator and the option utility.
- SRDebugger tests are in `Assets/Data/SRDebugger/Tests/Editor`: 5 cases.

Run them from the Test Runner.

## Upgrading from 1.x

| 1.x | 2.0.0 |
|---|---|
| The module's own `PlayerData`, `GameData` and `SettingsData` classes | Your own classes that derive from `DataContainer`, each with `[DataFile]` |
| Six assets per set-up, a default and an initial asset for each class | One asset per class, holding the defaults |
| `IDataService.PlayerData`, `GameData` and `SettingsData` properties | `Get<T>()` and `TryGet<T>()` |
| `LoadPlayerData()`, `SavePlayerData()`, `ResetPlayerData()`, and the same for `GameData` and `SettingsData` | `Save<T>()` and `Reset<T>()`. Loading happens at start. |
| `_serializationFormat` (Json or Binary) | Gone. JSON only. |
| `_useEncryption` and `_encryptionPassword` on the service | `Obfuscate` on each class's `[DataFile]`, and the Obfuscation Key on the service |
| `SRDataOptions` and the generated `SROptions` code | Tick boxes in the Container Inspector. The page is built at runtime. |
| `SRDataOptionsUtility.SyncOptionsForDataObject` | `SRDataOptionsUtility.SyncOptions(DataContainer)`, in the Editor assembly |
| `SRDataOptionsUtility.ResetSortOrderForData` | `SRDataOptionsUtility.ResetSortOrder(DataContainer)`, in the Editor assembly |

Also:

- 1.x save files are not read. Players start from the defaults after the update.
- Remove the `Assets/Services` copy from the project, and list `com.angrykoala.services` 2.0.0 in `manifest.json`.
- Write your data classes, create their assets, add them to the service's list, and run **Generate Properties**.
- Resolve the service in `Start` or later, not in `Awake`.

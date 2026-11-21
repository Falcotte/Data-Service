# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.0.0] - Unreleased

### Added

- `DataContainer`, the base class of the game's own data classes. Its asset holds the defaults, and the service hands out a runtime copy.
- `DataFileAttribute` with `Persistence`, `FileName` and `Obfuscate`, and the `DataPersistence` enum (`Saved` and `BuildOnly`). Every container class needs the attribute.
- `IDataService.Get<T>()`, `TryGet<T>()`, `Save<T>()`, `SaveAll()`, `Reset<T>()`, `ResetAll()` and `GetContainers(List<DataContainer>)`.
- `DataContainer.Version` and `Migrate(savedVersion, savedJson)`, for saves written by older versions.
- Automatic saving when the app pauses, quits, or the service is destroyed, with a Save On Pause field to turn it off.
- Backup and temporary files for every save. A damaged file falls back to the backup, then to the defaults, with one warning.
- Obfuscation of a container's file with AES, using the Obfuscation Key on the service.
- Build-only tuning. In development builds and the Editor, a changed build-only value is saved, and it is dropped when the asset's defaults change.
- Verbose field on the service, for logging each load and save.
- Data Inspector with the current values in Play Mode and Edit Mode, Save, Reload and Reset per container, Open Data Folder and Delete Saved Files.
- Container Inspector, drawn for every container class, with the SRDebugger option settings when `SRDEBUGGER` is defined.
- Property generator, menu `Angry Koala/Data/Generate Properties`, which writes properties for `partial` container classes.
- SRDebugger options page, built at runtime from the ticked fields, when `SRDEBUGGER` is defined.
- Demo scene and three demo containers in `Assets/Demo`.
- Play Mode tests in `Assets/Data/Tests/Runtime`, and Edit Mode tests in `Assets/Data/Tests/Editor` and `Assets/Data/SRDebugger/Tests/Editor`.
- Assembly definitions `AngryKoala.Data`, `AngryKoala.Data.Editor`, `AngryKoala.Data.SRDebugger` and their test assemblies.
- `package.json` for distribution as a UPM package from `Assets/Data`, depending on `com.angrykoala.services` 2.0.0.

### Changed

- The game defines its own data classes. The module no longer has `PlayerData`, `GameData` or `SettingsData`.
- `IDataService` is generic. It no longer has one property and one load, save and reset method per class.
- The service loads every container in `Awake`, and registers only after loading.
- Each container has one asset that holds its defaults. The six-asset set-up is gone.
- Saves are JSON only. A save is written with a small header line that holds its version.
- `SRDataOptionsUtility` moved to the Editor assembly. `SyncOptionsForDataObject` is now `SyncOptions(DataContainer)`, and `ResetSortOrderForData` is now `ResetSortOrder(DataContainer)`.
- The property generator writes full type names, so a field of a type from another namespace compiles.
- Services are resolved through the Services package, not a copy under `Assets/Services`.

### Removed

- `PlayerData`, `GameData` and `SettingsData` classes of the module, and their six assets.
- `DataSerializationFormat`, and the Binary format.
- `_useEncryption`, `_encryptionPassword` and `_serializationFormat` on the service.
- The generated `SROptions` code and the `SRDataOptions` partial class of the project. The SRDebugger page no longer needs code generation.
- The per-class Inspector editors for the module's own data classes.
- The copy of the Services module under `Assets/Services`.

### Fixed

- Saved values were loaded into the project's assets, so an asset kept the values of the last play session. The service now loads into a runtime copy and never writes an asset.
- Tuning values were saved on the device like progress, so a saved value won over a changed default in a new build. Build-only containers are not read or written in release builds.
- The editor settings for the SRDebugger page were written into the player's save file. They are now left out.
- One damaged save file stopped the files after it from loading. Each file now loads on its own.
- A save overwrote the only copy of the file in place. Writes now go to a temporary file first, and the previous file is kept as a backup.
- A saved change to a list or a nested object was not detected. Changes are now found by comparing the saved JSON.
- The "Reset SR Options Sort Orders" button did nothing, because it looked for a field that did not exist.
- The property generator could write files that did not compile for types from another namespace.
- The demo assets and classes did not match. The demo now has its own three containers and their assets.
- The service logged a line on every load and save. These lines now appear only when Verbose is on.
- The service threw in `Awake` when an asset was missing, and it registered before its data was loaded.
- The obfuscation key was derived again on every load and every save. It is now derived once per service, and each save uses a new random starting block.
- Editor-only code was in a folder that the runtime build compiles. It is now in the Editor assembly.

## Upgrading from 1.x

- Replace the module's `PlayerData`, `GameData` and `SettingsData` with your own classes that derive from `DataContainer`, each with `[DataFile]`. Create one asset per class, and add the assets to the service's list.
- Replace `IDataService.PlayerData`, `GameData` and `SettingsData` with `Get<T>()`. Replace `Load...`, `Save...` and `Reset...` calls with `Save<T>()` and `Reset<T>()`. Loading happens at start.
- Remove `_serializationFormat`, `_useEncryption` and `_encryptionPassword`. Set `Obfuscate` on a class's `[DataFile]`, and set the Obfuscation Key on the service.
- Replace `SRDataOptionsUtility.SyncOptionsForDataObject` with `SRDataOptionsUtility.SyncOptions(DataContainer)`, and `ResetSortOrderForData` with `ResetSortOrder(DataContainer)`.
- 1.x save files are not read. Players start from the defaults after the update.
- Remove the `Assets/Services` copy from the project, and list `com.angrykoala.services` 2.0.0 in `manifest.json`.
- Resolve the service in `Start` or later, not in `Awake`.

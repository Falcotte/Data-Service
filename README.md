# Data Service

A modular, extensible, and fully editor-integrated **data management system** for Unity.
Designed to provide a clean workflow for **player progression**, **game configuration**, and **user settings**, the Data Service handles **loading**, **saving**, **resetting**, **serialization**, **encryption**, and **SRDebugger integration** automatically.

It is built around ScriptableObjects, works seamlessly with the **Angry Koala Service Locator**, and includes powerful **Editor tooling** and **auto-generated properties** for a smooth development experience.

---

## ✨ Features

### Unified Data Service

  * Central access point for all runtime data (PlayerData, GameData, SettingsData)

  * Automatic loading on startup

  * Safe saving, resetting, and persistence

  * Works with the `BaseService<IDataService>` service ecosystem

### ScriptableObject-Based Data Containers

  * `PlayerData`: progression / player state

  * `GameData`: game configuration values

  * `SettingsData`: user preferences (never encrypted)

### Automatic Property & SROptions Code Generation

  * Generates clean C# properties for all serialized private fields

  * Generates SRDebugger entries (optional, per-field enable toggle)

  * Uses fast, stable reflection to extract properties

### Flexible Serialization

  * Supports **JSON** and **Binary** serialization formats

  * Clean utility methods for converting between bytes ↔ JSON

### Optional AES Encryption

* Encrypts PlayerData and GameData on disk

* Uses password-derived keys with PBKDF2

* SettingsData intentionally never encrypted

### Editor Tooling

  * Custom inspectors for:

    * DataService

    * PlayerData

    * GameData

    * SettingsData

  * Helpbox command panels for Load / Save / Reset

  * "Open Data Folder" button for convenience

  * SRDebugger options editor workflow

### SRDebugger Integration

  * Easily expose data values to the SRDebugger runtime panel

  * Supports:

    * Categories

    * Display names

    * Sorting order

    * Numeric ranges and increments

  * Fully compatible with generated properties

---

## 🧩 Architecture Overview

### Core ScriptableObjects

#### `PlayerData`

Holds all player-related runtime values (e.g., level, score).
Automatically synced with SRDebugger metadata and property generator.

#### `GameData`

Stores configuration and tuning parameters (e.g., life system values).
Comes with default template used on first load or reset.

#### `SettingsData`

Stores user preferences such as music/sound/vibration.
Not encrypted — intentionally lightweight and flexible.

### `DataService`

The backbone of the system, responsible for:

  * Loading data (with fallback to defaults on first launch)

  * Saving data with serialization + optional encryption

  * Applying "initial" or "default" values on reset

  * Managing persistent storage directories

  * Integrating with the Service Locator ecosystem

It exposes strongly-typed accessors:

```
public PlayerData PlayerData => _playerData;
public GameData GameData => _gameData;
public SettingsData SettingsData => _settingsData;
```

All data paths are stored under:

```
Application.persistentDataPath/Data/
```

### Serialization Workflow

#### Loading

  1. Checks if the data file exists

  2. If not, copies default/initial template into runtime ScriptableObject

  3. Reads bytes

  4. Decrypts (if enabled and applicable)

  5. Converts bytes → JSON

  6. Overwrites ScriptableObject fields via `JsonUtility.FromJsonOverwrite`

#### Saving

  1. Converts ScriptableObject → JSON

  2. Serializes JSON → bytes

  3. Encrypts bytes if enabled (except SettingsData)

  4. Writes file to disk safely

#### Resetting

Simply applies initial/default template and saves again.

### Encryption

PlayerData & GameData may be encrypted with:

 * AES (256-bit key, 128-bit IV)

 * PBKDF2 (Rfc2898DeriveBytes)

 * Custom salt for consistency

SettingsData is intentionally always saved unencrypted.

### Data ScriptableObject Inspectors

`PlayerData`, `GameData`, and `SettingsData` get enhanced inspectors when **SRDebugger** is present:

  * Display all serialized fields

  * Per-field toggle: Add to SROptions

  * Category override

  * DisplayName override

  * Sort order

  * Optional numeric ranges + increments

  * “Reset SR Options Sort Orders” button

These editors ensure clean and consistent SRDebugger integration across your data assets.

### Properties Generation

`DataPropertyGenerator` automatically generates C# files:

```
PlayerData.Properties.Generated.cs
GameData.Properties.Generated.cs
SettingsData.Properties.Generated.cs
```

These add:

  * Standard public getter/setter properties

  * Proper C# naming based on private `_fieldName`

  * Automatic detection of supported field types

### SRDebugger Options Generation

If SRDebugger integration is enabled, additional files like:

`PlayerData.SROptions.Generated.cs`

are created, exposing clean runtime SRDebugger controls.

---

## 🧠 Usage Example

### Getting the DataService

```
IDataService _dataService = ServiceLocator.Get<IDataService>();
```

### Reading and writing values

```
int level = _dataService.PlayerData.Level;
_dataService.PlayerData.Level = 5;

_dataService.SavePlayerData();
```

### Resetting

```
_dataService.ResetGameData();
```

### Switching serialization format (Editor)

Just change the field on the DataService component.

---

## 🧱 Extending the System

You can extend this framework by:

  * Adding new fields to PlayerData/GameData/SettingsData
→ properties + SRDebugger entries are generated automatically

  * Creating new data categories via additional ScriptableObjects

  * Writing custom validation logic inside your partial classes

  * Using SRDebugger categories to group related fields

  * Injecting analytics or telemetry hooks into DataService methods

---

## 🪲 Debugging Tips

  * **Values not updating?**
Make sure you saved the data after modifying ScriptableObject values.

  * **Encrypted file unreadable?**
Check that the encryption password matches — wrong password → failed decrypt.

  * **Default values not applied?**
Ensure the Default/Initial ScriptableObject references are assigned.

  * **SRDebugger values missing?**
Ensure SRDebugger is installed and the property is marked “Add to SROptions”.

  * **Data not loading on startup?**
Confirm the DataService is in the scene and AutoRegister is enabled (or registered manually).
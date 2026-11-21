using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AngryKoala.Data.Tests
{
    public sealed class DataServiceTests
    {
        private GameObject _serviceObject;

        private static string DataPath => Path.Combine(Application.persistentDataPath, "Data");
        private static string PlayerDataPath => Path.Combine(DataPath, "PlayerData.dat");
        private static string GameDataPath => Path.Combine(DataPath, "GameData.dat");

        [SetUp]
        public void SetUp()
        {
            DeleteSavedFiles();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyService();
            DeleteSavedFiles();
        }

        [Test]
        public void Start_WithSavedPlayerData_LeavesTheObjectItWasGivenUnchanged()
        {
            PlayerData firstSessionData = CreateData<PlayerData>();
            StartService(firstSessionData, CreateData<GameData>(), CreateData<SettingsData>());
            SetValue(firstSessionData, "_level", 7);
            _serviceObject.GetComponent<DataService>().SavePlayerData();
            DestroyService();

            PlayerData secondSessionData = CreateData<PlayerData>();
            StartService(secondSessionData, CreateData<GameData>(), CreateData<SettingsData>());

            Assert.AreEqual(0, GetValue<int>(secondSessionData, "_level"),
                "Loading wrote the saved value into the object the service was given.");
        }

        [Test]
        public void Start_WithSavedGameDataAndChangedDefault_UsesTheNewDefault()
        {
            GameData firstDefault = CreateData<GameData>();
            SetValue(firstDefault, "_maxLifeCount", 3);
            StartService(CreateData<PlayerData>(), CreateData<GameData>(), CreateData<SettingsData>(),
                defaultGameData: firstDefault);
            DestroyService();

            GameData secondDefault = CreateData<GameData>();
            SetValue(secondDefault, "_maxLifeCount", 5);
            DataService service = StartService(CreateData<PlayerData>(), CreateData<GameData>(),
                CreateData<SettingsData>(), defaultGameData: secondDefault);

            Assert.AreEqual(5, GetValue<int>(service.GameData, "_maxLifeCount"),
                "The saved tuning value won over the changed default.");
        }

        [Test]
        public void Start_WithCutOffPlayerFile_LogsNoErrorAndUsesDefaults()
        {
            WriteFile(PlayerDataPath, "{\"_level\":7,\"_sco");

            DataService service = StartService(CreateData<PlayerData>(), CreateData<GameData>(),
                CreateData<SettingsData>());

            Assert.AreEqual(0, GetValue<int>(service.PlayerData, "_level"));
        }

        [Test]
        public void Start_WithCutOffPlayerFile_StillLoadsGameData()
        {
            WriteFile(PlayerDataPath, "{\"_level\":7,\"_sco");
            WriteFile(GameDataPath, "{\"_maxLifeCount\":3,\"_lifeReplenishDuration\":0}");
            LogAssert.ignoreFailingMessages = true;

            DataService service = StartService(CreateData<PlayerData>(), CreateData<GameData>(),
                CreateData<SettingsData>());

            LogAssert.ignoreFailingMessages = false;
            Assert.AreEqual(3, GetValue<int>(service.GameData, "_maxLifeCount"),
                "A damaged player file stopped the game data from loading.");
        }

        [Test]
        public void Save_WritesNoEditorSettingsIntoTheFile()
        {
            DataService service = StartService(CreateData<PlayerData>(), CreateData<GameData>(),
                CreateData<SettingsData>());

            service.SavePlayerData();

            string fileText = File.ReadAllText(PlayerDataPath);
            StringAssert.DoesNotContain("_srDataOptions", fileText);
        }

        [Test]
        public void Start_WithSaveInTheOtherFormat_LogsNoErrorAndKeepsTheSavedValue()
        {
            PlayerData firstSessionData = CreateData<PlayerData>();
            StartService(firstSessionData, CreateData<GameData>(), CreateData<SettingsData>());
            SetValue(firstSessionData, "_level", 7);
            _serviceObject.GetComponent<DataService>().SavePlayerData();
            DestroyService();

            DataService service = StartService(CreateData<PlayerData>(), CreateData<GameData>(),
                CreateData<SettingsData>(), DataSerializationFormat.Binary);

            Assert.AreEqual(7, GetValue<int>(service.PlayerData, "_level"),
                "A save written as Json could not be read after switching the format to Binary.");
        }

        private DataService StartService(PlayerData playerData, GameData gameData, SettingsData settingsData,
            DataSerializationFormat serializationFormat = DataSerializationFormat.Json,
            GameData defaultGameData = null)
        {
            _serviceObject = new GameObject("DataService");
            _serviceObject.SetActive(false);

            DataService service = _serviceObject.AddComponent<DataService>();
            SetValue(service, "_playerData", playerData);
            SetValue(service, "_gameData", gameData);
            SetValue(service, "_settingsData", settingsData);
            SetValue(service, "_defaultGameData", defaultGameData);
            SetValue(service, "_serializationFormat", serializationFormat);

            _serviceObject.SetActive(true);

            return service;
        }

        private void DestroyService()
        {
            if (_serviceObject == null)
            {
                return;
            }

            Object.DestroyImmediate(_serviceObject);
            _serviceObject = null;
        }

        #region Utility

        private static TData CreateData<TData>() where TData : ScriptableObject
        {
            return ScriptableObject.CreateInstance<TData>();
        }

        private static void WriteFile(string path, string text)
        {
            Directory.CreateDirectory(DataPath);
            File.WriteAllText(path, text);
        }

        private static void DeleteSavedFiles()
        {
            if (Directory.Exists(DataPath))
            {
                Directory.Delete(DataPath, recursive: true);
            }
        }

        private static void SetValue(object target, string fieldName, object value)
        {
            GetField(target, fieldName).SetValue(target, value);
        }

        private static TValue GetValue<TValue>(object target, string fieldName)
        {
            return (TValue)GetField(target, fieldName).GetValue(target);
        }

        private static FieldInfo GetField(object target, string fieldName)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            FieldInfo field = target.GetType().GetField(fieldName, bindingFlags);
            Assert.IsNotNull(field, $"{target.GetType().Name} has no field {fieldName}.");

            return field;
        }

        #endregion
    }
}

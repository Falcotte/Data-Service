using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AngryKoala.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AngryKoala.Data.Tests
{
    public sealed class DataServiceTests
    {
        private readonly List<Object> _createdObjects = new();

        private GameObject _serviceObject;
        private string _folderPath;

        private const string _obfuscationKey = "test key";
        private const string _cutOffFileText = "{\"Version\":1,\"DefaultsChecksum\":\"\"}\n{\"_level\":7,\"_playe";

        [SetUp]
        public void SetUp()
        {
            _folderPath = Path.Combine(Application.temporaryCachePath, "AngryKoalaDataTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            DestroyService();

            foreach (Object createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();

            if (Directory.Exists(_folderPath))
            {
                Directory.Delete(_folderPath, recursive: true);
            }
        }

        [Test]
        public void Get_ReturnsACopyWithTheAssetsValues_NotTheAsset()
        {
            TestPlayerData asset = CreateAsset<TestPlayerData>();
            asset.Level = 4;

            DataService service = StartService(asset);
            TestPlayerData playerData = service.Get<TestPlayerData>();

            Assert.AreNotSame(asset, playerData);
            Assert.AreEqual(4, playerData.Level);
        }

        [Test]
        public void Start_WithSavedValues_LoadsThemAndLeavesTheAssetUnchanged()
        {
            SaveLevel(7);

            TestPlayerData asset = CreateAsset<TestPlayerData>();
            DataService service = StartService(asset);

            Assert.AreEqual(7, service.Get<TestPlayerData>().Level);
            Assert.AreEqual(0, asset.Level, "Loading wrote the saved value into the asset.");
        }

        [Test]
        public void Start_RegistersOnlyAfterLoading()
        {
            SaveLevel(7);

            int levelSeenAtRegistration = -1;

            void OnServiceRegistered(Type type, IService registeredService)
            {
                if (registeredService is IDataService dataService)
                {
                    levelSeenAtRegistration = dataService.Get<TestPlayerData>().Level;
                }
            }

            ServiceLocator.OnServiceRegistered += OnServiceRegistered;

            try
            {
                StartService(CreateAsset<TestPlayerData>());
            }
            finally
            {
                ServiceLocator.OnServiceRegistered -= OnServiceRegistered;
            }

            Assert.AreEqual(7, levelSeenAtRegistration);
        }

        [Test]
        public void Start_WithSavedTuningAndUnchangedDefault_KeepsTheTuningInADevelopmentBuild()
        {
            TestGameData firstAsset = CreateAsset<TestGameData>();
            firstAsset.MaxLifeCount = 3;
            DataService firstService = StartService(firstAsset);
            firstService.Get<TestGameData>().MaxLifeCount = 9;
            firstService.SaveAll();
            DestroyService();

            TestGameData secondAsset = CreateAsset<TestGameData>();
            secondAsset.MaxLifeCount = 3;
            DataService secondService = StartService(secondAsset);

            Assert.AreEqual(9, secondService.Get<TestGameData>().MaxLifeCount);
        }

        [Test]
        public void Start_WithSavedTuningAndChangedDefault_UsesTheNewDefaultAndDropsTheFile()
        {
            TestGameData firstAsset = CreateAsset<TestGameData>();
            firstAsset.MaxLifeCount = 3;
            DataService firstService = StartService(firstAsset);
            firstService.Get<TestGameData>().MaxLifeCount = 9;
            firstService.SaveAll();
            DestroyService();

            TestGameData secondAsset = CreateAsset<TestGameData>();
            secondAsset.MaxLifeCount = 5;
            DataService secondService = StartService(secondAsset);

            Assert.AreEqual(5, secondService.Get<TestGameData>().MaxLifeCount,
                "The saved tuning value won over the changed default.");
            Assert.IsFalse(File.Exists(GetFilePath(nameof(TestGameData))));
        }

        [Test]
        public void SaveAll_InAReleaseBuild_WritesNoTuningFile()
        {
            DataService service = StartService(isDevelopmentBuild: false, CreateAsset<TestGameData>());
            service.Get<TestGameData>().MaxLifeCount = 9;

            service.SaveAll();

            Assert.IsFalse(File.Exists(GetFilePath(nameof(TestGameData))));
        }

        [Test]
        public void Start_InAReleaseBuild_IgnoresATuningFile()
        {
            TestGameData firstAsset = CreateAsset<TestGameData>();
            DataService firstService = StartService(firstAsset);
            firstService.Get<TestGameData>().MaxLifeCount = 9;
            firstService.SaveAll();
            DestroyService();

            DataService secondService = StartService(isDevelopmentBuild: false, CreateAsset<TestGameData>());

            Assert.AreEqual(0, secondService.Get<TestGameData>().MaxLifeCount);
        }

        [Test]
        public void Start_WithCutOffFileAndNoBackup_UsesTheDefaultsWithAWarning()
        {
            WriteFile(GetFilePath(nameof(TestPlayerData)), _cutOffFileText);
            LogAssert.Expect(LogType.Warning, new Regex("TestPlayerData.*defaults are used"));

            TestPlayerData asset = CreateAsset<TestPlayerData>();
            asset.Level = 2;
            DataService service = StartService(asset);

            Assert.AreEqual(2, service.Get<TestPlayerData>().Level);
        }

        [Test]
        public void Start_WithCutOffFileAndABackup_LoadsTheBackupWithAWarning()
        {
            DataService firstService = StartService(CreateAsset<TestPlayerData>());
            firstService.Get<TestPlayerData>().Level = 6;
            firstService.SaveAll();
            firstService.Get<TestPlayerData>().Level = 7;
            firstService.SaveAll();
            DestroyService();

            WriteFile(GetFilePath(nameof(TestPlayerData)), _cutOffFileText);
            LogAssert.Expect(LogType.Warning, new Regex("TestPlayerData.*backup was loaded"));

            DataService secondService = StartService(CreateAsset<TestPlayerData>());

            Assert.AreEqual(6, secondService.Get<TestPlayerData>().Level);
        }

        [Test]
        public void Start_WithOneCutOffFile_StillLoadsTheOthers()
        {
            DataService firstService = StartService(CreateAsset<TestPlayerData>(), CreateAsset<TestVersionedData>());
            firstService.Get<TestVersionedData>().Score = 3;
            firstService.SaveAll();
            DestroyService();

            WriteFile(GetFilePath(nameof(TestPlayerData)), _cutOffFileText);
            LogAssert.Expect(LogType.Warning, new Regex("TestPlayerData"));

            DataService secondService =
                StartService(CreateAsset<TestPlayerData>(), CreateAsset<TestVersionedData>());

            Assert.AreEqual(3, secondService.Get<TestVersionedData>().Score,
                "A damaged first file stopped the files after it from loading.");
        }

        [Test]
        public void Save_WritesNoOptionSettingsIntoTheFile_AndKeepsThemOnTheContainer()
        {
            TestPlayerData asset = CreateAsset<TestPlayerData>();
            asset.SRDataOptionList.Add(new SRDataOptionEntry { PropertyName = "Level" });
            DataService service = StartService(asset);

            service.Save<TestPlayerData>();

            string fileText = File.ReadAllText(GetFilePath(nameof(TestPlayerData)));
            StringAssert.DoesNotContain("_srDataOptions", fileText);
            StringAssert.Contains("\"_level\":0", fileText);
            Assert.AreEqual(1, service.Get<TestPlayerData>().SRDataOptions.Count);
        }

        [Test]
        public void Save_WithoutAFile_WritesOne()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());

            service.Save<TestPlayerData>();

            Assert.IsTrue(File.Exists(GetFilePath(nameof(TestPlayerData))));
        }

        [Test]
        public void Save_WhenNothingChanged_DoesNotWriteAgain()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());
            service.Save<TestPlayerData>();

            service.Save<TestPlayerData>();

            Assert.IsFalse(File.Exists(GetBackupPath(nameof(TestPlayerData))),
                "A second write happened: the first file became a backup.");
        }

        [Test]
        public void Save_AfterAListChanged_WritesAgain()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());
            service.Save<TestPlayerData>();

            service.Get<TestPlayerData>().Items.Add(12);
            service.Save<TestPlayerData>();

            Assert.IsTrue(File.Exists(GetBackupPath(nameof(TestPlayerData))));
            StringAssert.Contains("\"_items\":[12]", File.ReadAllText(GetFilePath(nameof(TestPlayerData))));
        }

        [Test]
        public void Pause_SavesChangedContainers()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());
            service.Get<TestPlayerData>().Level = 5;

            service.SendMessage("OnApplicationPause", true);

            StringAssert.Contains("\"_level\":5", File.ReadAllText(GetFilePath(nameof(TestPlayerData))));
        }

        [Test]
        public void Pause_WithSaveOnPauseOff_SavesNothing()
        {
            DataService service = StartService(isDevelopmentBuild: true, saveOnPause: false,
                CreateAsset<TestPlayerData>());
            service.Get<TestPlayerData>().Level = 5;

            service.SendMessage("OnApplicationPause", true);

            Assert.IsFalse(File.Exists(GetFilePath(nameof(TestPlayerData))));
        }

        [Test]
        public void Destroy_SavesChangedContainersAndDeregisters()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());
            service.Get<TestPlayerData>().Level = 5;

            DestroyService();

            StringAssert.Contains("\"_level\":5", File.ReadAllText(GetFilePath(nameof(TestPlayerData))));
            Assert.IsFalse(ServiceLocator.IsRegistered<IDataService>());
        }

        [Test]
        public void Reset_PutsTheDefaultsBackIntoTheSameObjectAndSaves()
        {
            TestPlayerData asset = CreateAsset<TestPlayerData>();
            asset.Level = 1;
            DataService service = StartService(asset);
            TestPlayerData playerData = service.Get<TestPlayerData>();
            playerData.Level = 8;
            playerData.Items.Add(3);
            service.Save<TestPlayerData>();

            service.Reset<TestPlayerData>();

            Assert.AreSame(playerData, service.Get<TestPlayerData>());
            Assert.AreEqual(1, playerData.Level);
            Assert.AreEqual(0, playerData.Items.Count);
            StringAssert.Contains("\"_level\":1", File.ReadAllText(GetFilePath(nameof(TestPlayerData))));
        }

        [Test]
        public void Reset_OfTuning_DeletesItsFile()
        {
            DataService service = StartService(CreateAsset<TestGameData>());
            service.Get<TestGameData>().MaxLifeCount = 9;
            service.SaveAll();
            Assert.IsTrue(File.Exists(GetFilePath(nameof(TestGameData))));

            service.Reset<TestGameData>();

            Assert.AreEqual(0, service.Get<TestGameData>().MaxLifeCount);
            Assert.IsFalse(File.Exists(GetFilePath(nameof(TestGameData))));
        }

        [Test]
        public void Start_WithASaveOfAnOlderVersion_CallsMigrateAndWritesTheNewVersion()
        {
            var storage = new DataStorage(_folderPath, _obfuscationKey);
            const string savedJson = "{\"_score\":4,\"_oldField\":11}";
            storage.Write(nameof(TestVersionedData), obfuscate: false, new DataFileContent(1, null, savedJson));

            DataService service = StartService(CreateAsset<TestVersionedData>());
            TestVersionedData versionedData = service.Get<TestVersionedData>();

            Assert.AreEqual(4, versionedData.Score);
            Assert.AreEqual(1, versionedData.MigratedFromVersion);
            Assert.AreEqual(savedJson, versionedData.MigratedJson);

            service.SaveAll();

            storage.Read(nameof(TestVersionedData), DataFileKind.Current, obfuscate: false,
                out DataFileContent content);
            Assert.AreEqual(TestVersionedData.CurrentDataVersion, content.Version);
        }

        [Test]
        public void Start_WithASaveOfTheCurrentVersion_DoesNotCallMigrate()
        {
            DataService firstService = StartService(CreateAsset<TestVersionedData>());
            firstService.Get<TestVersionedData>().Score = 4;
            firstService.SaveAll();
            DestroyService();

            DataService secondService = StartService(CreateAsset<TestVersionedData>());

            Assert.AreEqual(-1, secondService.Get<TestVersionedData>().MigratedFromVersion);
        }

        [Test]
        public void Save_OfAnObfuscatedContainer_IsNotReadableAsText_AndLoadsAgain()
        {
            DataService firstService = StartService(CreateAsset<TestSecretData>());
            firstService.Get<TestSecretData>().Coins = 321;
            firstService.SaveAll();
            DestroyService();

            StringAssert.DoesNotContain("_coins", File.ReadAllText(GetFilePath("Secret")));

            DataService secondService = StartService(CreateAsset<TestSecretData>());

            Assert.AreEqual(321, secondService.Get<TestSecretData>().Coins);
        }

        [Test]
        public void Start_WithAClassWithoutTheAttribute_LogsAnErrorAndLoadsTheRest()
        {
            LogAssert.Expect(LogType.Error, new Regex("TestUnmarkedData has no \\[DataFile\\] attribute"));

            DataService service = StartService(CreateAsset<TestUnmarkedData>(), CreateAsset<TestPlayerData>());

            Assert.IsFalse(service.TryGet(out TestUnmarkedData _));
            Assert.IsTrue(service.TryGet(out TestPlayerData _));
        }

        [Test]
        public void Start_WithAnEmptyEntry_LogsAnErrorAndLoadsTheRest()
        {
            LogAssert.Expect(LogType.Error, new Regex("entry 0 of the container list is empty"));

            DataService service = StartService(null, CreateAsset<TestPlayerData>());

            Assert.IsTrue(service.TryGet(out TestPlayerData _));
        }

        [Test]
        public void Start_WithTheSameClassTwice_LogsAnErrorAndKeepsTheFirst()
        {
            TestPlayerData firstAsset = CreateAsset<TestPlayerData>();
            firstAsset.Level = 1;
            TestPlayerData secondAsset = CreateAsset<TestPlayerData>();
            secondAsset.Level = 2;
            LogAssert.Expect(LogType.Error, new Regex("TestPlayerData is in the container list twice"));

            DataService service = StartService(firstAsset, secondAsset);

            Assert.AreEqual(1, service.Get<TestPlayerData>().Level);
        }

        [Test]
        public void Get_OfAClassThatIsNotInTheList_LogsAnErrorAndReturnsNull()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());
            LogAssert.Expect(LogType.Error, new Regex("TestGameData is not in the container list"));

            Assert.IsNull(service.Get<TestGameData>());
        }

        [Test]
        public void TryGet_OfAClassThatIsNotInTheList_ReturnsFalseQuietly()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());

            Assert.IsFalse(service.TryGet(out TestGameData gameData));
            Assert.IsNull(gameData);
        }

        [Test]
        public void GetContainers_FillsTheListWithTheRuntimeCopies()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>(), CreateAsset<TestGameData>());
            var results = new List<DataContainer> { null };

            service.GetContainers(results);

            Assert.AreEqual(2, results.Count);
            Assert.AreSame(service.Get<TestPlayerData>(), results[0]);
            Assert.AreSame(service.Get<TestGameData>(), results[1]);
        }

        [Test]
        public void JsonUtility_IgnoresFormerlySerializedAs()
        {
            // Migrate is handed the raw JSON because of this. If this test ever fails, the README's note on
            // renamed fields is out of date.
            TestRenamedFieldData renamedFieldData = CreateAsset<TestRenamedFieldData>();

            JsonUtility.FromJsonOverwrite("{\"_oldName\":5}", renamedFieldData);

            Assert.AreEqual(0, renamedFieldData.NewName);
        }

        [Test]
        public void Start_WithTwoClassesOnOneFileName_LogsAnErrorAndKeepsTheFirst()
        {
            LogAssert.Expect(LogType.Error, new Regex("TestSharedFileData uses the file name"));

            DataService service = StartService(CreateAsset<TestPlayerData>(), CreateAsset<TestSharedFileData>());

            Assert.IsTrue(service.TryGet(out TestPlayerData _));
            Assert.IsFalse(service.TryGet(out TestSharedFileData _));
        }

        [Test]
        public void Start_WithAnInvalidFileName_LogsAnErrorAndLoadsTheRest()
        {
            LogAssert.Expect(LogType.Error, new Regex("TestBadFileNameData has the file name"));

            DataService service = StartService(CreateAsset<TestBadFileNameData>(), CreateAsset<TestPlayerData>());

            Assert.IsFalse(service.TryGet(out TestBadFileNameData _));
            Assert.IsTrue(service.TryGet(out TestPlayerData _));
            Assert.IsTrue(ServiceLocator.IsRegistered<IDataService>());
        }

        [Test]
        public void Save_AfterLoadingASaveOfANewerVersion_KeepsTheNewerVersionNumber()
        {
            var storage = new DataStorage(_folderPath, _obfuscationKey);
            const int newerVersion = TestVersionedData.CurrentDataVersion + 6;
            storage.Write(nameof(TestVersionedData), obfuscate: false,
                new DataFileContent(newerVersion, null, "{\"_score\":4}"));

            DataService service = StartService(CreateAsset<TestVersionedData>());
            service.Get<TestVersionedData>().Score = 5;
            service.SaveAll();

            storage.Read(nameof(TestVersionedData), DataFileKind.Current, obfuscate: false,
                out DataFileContent content);
            Assert.AreEqual(newerVersion, content.Version);
            Assert.AreEqual(-1, service.Get<TestVersionedData>().MigratedFromVersion);
        }

        [Test]
        public void Start_WhenMigrateThrows_LogsItAndStillLoadsAndRegisters()
        {
            var storage = new DataStorage(_folderPath, _obfuscationKey);
            storage.Write(nameof(TestThrowingData), obfuscate: false, new DataFileContent(1, null, "{\"_score\":4}"));
            LogAssert.Expect(LogType.Error, new Regex("TestThrowingData.Migrate threw"));
            LogAssert.Expect(LogType.Exception, new Regex("Migrate failed on purpose"));

            DataService service = StartService(CreateAsset<TestThrowingData>(), CreateAsset<TestPlayerData>());

            Assert.AreEqual(4, service.Get<TestThrowingData>().Score);
            Assert.IsTrue(service.TryGet(out TestPlayerData _));
            Assert.IsTrue(ServiceLocator.IsRegistered<IDataService>());
        }

        [Test]
        public void Start_OfASecondService_LogsAnErrorAndLoadsAndSavesNothing()
        {
            DataService firstService = StartService(CreateAsset<TestPlayerData>());
            LogAssert.Expect(LogType.Error, new Regex("another data service is already registered"));

            var secondObject = new GameObject("Second DataService");
            _createdObjects.Add(secondObject);
            secondObject.SetActive(false);
            DataService secondService = secondObject.AddComponent<DataService>();
            secondService.ContainerAssets.Add(CreateAsset<TestPlayerData>());
            secondService.FolderPath = _folderPath;
            secondObject.SetActive(true);

            Assert.IsFalse(secondService.TryGet(out TestPlayerData _));

            Object.DestroyImmediate(secondObject);

            Assert.IsFalse(File.Exists(GetFilePath(nameof(TestPlayerData))),
                "The second service wrote a file of defaults.");
            Assert.IsTrue(ServiceLocator.TryGet(out IDataService registeredService));
            Assert.AreSame(firstService, registeredService);
        }

        [Test]
        public void Quit_SavesChangedContainers()
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());
            service.Get<TestPlayerData>().Level = 5;

            service.SendMessage("OnApplicationQuit");

            StringAssert.Contains("\"_level\":5", File.ReadAllText(GetFilePath(nameof(TestPlayerData))));
        }

        [Test]
        public void Save_AfterLoadingTheBackup_KeepsTheBackupAndSetsTheUnreadableFileAside()
        {
            DataService firstService = StartService(CreateAsset<TestPlayerData>());
            firstService.Get<TestPlayerData>().Level = 6;
            firstService.SaveAll();
            firstService.Get<TestPlayerData>().Level = 7;
            firstService.SaveAll();
            DestroyService();

            WriteFile(GetFilePath(nameof(TestPlayerData)), _cutOffFileText);
            LogAssert.Expect(LogType.Warning, new Regex("backup was loaded"));

            DataService secondService = StartService(CreateAsset<TestPlayerData>());
            secondService.SaveAll();

            StringAssert.Contains("\"_level\":6", File.ReadAllText(GetFilePath(nameof(TestPlayerData))));
            StringAssert.Contains("\"_level\":6", File.ReadAllText(GetBackupPath(nameof(TestPlayerData))),
                "The unreadable file replaced the good backup.");
            Assert.AreEqual(_cutOffFileText,
                File.ReadAllText(Path.Combine(_folderPath, nameof(TestPlayerData) + ".corrupt")));
        }

        [Test]
        public void Save_OfAContainerWithoutFields_WritesEmptyJson()
        {
            TestEmptyData asset = CreateAsset<TestEmptyData>();
            asset.SRDataOptionList.Add(new SRDataOptionEntry { PropertyName = "Anything" });
            DataService service = StartService(asset);

            service.Save<TestEmptyData>();

            string[] lines = File.ReadAllText(GetFilePath(nameof(TestEmptyData))).Split('\n');
            Assert.AreEqual("{}", lines[1]);
        }

        private void SaveLevel(int level)
        {
            DataService service = StartService(CreateAsset<TestPlayerData>());
            service.Get<TestPlayerData>().Level = level;
            service.SaveAll();
            DestroyService();
        }

        private DataService StartService(params DataContainer[] containers)
        {
            return StartService(isDevelopmentBuild: true, saveOnPause: true, containers);
        }

        private DataService StartService(bool isDevelopmentBuild, params DataContainer[] containers)
        {
            return StartService(isDevelopmentBuild, saveOnPause: true, containers);
        }

        private DataService StartService(bool isDevelopmentBuild, bool saveOnPause,
            params DataContainer[] containers)
        {
            _serviceObject = new GameObject("DataService");
            _serviceObject.SetActive(false);

            DataService service = _serviceObject.AddComponent<DataService>();
            service.ContainerAssets.AddRange(containers);
            service.ObfuscationKey = _obfuscationKey;
            service.FolderPath = _folderPath;
            service.IsDevelopmentBuild = isDevelopmentBuild;
            service.SaveOnPause = saveOnPause;

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

        private TContainer CreateAsset<TContainer>() where TContainer : DataContainer
        {
            TContainer asset = ScriptableObject.CreateInstance<TContainer>();
            asset.name = typeof(TContainer).Name;
            _createdObjects.Add(asset);

            return asset;
        }

        private string GetFilePath(string fileName) => Path.Combine(_folderPath, fileName + ".dat");

        private string GetBackupPath(string fileName) => Path.Combine(_folderPath, fileName + ".bak");

        private void WriteFile(string path, string text)
        {
            Directory.CreateDirectory(_folderPath);
            File.WriteAllText(path, text);
        }

        #endregion
    }
}

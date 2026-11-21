using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AngryKoala.Data.Tests
{
    public sealed class DataStorageTests
    {
        private string _folderPath;

        private const string _fileName = "Player";
        private const string _json = "{\"_level\":7}";

        [SetUp]
        public void SetUp()
        {
            _folderPath = Path.Combine(Application.temporaryCachePath, "AngryKoalaDataTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folderPath))
            {
                Directory.Delete(_folderPath, recursive: true);
            }
        }

        [Test]
        public void Read_WithoutAFile_ReturnsMissing()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);

            DataReadResult result = storage.Read(_fileName, DataFileKind.Current, obfuscate: false, out _);

            Assert.AreEqual(DataReadResult.Missing, result);
        }

        [Test]
        public void Write_ThenRead_GivesBackVersionChecksumAndJson()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);

            storage.Write(_fileName, obfuscate: false, new DataFileContent(4, "abc", _json));
            DataReadResult result =
                storage.Read(_fileName, DataFileKind.Current, obfuscate: false, out DataFileContent content);

            Assert.AreEqual(DataReadResult.Read, result);
            Assert.AreEqual(4, content.Version);
            Assert.AreEqual("abc", content.DefaultsChecksum);
            Assert.AreEqual(_json, content.Json);
        }

        [Test]
        public void Write_LeavesOnlyTheCurrentFile_TheFirstTime()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);

            storage.Write(_fileName, obfuscate: false, new DataFileContent(1, null, _json));

            string[] fileNames = Directory.GetFiles(_folderPath).Select(Path.GetFileName).ToArray();
            CollectionAssert.AreEqual(new[] { _fileName + ".dat" }, fileNames);
        }

        [Test]
        public void Write_Twice_KeepsThePreviousFileAsTheBackup()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);

            storage.Write(_fileName, obfuscate: false, new DataFileContent(1, null, "{\"_level\":1}"));
            storage.Write(_fileName, obfuscate: false, new DataFileContent(1, null, "{\"_level\":2}"));

            storage.Read(_fileName, DataFileKind.Current, obfuscate: false, out DataFileContent current);
            storage.Read(_fileName, DataFileKind.Backup, obfuscate: false, out DataFileContent backup);

            Assert.AreEqual("{\"_level\":2}", current.Json);
            Assert.AreEqual("{\"_level\":1}", backup.Json);
        }

        [Test]
        public void Read_OfAFileWithoutAHeaderLine_ReturnsUnreadable()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);
            Directory.CreateDirectory(_folderPath);
            File.WriteAllText(storage.GetPath(_fileName, DataFileKind.Current), _json);

            DataReadResult result = storage.Read(_fileName, DataFileKind.Current, obfuscate: false, out _);

            Assert.AreEqual(DataReadResult.Unreadable, result);
        }

        [Test]
        public void Read_OfAnEmptyFile_ReturnsUnreadable()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);
            Directory.CreateDirectory(_folderPath);
            File.WriteAllBytes(storage.GetPath(_fileName, DataFileKind.Current), Array.Empty<byte>());

            DataReadResult result = storage.Read(_fileName, DataFileKind.Current, obfuscate: false, out _);

            Assert.AreEqual(DataReadResult.Unreadable, result);
        }

        [Test]
        public void Write_Obfuscated_IsNotReadableAsText_AndReadsBackWithTheKey()
        {
            var storage = new DataStorage(_folderPath, "key");

            storage.Write(_fileName, obfuscate: true, new DataFileContent(1, null, _json));

            string fileText = File.ReadAllText(storage.GetPath(_fileName, DataFileKind.Current));
            StringAssert.DoesNotContain("_level", fileText);

            DataReadResult result =
                storage.Read(_fileName, DataFileKind.Current, obfuscate: true, out DataFileContent content);
            Assert.AreEqual(DataReadResult.Read, result);
            Assert.AreEqual(_json, content.Json);
        }

        [Test]
        public void Write_Obfuscated_GivesDifferentBytesForTheSameContent()
        {
            var storage = new DataStorage(_folderPath, "key");
            string path = storage.GetPath(_fileName, DataFileKind.Current);

            storage.Write(_fileName, obfuscate: true, new DataFileContent(1, null, _json));
            byte[] firstBytes = File.ReadAllBytes(path);
            storage.Write(_fileName, obfuscate: true, new DataFileContent(1, null, _json));
            byte[] secondBytes = File.ReadAllBytes(path);

            CollectionAssert.AreNotEqual(firstBytes, secondBytes);
        }

        [Test]
        public void Read_Obfuscated_WithAnotherKey_ReturnsUnreadable()
        {
            new DataStorage(_folderPath, "key").Write(_fileName, obfuscate: true,
                new DataFileContent(1, null, _json));

            DataReadResult result = new DataStorage(_folderPath, "another key").Read(_fileName,
                DataFileKind.Current, obfuscate: true, out _);

            Assert.AreEqual(DataReadResult.Unreadable, result);
        }

        [Test]
        public void Read_Obfuscated_OfACutOffFile_ReturnsUnreadable()
        {
            var storage = new DataStorage(_folderPath, "key");
            string path = storage.GetPath(_fileName, DataFileKind.Current);
            storage.Write(_fileName, obfuscate: true, new DataFileContent(1, null, _json));
            byte[] bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(path, bytes.Take(bytes.Length - 5).ToArray());

            DataReadResult result = storage.Read(_fileName, DataFileKind.Current, obfuscate: true, out _);

            Assert.AreEqual(DataReadResult.Unreadable, result);
        }

        [Test]
        public void Delete_RemovesTheFileAndItsBackup()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);
            storage.Write(_fileName, obfuscate: false, new DataFileContent(1, null, _json));
            storage.Write(_fileName, obfuscate: false, new DataFileContent(1, null, _json));

            storage.Delete(_fileName);

            Assert.IsEmpty(Directory.GetFiles(_folderPath));
        }

        [Test]
        public void Delete_WithoutAFolder_DoesNotThrow()
        {
            var storage = new DataStorage(_folderPath, obfuscationKey: null);

            Assert.DoesNotThrow(() => storage.Delete(_fileName));
        }
    }
}

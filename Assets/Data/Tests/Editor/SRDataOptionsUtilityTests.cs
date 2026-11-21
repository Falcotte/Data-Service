using NUnit.Framework;
using UnityEngine;

namespace AngryKoala.Data.Tests
{
    public sealed class SRDataOptionsUtilityTests
    {
        private TestPlayerData _playerData;

        [SetUp]
        public void SetUp()
        {
            _playerData = ScriptableObject.CreateInstance<TestPlayerData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerData);
        }

        [Test]
        public void SyncOptions_AddsOneEntryPerSupportedField_WithNamesAndOrder()
        {
            SRDataOptionsUtility.SyncOptions(_playerData);

            // Level and PlayerName are supported; the Items list is not.
            Assert.AreEqual(2, _playerData.SRDataOptions.Count);
            Assert.AreEqual("Level", _playerData.SRDataOptions[0].PropertyName);
            Assert.AreEqual("Test Player Data", _playerData.SRDataOptions[0].Settings.Category);
            Assert.AreEqual("Player Name", _playerData.SRDataOptions[1].Settings.DisplayName);
            Assert.AreEqual(1, _playerData.SRDataOptions[1].Settings.Sort);
        }

        [Test]
        public void SyncOptions_KeepsWhatWasSetAndRemovesEntriesOfFieldsThatAreGone()
        {
            SRDataOptionsUtility.SyncOptions(_playerData);
            _playerData.SRDataOptionList[0].Settings.Enabled = true;
            _playerData.SRDataOptionList[0].Settings.Sort = 0;
            _playerData.SRDataOptionList[1].Settings.Sort = 0;
            _playerData.SRDataOptionList.Add(new SRDataOptionEntry { PropertyName = "Gone" });

            SRDataOptionsUtility.SyncOptions(_playerData);

            Assert.AreEqual(2, _playerData.SRDataOptions.Count);
            Assert.IsTrue(_playerData.SRDataOptions[0].Settings.Enabled);
            Assert.AreEqual(0, _playerData.SRDataOptions[1].Settings.Sort, "A sort value set by hand was changed.");
        }

        [Test]
        public void ResetSortOrder_PutsTheEntriesBackInFieldOrder()
        {
            SRDataOptionsUtility.SyncOptions(_playerData);
            _playerData.SRDataOptionList[0].Settings.Sort = 40;
            _playerData.SRDataOptionList[1].Settings.Sort = 30;

            SRDataOptionsUtility.ResetSortOrder(_playerData);

            Assert.AreEqual(0, _playerData.SRDataOptions[0].Settings.Sort);
            Assert.AreEqual(1, _playerData.SRDataOptions[1].Settings.Sort);
        }

        [Test]
        public void SyncOptions_WithNull_DoesNothing()
        {
            Assert.DoesNotThrow(() => SRDataOptionsUtility.SyncOptions(null));
        }
    }
}

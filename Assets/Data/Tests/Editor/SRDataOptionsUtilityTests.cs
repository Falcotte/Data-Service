using NUnit.Framework;
using UnityEngine;

namespace AngryKoala.Data.Tests
{
    public sealed class SRDataOptionsUtilityTests
    {
        [Test]
        public void ResetSortOrderForData_PutsTheEntriesBackInFieldOrder()
        {
            PlayerData playerData = ScriptableObject.CreateInstance<PlayerData>();
            SRDataOptionsUtility.SyncOptionsForDataObject(playerData, playerData.SRDataOptions);
            Assert.AreEqual(2, playerData.SRDataOptions.Count, "Expected one entry per field: Level and Score.");

            playerData.SRDataOptions[0].Settings.Sort = 40;
            playerData.SRDataOptions[1].Settings.Sort = 30;

            SRDataOptionsUtility.ResetSortOrderForData(playerData);

            Assert.AreEqual(0, playerData.SRDataOptions[0].Settings.Sort);
            Assert.AreEqual(1, playerData.SRDataOptions[1].Settings.Sort);

            Object.DestroyImmediate(playerData);
        }
    }
}

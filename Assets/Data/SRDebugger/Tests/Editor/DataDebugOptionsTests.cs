using System.Collections.Generic;
using NUnit.Framework;
using SRDebugger;
using UnityEngine;

namespace AngryKoala.Data.Tests
{
    public sealed class DataDebugOptionsTests
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
        public void BuildOptions_AddsOnlyEnabledEntries_WithTheirSettings()
        {
            AddEntry("Level", isEnabled: true, category: "Player", displayName: "Current Level", sort: 3);
            AddEntry("PlayerName", isEnabled: false, category: "Player", displayName: "Name", sort: 4);
            var optionContainer = new DynamicOptionContainer();

            DataDebugOptions.BuildOptions(new List<DataContainer> { _playerData }, optionContainer);

            Assert.AreEqual(1, optionContainer.Options.Count);
            OptionDefinition option = optionContainer.Options[0];
            Assert.AreEqual("Current Level", option.Name);
            Assert.AreEqual("Player", option.Category);
            Assert.AreEqual(3, option.SortPriority);
            Assert.AreEqual(typeof(int), option.Property.PropertyType);
        }

        [Test]
        public void Option_ReadsAndWritesTheContainersField()
        {
            AddEntry("Level", isEnabled: true, category: "Player", displayName: "Level", sort: 0);
            var optionContainer = new DynamicOptionContainer();
            DataDebugOptions.BuildOptions(new List<DataContainer> { _playerData }, optionContainer);
            OptionDefinition option = optionContainer.Options[0];

            _playerData.Level = 4;
            Assert.AreEqual(4, option.Property.GetValue());

            option.Property.SetValue(9);
            Assert.AreEqual(9, _playerData.Level);

            option.Property.SetValue(12.0);
            Assert.AreEqual(12, _playerData.Level, "A value of another number type was not converted.");
        }

        [Test]
        public void Option_OfANumber_CarriesRangeAndIncrement()
        {
            SRDataOptionEntry entry =
                AddEntry("Level", isEnabled: true, category: "Player", displayName: "Level", sort: 0);
            entry.Settings.NumberRange.Enabled = true;
            entry.Settings.NumberRange.Min = 1;
            entry.Settings.NumberRange.Max = 50;
            entry.Settings.Increment = 5;
            var optionContainer = new DynamicOptionContainer();

            DataDebugOptions.BuildOptions(new List<DataContainer> { _playerData }, optionContainer);

            OptionDefinition option = optionContainer.Options[0];
            NumberRangeAttribute numberRange = option.Property.GetAttribute<NumberRangeAttribute>();
            Assert.IsNotNull(numberRange);
            Assert.AreEqual(1, numberRange.Min);
            Assert.AreEqual(50, numberRange.Max);
            Assert.AreEqual(5, option.Property.GetAttribute<IncrementAttribute>().Increment);
        }

        [Test]
        public void Option_OfAString_CarriesNoRange()
        {
            SRDataOptionEntry entry =
                AddEntry("PlayerName", isEnabled: true, category: "Player", displayName: "Name", sort: 0);
            entry.Settings.NumberRange.Enabled = true;
            var optionContainer = new DynamicOptionContainer();

            DataDebugOptions.BuildOptions(new List<DataContainer> { _playerData }, optionContainer);

            Assert.IsNull(optionContainer.Options[0].Property.GetAttribute<NumberRangeAttribute>());
        }

        [Test]
        public void BuildOptions_SkipsAnEntryWhoseFieldIsGone()
        {
            AddEntry("Removed", isEnabled: true, category: "Player", displayName: "Removed", sort: 0);
            var optionContainer = new DynamicOptionContainer();

            DataDebugOptions.BuildOptions(new List<DataContainer> { _playerData }, optionContainer);

            Assert.AreEqual(0, optionContainer.Options.Count);
        }

        private SRDataOptionEntry AddEntry(string propertyName, bool isEnabled, string category,
            string displayName, int sort)
        {
            var entry = new SRDataOptionEntry
            {
                PropertyName = propertyName
            };

            entry.Settings.Enabled = isEnabled;
            entry.Settings.Category = category;
            entry.Settings.DisplayName = displayName;
            entry.Settings.Sort = sort;

            _playerData.SRDataOptionList.Add(entry);

            return entry;
        }
    }
}

using System.Collections.Generic;
using AngryKoala.Data.Tests.Sample;
using UnityEngine;

#pragma warning disable CS0169, CS0649

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestGeneratedData : DataContainer
    {
        public enum Flavor
        {
            Sweet,
            Sour
        }

        [SerializeField] private int _level;

        [SerializeField] private List<string> _names;

        [SerializeField] private TestGeneratedMode _mode;

        [SerializeField] [HideInInspector] private int _hiddenValue;
    }
}

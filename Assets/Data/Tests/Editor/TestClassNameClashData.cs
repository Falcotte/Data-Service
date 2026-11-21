using UnityEngine;

#pragma warning disable CS0169, CS0649

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestClassNameClashData : DataContainer
    {
        [SerializeField] private int _testClassNameClashData;

        [SerializeField] private int _other;
    }
}

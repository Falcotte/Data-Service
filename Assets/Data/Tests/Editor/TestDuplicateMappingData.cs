using UnityEngine;

#pragma warning disable CS0169, CS0649

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestDuplicateMappingData : DataContainer
    {
        [SerializeField] private int _value;

        [SerializeField] private int value;

        [SerializeField] private int _other;
    }
}

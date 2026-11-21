using UnityEngine;

#pragma warning disable CS0169, CS0649

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestInvalidIdentifierData : DataContainer
    {
        [SerializeField] private int _2nd;

        [SerializeField] private int _other;
    }
}

using UnityEngine;

#pragma warning disable CS0169, CS0649

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestHandWrittenData : DataContainer
    {
        [SerializeField] private int _speed;

        [SerializeField] private int _level;

        public int Speed => _speed;
    }
}

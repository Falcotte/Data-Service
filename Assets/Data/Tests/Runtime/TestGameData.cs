using UnityEngine;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.BuildOnly)]
    public sealed class TestGameData : DataContainer
    {
        [SerializeField] private int _maxLifeCount;

        public int MaxLifeCount
        {
            get => _maxLifeCount;
            set => _maxLifeCount = value;
        }
    }
}

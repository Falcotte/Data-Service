using UnityEngine;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved, FileName = "bad/name")]
    public sealed class TestBadFileNameData : DataContainer
    {
        [SerializeField] private int _value;
        public int Value => _value;
    }
}

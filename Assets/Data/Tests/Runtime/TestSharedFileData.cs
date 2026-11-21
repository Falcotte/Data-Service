using UnityEngine;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved, FileName = "testplayerdata")]
    public sealed class TestSharedFileData : DataContainer
    {
        [SerializeField] private int _value;
        public int Value => _value;
    }
}

using UnityEngine;
using UnityEngine.Serialization;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestRenamedFieldData : DataContainer
    {
        [SerializeField] [FormerlySerializedAs("_oldName")] private int _newName;
        public int NewName => _newName;
    }
}

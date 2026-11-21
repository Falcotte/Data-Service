using UnityEngine;

#pragma warning disable CS0169, CS0649

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestInheritedClashData : DataContainer
    {
        [SerializeField] private int _version;

        [SerializeField] private string _name;
    }
}

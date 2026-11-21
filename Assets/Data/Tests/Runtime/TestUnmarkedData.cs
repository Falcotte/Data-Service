using UnityEngine;

namespace AngryKoala.Data.Tests
{
    public sealed class TestUnmarkedData : DataContainer
    {
        [SerializeField] private int _value;
        public int Value => _value;
    }
}

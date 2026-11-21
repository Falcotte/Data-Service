using System;
using UnityEngine;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestThrowingData : DataContainer
    {
        [SerializeField] private int _score;
        public int Score => _score;

        protected override int Version => 2;

        protected override void Migrate(int savedVersion, string savedJson)
        {
            throw new InvalidOperationException("Migrate failed on purpose.");
        }
    }
}

using UnityEngine;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestVersionedData : DataContainer
    {
        [SerializeField] private int _score;

        public int Score
        {
            get => _score;
            set => _score = value;
        }

        public int MigratedFromVersion { get; private set; } = -1;

        public string MigratedJson { get; private set; }

        public const int CurrentDataVersion = 3;

        protected override int Version => CurrentDataVersion;

        protected override void Migrate(int savedVersion, string savedJson)
        {
            MigratedFromVersion = savedVersion;
            MigratedJson = savedJson;
        }
    }
}

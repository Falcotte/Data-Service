using UnityEngine;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved, FileName = "Secret", Obfuscate = true)]
    public sealed class TestSecretData : DataContainer
    {
        [SerializeField] private int _coins;

        public int Coins
        {
            get => _coins;
            set => _coins = value;
        }
    }
}

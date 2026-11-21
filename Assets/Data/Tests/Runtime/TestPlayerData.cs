using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data.Tests
{
    [DataFile(DataPersistence.Saved)]
    public sealed class TestPlayerData : DataContainer
    {
        [SerializeField] private int _level;

        public int Level
        {
            get => _level;
            set => _level = value;
        }

        [SerializeField] private string _playerName;

        public string PlayerName
        {
            get => _playerName;
            set => _playerName = value;
        }

        [SerializeField] private List<int> _items = new();
        public List<int> Items => _items;
    }
}

using UnityEngine;

namespace AngryKoala.Data.Demo
{
    [DataFile(DataPersistence.Saved)]
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Angry Koala/Data/Demo/Player Data")]
    public sealed partial class PlayerData : DataContainer
    {
        [SerializeField] private int _level = 1;
        [SerializeField] private int _score;
        [SerializeField] private int _lives = 5;
        [SerializeField] private int _currency;
    }
}

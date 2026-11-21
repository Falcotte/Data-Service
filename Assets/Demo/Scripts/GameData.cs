using UnityEngine;

namespace AngryKoala.Data.Demo
{
    [DataFile(DataPersistence.BuildOnly)]
    [CreateAssetMenu(fileName = "GameData", menuName = "Angry Koala/Data/Demo/Game Data")]
    public sealed partial class GameData : DataContainer
    {
        [SerializeField] [Min(1)] private int _maxLifeCount = 5;

        [SerializeField] [Min(1)] [Tooltip("In minutes.")]
        private int _lifeReplenishDuration = 30;
    }
}

using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Angry Koala/Data/Game Data", order = 3)]
    public partial class GameData : ScriptableObject
    { 
        [SerializeField] private int _maxLifeCount;

        // In minutes
        [SerializeField] private int _lifeReplenishDuration;
    }
}
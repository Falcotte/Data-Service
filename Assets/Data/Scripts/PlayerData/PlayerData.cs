using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Angry Koala/Data/Player Data", order = 2)]
    public partial class PlayerData : ScriptableObject
    {
        [SerializeField] private int _level;
        
        [SerializeField] private int _score;
    }
}
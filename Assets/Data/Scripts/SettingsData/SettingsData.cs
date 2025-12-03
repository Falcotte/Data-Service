using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "SettingsData", menuName = "Angry Koala/Data/Settings Data", order = 4)]
    public partial class SettingsData : ScriptableObject
    {
        [SerializeField] private bool _musicEnabled = true;
        [SerializeField] private bool _soundEnabled = true;
        [SerializeField] private bool _vibrationEnabled = true;
    }
}
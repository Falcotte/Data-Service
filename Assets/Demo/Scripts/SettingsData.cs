using UnityEngine;

namespace AngryKoala.Data.Demo
{
    [DataFile(DataPersistence.Saved)]
    [CreateAssetMenu(fileName = "SettingsData", menuName = "Angry Koala/Data/Demo/Settings Data")]
    public sealed partial class SettingsData : DataContainer
    {
        [SerializeField] private bool _musicEnabled = true;
        [SerializeField] private bool _soundEnabled = true;
        [SerializeField] private bool _vibrationEnabled = true;
    }
}

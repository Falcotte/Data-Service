using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "SettingsData", menuName = "Angry Koala/Data/Settings Data", order = 4)]
    public partial class SettingsData : ScriptableObject
    {
        [SerializeField] private bool _musicEnabled = true;
        [SerializeField] private bool _soundEnabled = true;
        [SerializeField] private bool _vibrationEnabled = true;

        [SerializeField] private List<SRDebuggerOptionEntry> _srDebuggerOptions = new();

        public List<SRDebuggerOptionEntry> SrDebuggerOptions => _srDebuggerOptions;

#if UNITY_EDITOR
        private void OnValidate()
        {
            SRDataOptionsUtility.SyncOptionsForDataObject(this, _srDebuggerOptions);
        }
#endif
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "SettingsData", menuName = "Angry Koala/Data/Settings Data", order = 4)]
    public partial class SettingsData : ScriptableObject
    {
        [SerializeField] [HideInInspector] private List<SRDataOptionEntry> _srDataOptions = new();

        public List<SRDataOptionEntry> SRDataOptions => _srDataOptions;

#if UNITY_EDITOR
        private void OnValidate()
        {
            SRDataOptionsUtility.SyncOptionsForDataObject(this, _srDataOptions);
        }
#endif
    }
}
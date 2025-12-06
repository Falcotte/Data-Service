using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Angry Koala/Data/Player Data", order = 2)]
    public partial class PlayerData : ScriptableObject
    {
        [SerializeField] private int _level;

        [SerializeField] private int _score;

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
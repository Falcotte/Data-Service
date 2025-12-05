using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Angry Koala/Data/Player Data", order = 2)]
    public partial class PlayerData : ScriptableObject
    {
        [SerializeField] private int _level;

        [SerializeField] private int _score;

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
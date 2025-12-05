using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Angry Koala/Data/Game Data", order = 3)]
    public partial class GameData : ScriptableObject
    {
        [SerializeField] private int _maxLifeCount;

        // In minutes
        [SerializeField] private int _lifeReplenishDuration;

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
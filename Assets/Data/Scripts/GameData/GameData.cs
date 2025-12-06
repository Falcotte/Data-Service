using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Angry Koala/Data/Game Data", order = 3)]
    public partial class GameData : ScriptableObject
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
#if UNITY_EDITOR && SRDEBUGGER

using UnityEditor;

namespace AngryKoala.Data
{
    [CustomEditor(typeof(PlayerData))]
    public class PlayerDataEditor : SRDataOptionsEditor<PlayerData>
    {
    }
}

#endif
#if UNITY_EDITOR && SRDEBUGGER

using UnityEditor;

namespace AngryKoala.Data
{
    [CustomEditor(typeof(GameData))]
    public class GameDataEditor : SRDataOptionsEditor<GameData>
    {
    }
}

#endif
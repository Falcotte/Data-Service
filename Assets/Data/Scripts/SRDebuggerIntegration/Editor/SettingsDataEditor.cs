#if UNITY_EDITOR && SRDEBUGGER

using UnityEditor;

namespace AngryKoala.Data
{
    [CustomEditor(typeof(SettingsData))]
    public class SettingsDataEditor : SRDataOptionsEditor<SettingsData>
    {
    }
}

#endif
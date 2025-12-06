using System.ComponentModel;
using AngryKoala.Data;
using AngryKoala.Services;
using SRDebugger;

public partial class SROptions
{
    [Category("Player Data")]
    [DisplayName("Level")]
    public int Level
    {
        get => DataService.PlayerData.Level;
        set => DataService.PlayerData.Level = value;
    }
}
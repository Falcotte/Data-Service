using AngryKoala.Services;

namespace AngryKoala.Data
{
    public interface IDataService : IService
    {
        PlayerData PlayerData { get; }
        
        GameData GameData { get; }
        
        void LoadPlayerData();
        void SavePlayerData();
        void ResetPlayerData();
        
        void LoadGameData();
        void SaveGameData();
    }
}
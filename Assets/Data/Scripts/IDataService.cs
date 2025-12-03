using AngryKoala.Services;

namespace AngryKoala.Data
{
    public interface IDataService : IService
    {
        PlayerData PlayerData { get; }
        
        void LoadPlayerData();
        void SavePlayerData();
    }
}
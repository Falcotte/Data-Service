using AngryKoala.Services;
using UnityEngine;

namespace AngryKoala.Data
{
    public class DataService : BaseService<IDataService>, IDataService
    {
        [SerializeField] private PlayerData _playerData;
    }
}
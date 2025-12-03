using System;
using System.IO;
using System.Text;
using AngryKoala.Services;
using UnityEngine;

namespace AngryKoala.Data
{
    public class DataService : BaseService<IDataService>, IDataService
    {
        [SerializeField] private PlayerData _initialPlayerData;
        
        [SerializeField] private PlayerData _playerData;

        public PlayerData PlayerData => _playerData;

        [SerializeField] private DataSerializationFormat _serializationFormat = DataSerializationFormat.Json;

        private string _dataPath => Application.persistentDataPath;
        
        private string _playerDataPath =>
            Path.Combine(Application.persistentDataPath, "PlayerData.dat");

        protected override void Awake()
        {
            base.Awake();

            if (_playerData == null)
            {
                throw new InvalidOperationException("PlayerData reference is not assigned.");
            }

            LoadPlayerData();
        }

        public void LoadPlayerData()
        {
            if (!File.Exists(_playerDataPath))
            {
                Debug.Log("Player data file not found.");

                ApplyInitialPlayerData();
                SavePlayerData();
                
                return;
            }

            byte[] bytes = File.ReadAllBytes(_playerDataPath);

            if (bytes.Length == 0)
            {
                return;
            }

            switch (_serializationFormat)
            {
                case DataSerializationFormat.Json:
                {
                    string json = Encoding.UTF8.GetString(bytes);

                    JsonUtility.FromJsonOverwrite(json, _playerData);
                    break;
                }

                case DataSerializationFormat.Binary:
                {
                    using MemoryStream memoryStream = new MemoryStream(bytes);
                    using BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);

                    string json = binaryReader.ReadString();

                    JsonUtility.FromJsonOverwrite(json, _playerData);
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            
            Debug.Log("Player data loaded.");
        }
        
        private void ApplyInitialPlayerData()
        {
            if (_initialPlayerData == null)
            {
                return;
            }

            if (_playerData == null)
            {
                Debug.LogWarning("PlayerData reference is null.");
                return;
            }

            string json = JsonUtility.ToJson(_initialPlayerData, prettyPrint: false);
            JsonUtility.FromJsonOverwrite(json, _playerData);

            Debug.Log("Initial player data applied.");
        }

        public void SavePlayerData()
        {
            if (_playerData == null)
            {
                Debug.LogWarning("PlayerData reference is null.");
                return;
            }

            string json = JsonUtility.ToJson(_playerData, prettyPrint: false);
            byte[] bytes;

            switch (_serializationFormat)
            {
                case DataSerializationFormat.Json:
                {
                    bytes = Encoding.UTF8.GetBytes(json);
                    break;
                }

                case DataSerializationFormat.Binary:
                {
                    using MemoryStream memoryStream = new MemoryStream();
                    using BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);

                    binaryWriter.Write(json);
                    binaryWriter.Flush();
                    bytes = memoryStream.ToArray();
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            string directory = Path.GetDirectoryName(_playerDataPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(_playerDataPath, bytes);
            
            Debug.Log("Player data saved.");
        }
    }
}
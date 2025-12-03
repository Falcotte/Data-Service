using System;
using System.IO;
using System.Security.Cryptography;
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

        [SerializeField] private GameData _gameData;
        
        public GameData GameData => _gameData;

        [SerializeField] private DataSerializationFormat _serializationFormat = DataSerializationFormat.Json;

        [SerializeField] private bool _useEncryption = false;

        [SerializeField] private string _encryptionPassword = "Password";

        private string _dataPath => Application.persistentDataPath;

        private string _playerDataPath =>
            Path.Combine(Application.persistentDataPath, "PlayerData.dat");
        
        private string _gameDataPath =>
            Path.Combine(Application.persistentDataPath, "GameData.dat");

        private static readonly byte[] _saltBytes = Encoding.UTF8.GetBytes("AngryKoala_Data_Salt");

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
                Debug.Log("Player data file not found. Applying initial data if available and saving.");

                ApplyInitialPlayerData();
                SavePlayerData();

                return;
            }

            if (_playerData == null)
            {
                Debug.LogWarning("PlayerData reference is null. Cannot load player data.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(_playerDataPath);

            if (bytes.Length == 0)
            {
                Debug.LogWarning("Player data file is empty.");
                return;
            }

            if (_useEncryption)
            {
                try
                {
                    bytes = Decrypt(bytes);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to decrypt player data. {exception}");
                    return;
                }
            }

            string json = DeserializeToJson(bytes);

            JsonUtility.FromJsonOverwrite(json, _playerData);

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
                Debug.LogWarning("PlayerData reference is null. Cannot save player data.");
                return;
            }

            string json = JsonUtility.ToJson(_playerData, prettyPrint: false);
            byte[] bytes = SerializeFromJson(json);

            if (_useEncryption && bytes.Length > 0)
            {
                try
                {
                    bytes = Encrypt(bytes);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to encrypt player data. {exception}");
                    return;
                }
            }

            SetDirectory(_playerDataPath);
            File.WriteAllBytes(_playerDataPath, bytes);

            Debug.Log("Player data saved.");
        }

        public void ResetPlayerData()
        {
            ApplyInitialPlayerData();
            SavePlayerData();

            Debug.Log("Player data reset to initial defaults and saved.");
        }
        
        public void LoadGameData()
        {
            if (_gameData == null)
            {
                Debug.LogWarning("GameData reference is not assigned. Cannot load game data.");
                return;
            }

            if (!File.Exists(_gameDataPath))
            {
                Debug.Log("Game data file not found. Using GameData asset values.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(_gameDataPath);

            if (bytes.Length == 0)
            {
                Debug.LogWarning("Game data file is empty.");
                return;
            }

            if (_useEncryption)
            {
                try
                {
                    bytes = Decrypt(bytes);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to decrypt game data. {exception}");
                    return;
                }
            }

            string json = DeserializeToJson(bytes);

            JsonUtility.FromJsonOverwrite(json, _gameData);

            Debug.Log("Game data loaded.");
        }

        public void SaveGameData()
        {
            if (_gameData == null)
            {
                Debug.LogWarning("GameData reference is not assigned. Cannot save game data.");
                return;
            }

            string json = JsonUtility.ToJson(_gameData, prettyPrint: false);
            byte[] bytes = SerializeFromJson(json);

            if (_useEncryption && bytes.Length > 0)
            {
                try
                {
                    bytes = Encrypt(bytes);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to encrypt game data. {exception}");
                    return;
                }
            }

            SetDirectory(_gameDataPath);
            File.WriteAllBytes(_gameDataPath, bytes);

            Debug.Log("Game data saved.");
        }

        #region Utility

        private string DeserializeToJson(byte[] bytes)
        {
            switch (_serializationFormat)
            {
                case DataSerializationFormat.Json:
                {
                    return Encoding.UTF8.GetString(bytes);
                }

                case DataSerializationFormat.Binary:
                {
                    using MemoryStream memoryStream = new MemoryStream(bytes);
                    using BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);

                    return binaryReader.ReadString();
                }

                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private byte[] SerializeFromJson(string json)
        {
            switch (_serializationFormat)
            {
                case DataSerializationFormat.Json:
                {
                    return Encoding.UTF8.GetBytes(json);
                }

                case DataSerializationFormat.Binary:
                {
                    using MemoryStream memoryStream = new MemoryStream();
                    using BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);

                    binaryWriter.Write(json);
                    binaryWriter.Flush();
                    return memoryStream.ToArray();
                }

                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private void SetDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        
        private byte[] Decrypt(byte[] cipherBytes)
        {
            if (string.IsNullOrEmpty(_encryptionPassword))
            {
                Debug.LogWarning("Encryption password is empty. Data will be treated as plain bytes.");
                return cipherBytes;
            }

            using Aes aes = Aes.Create();
            using Rfc2898DeriveBytes keyDerivation =
                new Rfc2898DeriveBytes(_encryptionPassword, _saltBytes, 10000);

            aes.Key = keyDerivation.GetBytes(32);
            aes.IV = keyDerivation.GetBytes(16);

            using MemoryStream memoryStream = new MemoryStream(cipherBytes);
            using CryptoStream cryptoStream =
                new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using MemoryStream resultStream = new MemoryStream();

            cryptoStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }

        private byte[] Encrypt(byte[] plainBytes)
        {
            if (string.IsNullOrEmpty(_encryptionPassword))
            {
                Debug.LogWarning("Encryption password is empty. Data will not be encrypted.");
                return plainBytes;
            }

            using Aes aes = Aes.Create();
            using Rfc2898DeriveBytes keyDerivation =
                new Rfc2898DeriveBytes(_encryptionPassword, _saltBytes, 10000);

            aes.Key = keyDerivation.GetBytes(32); // 256-bit key
            aes.IV = keyDerivation.GetBytes(16); // 128-bit IV

            using MemoryStream memoryStream = new MemoryStream();
            using CryptoStream cryptoStream =
                new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();

            return memoryStream.ToArray();
        }

        #endregion
    }
}
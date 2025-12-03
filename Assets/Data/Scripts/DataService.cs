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

            string directory = Path.GetDirectoryName(_playerDataPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(_playerDataPath, bytes);

            Debug.Log("Player data saved.");
        }

        public void ResetPlayerData()
        {
            ApplyInitialPlayerData();
            SavePlayerData();

            Debug.Log("Player data reset to initial defaults and saved.");
        }

        #region Utility

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
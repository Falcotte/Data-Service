using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AngryKoala.Data
{
    // Reads and writes data files in one folder. A write goes to a temporary file first. The file it replaces
    // is kept as the backup, or set aside when it could not be read.
    internal sealed class DataStorage
    {
        public string FolderPath { get; }

        public bool CanObfuscate => !string.IsNullOrEmpty(_obfuscationKey);

        private readonly string _obfuscationKey;

        private byte[] _keyBytes;

        private static readonly byte[] _saltBytes = Encoding.UTF8.GetBytes("AngryKoala_Data_Salt");

        private const string _fileExtension = ".dat";
        private const string _backupExtension = ".bak";
        private const string _temporaryExtension = ".tmp";
        private const string _unreadableExtension = ".corrupt";

        private const int _keyLength = 32;
        private const int _blockLength = 16;
        private const int _keyIterations = 10000;

        public DataStorage(string folderPath, string obfuscationKey)
        {
            FolderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            _obfuscationKey = obfuscationKey;
        }

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        public string GetPath(string fileName, DataFileKind kind)
        {
            string extension = kind switch
            {
                DataFileKind.Current => _fileExtension,
                DataFileKind.Backup => _backupExtension,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };

            return Path.Combine(FolderPath, fileName + extension);
        }

        public DataReadResult Read(string fileName, DataFileKind kind, bool obfuscate, out DataFileContent content)
        {
            content = null;

            try
            {
                string path = GetPath(fileName, kind);

                if (!File.Exists(path))
                {
                    return DataReadResult.Missing;
                }

                byte[] bytes = File.ReadAllBytes(path);

                if (obfuscate && CanObfuscate)
                {
                    bytes = Decrypt(bytes);
                }

                string text = Encoding.UTF8.GetString(bytes);

                return DataFileContent.TryParse(text, out content) ? DataReadResult.Read : DataReadResult.Unreadable;
            }
            catch (Exception exception) when (exception is IOException or CryptographicException
                                                  or UnauthorizedAccessException or ArgumentException)
            {
                return DataReadResult.Unreadable;
            }
        }

        public void Write(string fileName, bool obfuscate, DataFileContent content,
            DataReplaceRule replaceRule = DataReplaceRule.KeepAsBackup)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            byte[] bytes = Encoding.UTF8.GetBytes(content.ToText());

            if (obfuscate && CanObfuscate)
            {
                bytes = Encrypt(bytes);
            }

            Directory.CreateDirectory(FolderPath);

            string currentPath = GetPath(fileName, DataFileKind.Current);
            string backupPath = GetPath(fileName, DataFileKind.Backup);
            string temporaryPath = GetTemporaryPath(fileName);

            File.WriteAllBytes(temporaryPath, bytes);

            if (File.Exists(currentPath))
            {
                string replacedPath = replaceRule switch
                {
                    DataReplaceRule.KeepAsBackup => backupPath,
                    DataReplaceRule.SetAside => Path.Combine(FolderPath, fileName + _unreadableExtension),
                    _ => throw new ArgumentOutOfRangeException(nameof(replaceRule))
                };

                File.Delete(replacedPath);
                File.Move(currentPath, replacedPath);
            }

            File.Move(temporaryPath, currentPath);
        }

        public void Delete(string fileName)
        {
            if (!Directory.Exists(FolderPath))
            {
                return;
            }

            File.Delete(GetPath(fileName, DataFileKind.Current));
            File.Delete(GetPath(fileName, DataFileKind.Backup));
            File.Delete(GetTemporaryPath(fileName));
            File.Delete(Path.Combine(FolderPath, fileName + _unreadableExtension));
        }

        private string GetTemporaryPath(string fileName) => Path.Combine(FolderPath, fileName + _temporaryExtension);

        private byte[] Encrypt(byte[] plainBytes)
        {
            using Aes aes = Aes.Create();

            aes.Key = GetKeyBytes();
            aes.GenerateIV();

            using var memoryStream = new MemoryStream();

            memoryStream.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            return memoryStream.ToArray();
        }

        private byte[] Decrypt(byte[] fileBytes)
        {
            if (fileBytes.Length <= _blockLength)
            {
                throw new CryptographicException("The file is too short to hold obfuscated data.");
            }

            var initializationVector = new byte[_blockLength];
            Array.Copy(fileBytes, initializationVector, _blockLength);

            using Aes aes = Aes.Create();

            aes.Key = GetKeyBytes();
            aes.IV = initializationVector;

            using var inputStream = new MemoryStream(fileBytes, _blockLength, fileBytes.Length - _blockLength);
            using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var outputStream = new MemoryStream();

            cryptoStream.CopyTo(outputStream);

            return outputStream.ToArray();
        }

        private byte[] GetKeyBytes()
        {
            if (_keyBytes == null)
            {
                using var keyDerivation = new Rfc2898DeriveBytes(_obfuscationKey, _saltBytes, _keyIterations);
                _keyBytes = keyDerivation.GetBytes(_keyLength);
            }

            return _keyBytes;
        }
    }
}

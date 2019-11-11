using System;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Misc
{
    /// <summary>
    /// contains info about saved database file - including password
    /// </summary>
    [Serializable]
    public class SavedFileInfo
    {
        private string _password;
        private const string EncryptKey = "BDEA0710-5AE3-426A-AFE3-ED4EBEB9E9E8";

        public SavedFileInfo()
        {
        }

        public SavedFileInfo(string filePath, string password = null)
        {
            FilePath = filePath;
            Password = password;
        }

        public string FilePath { get; set; }

        public string Password
        {
            get => string.IsNullOrEmpty(_password) ? _password : StringEncryptionHelper.Decrypt(_password, EncryptKey);
            set => _password = string.IsNullOrEmpty(value) ? value : StringEncryptionHelper.Encrypt(value, EncryptKey);
        }
    }
}
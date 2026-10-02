using System;
using System.IO;
using UnityEngine;

namespace DrivingSim.Save
{
    [DefaultExecutionOrder(-900)]
    public sealed class SaveService : MonoBehaviour
    {
        private const string FileName = "driving-save.json";
        private const string BackupName = "driving-save.backup.json";
        private string savePath;
        private string backupPath;

        public event Action<PlayerProfile> ProfileLoaded;
        public event Action ProfileSaved;
        public PlayerProfile Profile { get; private set; }
        public string SavePath => savePath;

        private void Awake()
        {
            savePath = Path.Combine(Application.persistentDataPath, FileName);
            backupPath = Path.Combine(Application.persistentDataPath, BackupName);
            Load();
        }

        public void Load()
        {
            Profile = TryRead(savePath) ?? TryRead(backupPath) ?? new PlayerProfile();
            Profile.Sanitize();
            ProfileLoaded?.Invoke(Profile);
        }

        public void Save()
        {
            if (Profile == null) return;
            Profile.Sanitize();
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporaryPath = savePath + ".tmp";
            string json = JsonUtility.ToJson(Profile, true);

            try
            {
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(savePath)) File.Copy(savePath, backupPath, true);
                File.Copy(temporaryPath, savePath, true);
                File.Delete(temporaryPath);
                ProfileSaved?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Save failed: {exception.Message}");
            }
        }

        public void ResetProfile()
        {
            Profile = new PlayerProfile();
            Save();
            ProfileLoaded?.Invoke(Profile);
        }

        private static PlayerProfile TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<PlayerProfile>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load '{path}': {exception.Message}");
                return null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();
    }
}

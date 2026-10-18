using System;
using System.IO;
using System.Text;
using RuinsEcho.Core;
using UnityEngine;

namespace RuinsEcho.Save
{
    /// <summary>
    /// SaveData.json 读写（规格 11.3）。写入采用临时文件 + 备份，避免断电时半文件损坏。
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "SaveData.json";

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Load(string path = null)
        {
            path = string.IsNullOrEmpty(path) ? DefaultPath : path;

            if (!File.Exists(path))
            {
                return CreateDefault();
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    return CreateDefault();
                }

                data.EnsureDefaults(MetaIds.DefaultRelicIds, MetaIds.DefaultCharacterIds);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] 存档读取失败，将使用默认存档：{exception.Message}");
                BackupCorruptFile(path);
                return CreateDefault();
            }
        }

        public static SaveData CreateDefault()
        {
            var data = new SaveData();
            data.EnsureDefaults(MetaIds.DefaultRelicIds, MetaIds.DefaultCharacterIds);
            return data;
        }

        public static void Save(SaveData data, string path = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            path = string.IsNullOrEmpty(path) ? DefaultPath : path;

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(data, true);
            string tempPath = path + ".tmp";

            File.WriteAllText(tempPath, json, Utf8NoBom);

            if (File.Exists(path))
            {
                string backupPath = path + ".bak";
                File.Copy(path, backupPath, true);
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }

        private static void BackupCorruptFile(string path)
        {
            try
            {
                string backup = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                File.Copy(path, backup, true);
            }
            catch
            {
                // 备份失败不应阻止游戏启动。
            }
        }
    }
}

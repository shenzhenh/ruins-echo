using System.IO;
using System.Linq;
using RuinsEcho.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RuinsEcho.EditorTools
{
    /// <summary>
    /// 首次打开工程后运行一次：生成 Main 场景并加入 Build Settings。
    /// EchoLab 也可以在任意空场景直接 Play（RuntimeInitializeOnLoadMethod 自动启动）。
    /// </summary>
    public static class ProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/Ruins Echo/Create Main Scene", priority = 0)]
        public static void CreateMainScene()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "覆盖 Main 场景？",
                    $"{ScenePath} 已存在。继续会覆盖该场景。",
                    "覆盖",
                    "取消"))
            {
                return;
            }

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("EchoLab");
            root.AddComponent<EchoLabBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Selection.activeGameObject = root;
            AssetDatabase.Refresh();
            Debug.Log($"[Ruins Echo] 已创建 {ScenePath} 并加入 Build Settings。按 Play 运行技术切片。");
        }

        [MenuItem("Tools/Ruins Echo/Open Main Scene", priority = 1)]
        public static void OpenMainScene()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("未找到 Main 场景", "请先执行 Tools/Ruins Echo/Create Main Scene。", "好");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Ruins Echo/Validate Project", priority = 20)]
        public static void ValidateProject()
        {
            bool ok = true;

            ok &= CheckDirectory("Assets/RuinsEcho/Runtime");
            ok &= CheckDirectory("Assets/RuinsEcho/Editor");
            ok &= CheckDirectory("Assets/RuinsEcho/Tests/EditMode");
            ok &= CheckFile("Packages/manifest.json");
            ok &= CheckFile("ProjectSettings/ProjectVersion.txt");

            Debug.Log(ok
                ? "[Ruins Echo] 工程结构校验通过。"
                : "[Ruins Echo] 工程结构校验失败，请检查上方日志。");
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            if (existing.Any(scene => scene.path == ScenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = existing
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
                .ToArray();
        }

        private static bool CheckDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return true;
            }

            Debug.LogError($"[Ruins Echo] 缺少目录：{path}");
            return false;
        }

        private static bool CheckFile(string path)
        {
            if (File.Exists(path))
            {
                return true;
            }

            Debug.LogError($"[Ruins Echo] 缺少文件：{path}");
            return false;
        }
    }
}

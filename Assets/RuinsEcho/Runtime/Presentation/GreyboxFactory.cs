using System;
using System.Collections.Generic;
using RuinsEcho.Core;
using RuinsEcho.Enemies;
using RuinsEcho.Player;
using RuinsEcho.Recording;
using UnityEngine;

namespace RuinsEcho.Presentation
{
    /// <summary>
    /// 无美术资源的技术切片工厂：运行时生成纯色 Sprite，避免仓库依赖图片。
    /// 正式版本替换为 Prefab / Tilemap，脚本以外的接口保持不变。
    /// </summary>
    public static class GreyboxFactory
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SpriteCache.Clear();
        }

        public static Camera CreateCamera()
        {
            Camera existing = Camera.main;
            if (existing != null)
            {
                ConfigureCamera(existing);
                return existing;
            }

            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 0f, -10f);
            ConfigureCamera(camera);
            return camera;
        }

        private static void ConfigureCamera(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = 7.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.055f, 0.075f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
        }

        public static void CreateArena(Transform parent)
        {
            ArenaBounds.Reset();

            var floor = new GameObject("Floor");
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, 0f, 1f);
            floor.transform.localScale = new Vector3(
                GameConfig.ArenaHalfWidth * 2f,
                GameConfig.ArenaHalfHeight * 2f,
                1f);
            AddSprite(floor, Square(new Color(0.075f, 0.09f, 0.12f, 1f)), -20);

            Color edge = new Color(0.18f, 0.24f, 0.32f, 1f);
            CreateEdge(parent, "EdgeTop", new Vector2(0f, GameConfig.ArenaHalfHeight), new Vector2(GameConfig.ArenaHalfWidth * 2f, 0.12f), edge);
            CreateEdge(parent, "EdgeBottom", new Vector2(0f, -GameConfig.ArenaHalfHeight), new Vector2(GameConfig.ArenaHalfWidth * 2f, 0.12f), edge);
            CreateEdge(parent, "EdgeLeft", new Vector2(-GameConfig.ArenaHalfWidth, 0f), new Vector2(0.12f, GameConfig.ArenaHalfHeight * 2f), edge);
            CreateEdge(parent, "EdgeRight", new Vector2(GameConfig.ArenaHalfWidth, 0f), new Vector2(0.12f, GameConfig.ArenaHalfHeight * 2f), edge);

            Color grid = new Color(0.12f, 0.15f, 0.2f, 0.55f);
            for (int x = -10; x <= 10; x += 2)
            {
                CreateEdge(parent, $"GridX{x}", new Vector2(x, 0f), new Vector2(0.02f, GameConfig.ArenaHalfHeight * 2f - 0.3f), grid, -19);
            }

            for (int y = -6; y <= 6; y += 2)
            {
                CreateEdge(parent, $"GridY{y}", new Vector2(0f, y), new Vector2(GameConfig.ArenaHalfWidth * 2f - 0.3f, 0.02f), grid, -19);
            }
        }

        private static void CreateEdge(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            int sortingOrder = -18)
        {
            var edge = new GameObject(name);
            edge.transform.SetParent(parent, false);
            edge.transform.position = new Vector3(position.x, position.y, 0.9f);
            edge.transform.localScale = new Vector3(size.x, size.y, 1f);
            AddSprite(edge, Square(color), sortingOrder);
        }

        public static GameObject CreatePlayer(Vector2 position)
        {
            var go = new GameObject("Player");
            go.transform.position = position;

            AddSprite(go, Circle(new Color(0.3f, 0.75f, 1f, 1f)), 10);

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            collider.isTrigger = true;

            var aim = new GameObject("AimPivot");
            aim.transform.SetParent(go.transform, false);

            var tip = new GameObject("AimTip");
            tip.transform.SetParent(aim.transform, false);
            tip.transform.localPosition = new Vector3(0.62f, 0f, 0f);
            tip.transform.localScale = new Vector3(0.7f, 0.09f, 1f);
            AddSprite(tip, Square(new Color(1f, 1f, 1f, 0.9f)), 11);

            return go;
        }

        public static EnemyBase CreateEnemy(EnemyKind kind, Vector2 position, Transform parent)
        {
            var go = new GameObject($"Enemy_{kind}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            Sprite sprite;
            Color color;
            switch (kind)
            {
                case EnemyKind.ShadowHunter:
                    sprite = Diamond(new Color(0.85f, 0.35f, 0.95f, 1f));
                    color = new Color(0.85f, 0.35f, 0.95f, 1f);
                    go.transform.localScale = Vector3.one * 1.15f;
                    break;
                case EnemyKind.Ranged:
                    sprite = Circle(new Color(1f, 0.65f, 0.25f, 1f));
                    color = new Color(1f, 0.65f, 0.25f, 1f);
                    break;
                default:
                    sprite = Square(new Color(1f, 0.35f, 0.35f, 1f));
                    color = new Color(1f, 0.35f, 0.35f, 1f);
                    break;
            }

            SpriteRenderer renderer = AddSprite(go, sprite, 8);
            renderer.color = color;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            collider.isTrigger = true;

            return go.AddComponent<EnemyBase>();
        }

        public static ShadowEcho CreateShadow(Transform parent)
        {
            var go = new GameObject("ShadowEcho");
            go.transform.SetParent(parent, false);

            SpriteRenderer renderer = AddSprite(go, Circle(new Color(0.35f, 0.95f, 0.95f, 0.6f)), 9);
            renderer.color = new Color(0.35f, 0.95f, 0.95f, 0.6f);

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;
            collider.isTrigger = true;

            go.SetActive(false);
            return go.AddComponent<ShadowEcho>();
        }

        public static Sprite Circle(Color color)
        {
            return BuildSprite("circle_" + ColorKey(color), 64, (x, y) =>
            {
                float dx = x - 31.5f;
                float dy = y - 31.5f;
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                if (distance <= 30f)
                {
                    return color;
                }

                if (distance <= 32f)
                {
                    return new Color(1f, 1f, 1f, color.a * 0.45f);
                }

                return Color.clear;
            });
        }

        public static Sprite Diamond(Color color)
        {
            return BuildSprite("diamond_" + ColorKey(color), 64, (x, y) =>
            {
                float distance = Mathf.Abs(x - 31.5f) + Mathf.Abs(y - 31.5f);
                return distance <= 30f ? color : Color.clear;
            });
        }

        public static Sprite Square(Color color)
        {
            return BuildSprite("square_" + ColorKey(color), 8, (x, y) => color);
        }

        private static Sprite BuildSprite(string key, int size, Func<int, int, Color> pixel)
        {
            if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    pixels[(y * size) + x] = pixel(x, y);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            sprite.name = key;
            sprite.hideFlags = HideFlags.DontSave;

            SpriteCache[key] = sprite;
            return sprite;
        }

        private static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int sortingOrder)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static string ColorKey(Color color)
        {
            return $"{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}";
        }
    }
}

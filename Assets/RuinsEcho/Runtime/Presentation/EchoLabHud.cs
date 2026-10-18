using System.Collections.Generic;
using RuinsEcho.Relics;
using UnityEngine;

namespace RuinsEcho.Presentation
{
    /// <summary>
    /// 技术切片的调试 HUD（IMGUI）。正式 MVP 的 UGUI 血条 / 能量条 / 遗物栏
    /// 在 docs/roadmap.md M5 中替换；这里优先保证核心链路可观察、可操作。
    /// </summary>
    public sealed class EchoLabHud : MonoBehaviour
    {
        private struct Toast
        {
            public string Message;
            public float ExpiresAt;
        }

        private readonly List<Toast> _toasts = new List<Toast>();
        private EchoLabBootstrap _lab;
        private Font _font;
        private GUIStyle _label;
        private GUIStyle _small;
        private GUIStyle _title;
        private GUIStyle _button;
        private GUIStyle _center;
        private bool _stylesReady;

        public void Bind(EchoLabBootstrap lab)
        {
            _lab = lab;
        }

        public void ShowToast(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _toasts.Add(new Toast
            {
                Message = message,
                ExpiresAt = Time.unscaledTime + 3.5f
            });

            if (_toasts.Count > 5)
            {
                _toasts.RemoveAt(0);
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (_toasts[i].ExpiresAt <= now)
                {
                    _toasts.RemoveAt(i);
                }
            }
        }

        private void OnGUI()
        {
            if (_lab == null || _lab.Player == null)
            {
                return;
            }

            EnsureStyles();

            DrawVitals();
            DrawBufferInfo();
            DrawRelics();
            DrawControls();
            DrawToasts();

            if (_lab.Choices.Count > 0)
            {
                DrawReward();
            }

            if (_lab.IsDead)
            {
                DrawDeath();
            }
        }

        private void DrawVitals()
        {
            var panel = new Rect(20f, 18f, 340f, 116f);
            DrawPanel(panel);

            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, 300f, 22f), "遗迹回响 · Echo Lab", _title);

            float hpRatio = _lab.Player.MaxHP > 0f ? _lab.Player.HP / _lab.Player.MaxHP : 0f;
            DrawBar(new Rect(panel.x + 12f, panel.y + 42f, 300f, 20f), hpRatio, new Color(0.9f, 0.25f, 0.3f, 1f));
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 42f, 290f, 20f),
                $"HP {_lab.Player.HP:F0} / {_lab.Player.MaxHP:F0}",
                _small);

            float energyRatio = _lab.Controller.Energy.Normalized;
            DrawBar(new Rect(panel.x + 12f, panel.y + 68f, 300f, 18f), energyRatio, new Color(0.25f, 0.8f, 0.95f, 1f));
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 68f, 290f, 18f),
                $"回响能量 {_lab.Controller.Energy.CurrentEnergy:F0} / {_lab.Controller.Energy.MaxEnergy:F0}",
                _small);

            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 90f, 310f, 20f),
                $"召唤消耗 {_lab.Controller.Energy.SummonCost:F0}  ·  种子 {_lab.Seed}",
                _small);
        }

        private void DrawBufferInfo()
        {
            var panel = new Rect(20f, 144f, 340f, 92f);
            DrawPanel(panel);

            float duration = _lab.Recorder.Buffer.Duration;
            float maxDuration = _lab.Recorder.Buffer.MaxDuration;
            int frames = _lab.Recorder.Buffer.Count;

            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, 310f, 22f), "动作录制缓存", _label);
            DrawBar(
                new Rect(panel.x + 12f, panel.y + 38f, 300f, 16f),
                maxDuration > 0f ? Mathf.Clamp01(duration / maxDuration) : 0f,
                new Color(0.95f, 0.75f, 0.3f, 1f));

            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 58f, 310f, 24f),
                $"{duration:F2}s / {maxDuration:F1}s   {frames} 帧   影子 {_lab.Pool.ActiveCount}/{_lab.Pool.MaxSize}",
                _small);
        }

        private void DrawRelics()
        {
            IReadOnlyList<RelicData> relics = _lab.Relics.ActiveRelics;
            var panel = new Rect(Screen.width - 300f, Screen.height - 260f, 280f, 200f);
            DrawPanel(panel);

            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 8f, 260f, 22f),
                $"回响构筑 {relics.Count} / {_lab.Relics.MaxRelicCount}",
                _label);

            RelicData hovered = null;
            for (int i = 0; i < relics.Count; i++)
            {
                var row = new Rect(panel.x + 12f, panel.y + 34f + (i * 20f), 256f, 20f);
                RelicData relic = relics[i];
                string prefix = relic.isTwisted ? "◆ " : "◇ ";
                GUI.Label(row, prefix + relic.displayName, _small);

                if (row.Contains(Event.current.mousePosition))
                {
                    hovered = relic;
                }
            }

            if (hovered != null)
            {
                var tooltip = new Rect(panel.x - 300f, panel.y + 30f, 280f, 120f);
                DrawPanel(tooltip);
                GUI.Label(
                    new Rect(tooltip.x + 10f, tooltip.y + 8f, 260f, 104f),
                    $"{hovered.displayName}\n{hovered.description}",
                    _small);
            }
        }

        private void DrawControls()
        {
            var panel = new Rect(20f, Screen.height - 116f, 430f, 96f);
            DrawPanel(panel);
            GUI.Label(
                new Rect(panel.x + 12f, panel.y + 8f, 410f, 80f),
                "WASD 移动   ·   鼠标瞄准   ·   左键普攻\n" +
                "空格闪避   ·   Q 召唤影子   ·   R 清空缓存\n" +
                "战斗结束按 1 / 2 / 3 选择遗物",
                _small);
        }

        private void DrawToasts()
        {
            float y = Screen.height - 170f;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                float alpha = Mathf.Clamp01((_toasts[i].ExpiresAt - Time.unscaledTime) / 0.6f);
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(
                    new Rect((Screen.width * 0.5f) - 220f, y, 440f, 24f),
                    _toasts[i].Message,
                    _center);
                GUI.color = previous;
                y -= 26f;
            }
        }

        private void DrawReward()
        {
            var overlay = new Rect(0f, 0f, Screen.width, Screen.height);
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(overlay, Texture2D.whiteTexture);
            GUI.color = previous;

            var panel = new Rect(
                (Screen.width * 0.5f) - 380f,
                (Screen.height * 0.5f) - 190f,
                760f,
                380f);
            DrawPanel(panel);

            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 16f, 712f, 34f),
                "战斗结束 · 选择一件回响",
                _title);

            IReadOnlyList<RelicData> choices = _lab.Choices;
            for (int i = 0; i < choices.Count; i++)
            {
                RelicData relic = choices[i];
                var card = new Rect(panel.x + 24f + (i * 240f), panel.y + 66f, 224f, 200f);
                string kind = relic.isTwisted ? "扭曲回响" : "增益回响";

                if (GUI.Button(card, $"{i + 1}\n{relic.displayName}\n\n{kind}\n\n{relic.description}", _button))
                {
                    _lab.SelectRelic(i);
                    break;
                }
            }

            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 286f, 712f, 70f),
                $"当前携带 {_lab.Relics.ActiveRelics.Count} / {_lab.Relics.MaxRelicCount} 件。达到上限后将无法继续拾取。",
                _small);
        }

        private void DrawDeath()
        {
            var overlay = new Rect(0f, 0f, Screen.width, Screen.height);
            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0f, 0f, 0.72f);
            GUI.DrawTexture(overlay, Texture2D.whiteTexture);
            GUI.color = previous;

            var panel = new Rect(
                (Screen.width * 0.5f) - 260f,
                (Screen.height * 0.5f) - 130f,
                520f,
                260f);
            DrawPanel(panel);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 20f, 480f, 40f), "本局结束", _title);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 70f, 480f, 80f),
                $"抵达第 {_lab.WaveIndex + 1} 波\n" +
                $"携带 {_lab.Relics.ActiveRelics.Count} 件回响\n" +
                "所有局内数据已销毁，局外存档保留。",
                _small);

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 170f, 480f, 50f), "按 Enter 或点击此处重新开始", _button))
            {
                _lab.RestartRun();
            }
        }

        private void DrawBar(Rect rect, float fill, Color color)
        {
            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), Texture2D.whiteTexture);

            GUI.color = previous;
        }

        private void DrawPanel(Rect rect)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.06f, 0.08f, 0.11f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _stylesReady = true;

            try
            {
                _font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "SimHei", "Arial" },
                    16);
            }
            catch
            {
                _font = null;
            }

            _label = new GUIStyle(GUI.skin.label)
            {
                font = _font,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                richText = false
            };
            _label.normal.textColor = Color.white;

            _small = new GUIStyle(_label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal
            };
            _small.normal.textColor = new Color(0.85f, 0.9f, 0.95f, 1f);

            _title = new GUIStyle(_label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };

            _button = new GUIStyle(GUI.skin.button)
            {
                font = _font,
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 10, 10)
            };
            _button.normal.textColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            _button.hover.textColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            _button.active.textColor = Color.black;

            _center = new GUIStyle(_label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            _center.normal.textColor = Color.white;
        }
    }
}

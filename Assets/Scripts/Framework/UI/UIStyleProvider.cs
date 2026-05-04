// Assets/Scripts/Framework/UI/UIStyleProvider.cs
// AgentSim — IMGUI スタイルキャッシュ
//
// GUIStyle を初回のみ生成してキャッシュする。
// OnGUI() 内で毎フレーム new GUIStyle() するとガベージが発生するため、
// すべての共通スタイルをここで管理する。

using UnityEngine;

namespace AgentSim.UI
{
    public static class UIStyleProvider
    {
        // ── キャッシュ ────────────────────────────────────────────────
        private static GUIStyle _panelBg;
        private static GUIStyle _sectionHeader;
        private static GUIStyle _labelStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _selectedRow;
        private static GUIStyle _normalRow;
        private static GUIStyle _titleStyle;

        // ── アクセサー（lazy init） ────────────────────────────────────
        public static GUIStyle PanelBackground => _panelBg       ??= BuildPanelBg();
        public static GUIStyle SectionHeader   => _sectionHeader ??= BuildSectionHeader();
        public static GUIStyle LabelStyle      => _labelStyle    ??= BuildLabel();
        public static GUIStyle ValueStyle      => _valueStyle    ??= BuildValue();
        public static GUIStyle SelectedRow     => _selectedRow   ??= BuildSelectedRow();
        public static GUIStyle NormalRow       => _normalRow     ??= BuildNormalRow();
        public static GUIStyle TitleStyle      => _titleStyle    ??= BuildTitle();

        // ── ファクトリ ────────────────────────────────────────────────
        private static GUIStyle BuildPanelBg()
        {
            var s = new GUIStyle(GUI.skin.box);
            s.normal.background = MakeTex(2, 2, new Color(0.12f, 0.12f, 0.14f, 0.95f));
            s.border = new RectOffset(4, 4, 4, 4);
            return s;
        }

        private static GUIStyle BuildSectionHeader()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize  = 11;
            s.fontStyle = FontStyle.Bold;
            s.normal.textColor = new Color(0.7f, 0.85f, 1.0f);
            s.padding = new RectOffset(4, 4, 6, 2);
            return s;
        }

        private static GUIStyle BuildLabel()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 11;
            s.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            s.padding = new RectOffset(4, 4, 1, 1);
            return s;
        }

        private static GUIStyle BuildValue()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize  = 11;
            s.fontStyle = FontStyle.Bold;
            s.normal.textColor = new Color(1.0f, 1.0f, 0.85f);
            s.padding = new RectOffset(4, 4, 1, 1);
            return s;
        }

        private static GUIStyle BuildSelectedRow()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 11;
            s.normal.background = MakeTex(2, 2, new Color(0.25f, 0.45f, 0.65f, 0.7f));
            s.normal.textColor  = Color.white;
            s.padding = new RectOffset(6, 4, 3, 3);
            return s;
        }

        private static GUIStyle BuildNormalRow()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 11;
            s.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            s.hover.background = MakeTex(2, 2, new Color(1f, 1f, 1f, 0.05f));
            s.hover.textColor  = Color.white;
            s.padding = new RectOffset(6, 4, 3, 3);
            return s;
        }

        private static GUIStyle BuildTitle()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize  = 13;
            s.fontStyle = FontStyle.Bold;
            s.normal.textColor = new Color(0.9f, 0.9f, 1.0f);
            s.padding = new RectOffset(6, 4, 4, 4);
            return s;
        }

        // ── ユーティリティ ────────────────────────────────────────────
        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            return t;
        }
    }
}

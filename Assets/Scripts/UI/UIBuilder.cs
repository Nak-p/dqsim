using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DQSim
{
    /// <summary>
    /// Play時にUIを動的構築するための静的ユーティリティ。
    /// </summary>
    public static class UIBuilder
    {
        // RectTransform をストレッチアンカーで埋める
        public static void Stretch(RectTransform rt, float l = 0, float r = 0, float b = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        // 上端アンカー固定のバー
        public static GameObject TopBar(Transform parent, float height, Color bg)
        {
            var go = new GameObject("TopBar");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = bg;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, height);
            return go;
        }

        // 下端アンカー固定のボタン
        public static Button BottomBtn(Transform parent, string label, float fontSize,
            Color bg, float bottom, float height)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, bottom);
            rt.sizeDelta = new Vector2(-8, height);
            AddLabel(go.transform, label, fontSize, Color.white, TextAlignmentOptions.Center);
            return btn;
        }

        // 左右半分ボタン（Dispatch/Cancel用）
        public static Button HalfBtn(Transform parent, string label, float fontSize,
            Color bg, bool left)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = left ? new Vector2(0.04f, 0) : new Vector2(0.54f, 0);
            rt.anchorMax = left ? new Vector2(0.46f, 0) : new Vector2(0.96f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 8);
            rt.sizeDelta = new Vector2(0, 44);
            AddLabel(go.transform, label, fontSize, Color.white, TextAlignmentOptions.Center);
            return btn;
        }

        // テキストラベルを親にストレッチで追加
        public static TextMeshProUGUI AddLabel(Transform parent, string text,
            float fontSize, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text         = text;
            tmp.fontSize     = fontSize;
            tmp.color        = color;
            tmp.alignment    = align;
            tmp.overflowMode = TMPro.TextOverflowModes.Overflow; // never clip text silently
            Stretch(go.GetComponent<RectTransform>(), 4, 4, 2, 2);
            return tmp;
        }

        /// <summary>
        /// スクロール可能なコンテンツエリアを生成。
        /// topOffset: 親の上端からの距離, bottomOffset: 親の下端からの距離。
        /// 返り値: 行アイテムをSetParentする先のContent RectTransform。
        /// </summary>
        public static RectTransform ScrollContent(Transform parent, float topOffset, float bottomOffset)
        {
            // Scroll root
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(parent, false);
            scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
            var srt = scrollGO.GetComponent<RectTransform>();
            Stretch(srt, 3, 3, bottomOffset, topOffset);

            // Viewport with RectMask2D (avoids stencil vs TMP shader conflict)
            var vpGO = new GameObject("Viewport");
            vpGO.transform.SetParent(scrollGO.transform, false);
            vpGO.AddComponent<Image>().color = Color.clear; // forces RectTransform
            vpGO.AddComponent<RectMask2D>();
            var vpRT = vpGO.GetComponent<RectTransform>();
            Stretch(vpRT);

            // Content (Image追加でRectTransformを強制生成)
            var cGO = new GameObject("Content");
            cGO.transform.SetParent(vpGO.transform, false);
            cGO.AddComponent<Image>().color = Color.clear;

            var vlg = cGO.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 3;
            vlg.padding = new RectOffset(2, 2, 2, 2);

            cGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var crt = cGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot     = new Vector2(0.5f, 1);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = Vector2.zero;

            // ScrollRect
            var sr = scrollGO.AddComponent<ScrollRect>();
            sr.viewport     = vpRT;
            sr.content      = crt;
            sr.horizontal   = false;
            sr.vertical     = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            return crt;
        }

        // 行アイテム用の背景GO（LayoutElement付き）
        public static GameObject Row(Transform parent, Color bg, float height)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = bg;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return go;
        }

        // 行の中に左寄せTMPを追加（anchorMin/Max で比率指定）
        public static TextMeshProUGUI RowCell(Transform rowParent, string text,
            float fontSize, Color color,
            Vector2 ancMin, Vector2 ancMax)
        {
            var go = new GameObject("Cell");
            go.transform.SetParent(rowParent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text         = text;
            tmp.fontSize     = fontSize;
            tmp.color        = color;
            tmp.alignment    = TextAlignmentOptions.MidlineLeft;
            tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin;
            rt.anchorMax = ancMax;
            rt.offsetMin = new Vector2(4, 2);
            rt.offsetMax = new Vector2(-2, -2);
            return tmp;
        }
    }
}

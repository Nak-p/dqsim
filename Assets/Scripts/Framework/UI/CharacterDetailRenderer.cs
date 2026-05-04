// Assets/Scripts/Framework/UI/CharacterDetailRenderer.cs
// AgentSim — キャラクターパラメータ詳細ブロックの IMGUI 描画
//
// Character 1体分のパラメータを指定 Rect 内に描画する静的ヘルパー。
// ステータス名・用語はすべて SettingsRegistry から取得する。
// C# にステータス名・ゲーム用語をハードコーディングしてはいけない。

using System.Collections.Generic;
using UnityEngine;
using AgentSim.Config;
using AgentSim.Core;

namespace AgentSim.UI
{
    public static class CharacterDetailRenderer
    {
        // ── 列レイアウト定数（アルゴリズム用、ゲームパラメータではない） ──
        private const float LabelColWidth = 80f;
        private const float RowHeight     = 18f;
        private const float SectionGap    = 6f;

        // ── 公開 API ──────────────────────────────────────────────────
        /// <summary>
        /// rect 内に character のパラメータを描画する。
        /// cachedDerived は OnStateChanged 時に事前計算した derived stat のキャッシュ。
        /// cachedTotalPower も同様にキャッシュ済みの値を渡す。
        /// </summary>
        public static void Draw(
            Rect rect,
            Character character,
            Dictionary<string, int> cachedDerived,
            int cachedTotalPower)
        {
            if (SettingsRegistry.Current == null) return;

            var game  = SettingsRegistry.Current.Game;
            var stats = SettingsRegistry.Current.Stats;
            var role  = SettingsRegistry.Current.GetRole(character.RoleId);
            var origin = SettingsRegistry.Current.GetOrigin(character.OriginId);
            var tier  = SettingsRegistry.Current.GetTier(character.TierId);

            float x = rect.x + 8f;
            float y = rect.y + 8f;
            float w = rect.width - 16f;

            var lStyle = UIStyleProvider.LabelStyle;
            var vStyle = UIStyleProvider.ValueStyle;
            var hStyle = UIStyleProvider.SectionHeader;

            // ── 識別情報 ──────────────────────────────────────────────
            DrawRow(x, ref y, w, character.Name, $"Age {character.Age}", vStyle, lStyle);
            DrawRow(x, ref y, w, role?.display_name   ?? character.RoleId,   tier?.display_name  ?? character.TierId,  lStyle, vStyle);
            DrawRow(x, ref y, w, origin?.display_name ?? character.OriginId, character.IsAvailable ? "Available" : "On Contract", lStyle, lStyle);

            y += SectionGap;

            // ── Primary Stats ─────────────────────────────────────────
            GUI.Label(new Rect(x, y, w, RowHeight), $"— {GetPrimaryHeader()} —", hStyle);
            y += RowHeight;

            for (int i = 0; i < stats.primary_stats.Length; i++)
            {
                var def = stats.primary_stats[i];
                int val = character.Stats.GetPrimary(i);
                DrawRow(x, ref y, w, def.display_name, val.ToString(), lStyle, vStyle);
            }

            y += SectionGap;

            // ── Derived Stats ─────────────────────────────────────────
            GUI.Label(new Rect(x, y, w, RowHeight), $"— {GetDerivedHeader()} —", hStyle);
            y += RowHeight;

            foreach (var def in stats.derived_stats)
            {
                int val = cachedDerived.TryGetValue(def.id, out int cached) ? cached : 0;
                DrawRow(x, ref y, w, def.display_name, val.ToString(), lStyle, vStyle);
            }

            y += SectionGap;

            // ── 総合戦力 ──────────────────────────────────────────────
            DrawRow(x, ref y, w, "Power", cachedTotalPower.ToString(), lStyle, vStyle);

            y += SectionGap;

            // ── 通貨 ──────────────────────────────────────────────────
            string currLabel    = game.currency_name;
            string currSymbol   = game.currency_symbol;
            DrawRow(x, ref y, w, currLabel,
                $"{character.CurrentCurrency}{currSymbol}  (Earned: {character.EarnedCurrency}{currSymbol})",
                lStyle, lStyle);
        }

        // ── 内部ヘルパー ──────────────────────────────────────────────
        private static void DrawRow(
            float x, ref float y, float w,
            string label, string value,
            GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUI.Label(new Rect(x,                  y, LabelColWidth,          RowHeight), label, labelStyle);
            GUI.Label(new Rect(x + LabelColWidth,  y, w - LabelColWidth,      RowHeight), value, valueStyle);
            y += RowHeight;
        }

        // セクションヘッダーは将来 i18n 対応できるよう別メソッドに分離
        private static string GetPrimaryHeader()  => "Primary Stats";
        private static string GetDerivedHeader()  => "Derived Stats";
    }
}

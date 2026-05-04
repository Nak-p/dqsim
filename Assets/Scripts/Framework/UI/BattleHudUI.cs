// Assets/Scripts/Framework/UI/BattleHudUI.cs
// AgentSim — バトル HUD（IMGUI）
//
// H キーで表示切替。
// 表示テキストはすべて ActionDef.display_name や SettingsRegistry から取得し、
// C# に世界観固有の文字列をハードコーディングしない。

using System.Collections.Generic;
using UnityEngine;
using AgentSim.Battle;
using AgentSim.Config;

namespace AgentSim.UI
{
    public class BattleHudUI : MonoBehaviour
    {
        [SerializeField] private BattleTurnManager turnManager;

        // レイアウト定数（アルゴリズム用ピクセル値、ゲームパラメータではない）
        private const int PanelW    = 400;
        private const int PanelH    = 340;
        private const int RowH      = 22;
        private const int BarH      = 14;
        private const int BtnH      = 26;
        private const int Padding   = 8;

        private bool     _visible   = true;
        private Vector2  _scroll;

        // HP/AP バー用の 1px テクスチャ（初回のみ生成）
        private static Texture2D _texWhite;
        private static Texture2D _texDark;

        private static Texture2D TexWhite => _texWhite ??= MakeTex(Color.white);
        private static Texture2D TexDark  => _texDark  ??= MakeTex(new Color(0.15f, 0.15f, 0.15f));

        // ── 初期化 ────────────────────────────────────────────────────
        public void Initialize(BattleTurnManager tm)
        {
            turnManager = tm;
            turnManager.OnStateChanged += Repaint;
        }

        private void Repaint() { }  // OnStateChanged で GUI を更新させるだけ

        private void OnDestroy()
        {
            if (turnManager != null)
                turnManager.OnStateChanged -= Repaint;
        }

        // ── 入力 ─────────────────────────────────────────────────────
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                _visible = !_visible;
        }

        // ── 描画 ─────────────────────────────────────────────────────
        private void OnGUI()
        {
            if (!_visible || turnManager == null) return;

            int panelX = 10;
            int panelY = Screen.height - PanelH - 10;
            var panelRect = new Rect(panelX, panelY, PanelW, PanelH);

            GUI.Box(panelRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(panelX + Padding, panelY + Padding,
                                         PanelW - Padding * 2, PanelH - Padding * 2));

            DrawActiveUnit();

            if (turnManager.Phase == BattleTurnManager.PhasePlayerAction)
                DrawActionButtons();

            DrawTurnOrder();

            if (turnManager.Phase == BattleTurnManager.PhaseBattleOver)
                DrawBattleOverOverlay();

            GUILayout.EndArea();
        }

        // ── アクティブユニット情報 ────────────────────────────────────
        private void DrawActiveUnit()
        {
            var unit = turnManager.ActiveUnit;
            if (unit == null) return;

            var cfg = SettingsRegistry.Current?.Game;
            string teamLabel = unit.Team == BattleTeam.Player
                ? (cfg?.agent_term  ?? "Player")
                : "Enemy";

            int maxTurns = cfg?.battle_max_turns ?? 0;
            GUILayout.Label($"[{unit.AgentName}]  Turn {turnManager.CurrentTurn - 1} / {maxTurns}");
            GUILayout.Label(teamLabel);

            GUILayout.Space(2);
            DrawBar(unit.CurrentHp / (float)unit.MaxHp,
                    $"HP  {unit.CurrentHp} / {unit.MaxHp}",
                    new Color(0.2f, 0.8f, 0.3f));

            DrawBar(unit.CurrentAp / unit.MaxAp,
                    $"AP  {unit.CurrentAp:F1} / {unit.MaxAp:F1}",
                    new Color(0.3f, 0.6f, 1.0f));

            GUILayout.Space(4);
        }

        // ── アクションボタン ──────────────────────────────────────────
        private void DrawActionButtons()
        {
            var unit    = turnManager.ActiveUnit;
            var actions = SettingsRegistry.Current?.Actions?.actions;
            if (unit == null || actions == null) return;

            GUILayout.Label("─ Actions ─");

            foreach (var action in actions)
            {
                bool canAfford = unit.CurrentAp >= action.cost;
                GUI.enabled = canAfford;

                if (GUILayout.Button($"  {action.display_name}  (AP {action.cost:F1})",
                                     GUILayout.Height(BtnH)))
                    turnManager.OnActionSelected(action);

                GUI.enabled = true;
            }

            if (GUILayout.Button("  End Turn", GUILayout.Height(BtnH)))
                turnManager.EndPlayerTurn();

            GUILayout.Space(4);
        }

        // ── ターン順リスト ────────────────────────────────────────────
        private void DrawTurnOrder()
        {
            GUILayout.Label("─ Turn Order ─");

            var order = turnManager.TurnOrder;
            if (order == null || order.Count == 0) return;

            int visibleRows = 5;
            float listH = visibleRows * RowH;
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(listH));

            foreach (var u in order)
            {
                if (!u.IsAlive) continue;
                bool active = u == turnManager.ActiveUnit;
                string team = u.Team == BattleTeam.Player ? "P" : "E";
                string label = $"{(active ? "> " : "  ")}{u.AgentName} [{team}]  {u.CurrentHp}/{u.MaxHp}";

                if (active)
                {
                    var prev = GUI.color;
                    GUI.color = new Color(1f, 0.9f, 0.4f);
                    GUILayout.Label(label, GUILayout.Height(RowH));
                    GUI.color = prev;
                }
                else
                {
                    GUILayout.Label(label, GUILayout.Height(RowH));
                }
            }

            GUILayout.EndScrollView();
        }

        // ── バトル終了オーバーレイ ────────────────────────────────────
        private void DrawBattleOverOverlay()
        {
            GUILayout.EndArea();  // 一度 EndArea して中央に描画

            float ow = 260, oh = 100;
            float ox = (Screen.width  - ow) * 0.5f;
            float oy = (Screen.height - oh) * 0.5f;

            GUI.Box(new Rect(ox, oy, ow, oh), GUIContent.none);
            GUILayout.BeginArea(new Rect(ox + Padding, oy + Padding,
                                         ow - Padding * 2, oh - Padding * 2));

            string result = turnManager.PlayerWon ? "VICTORY" : "DEFEAT";
            GUILayout.Label(result, GetCenteredLabelStyle());
            GUILayout.Space(8);
            if (GUILayout.Button("OK", GUILayout.Height(BtnH)))
                _visible = false;

            GUILayout.EndArea();

            // パネル領域を再開（EndArea と対になる BeginArea の代替）
            GUILayout.BeginArea(new Rect(0, 0, 0, 0));
        }

        // ── HP/AP バー描画ヘルパー ────────────────────────────────────
        private void DrawBar(float fraction, string label, Color fill)
        {
            GUILayout.Label(label);

            var rect = GUILayoutUtility.GetRect(PanelW - Padding * 2 - 4, BarH);
            GUI.DrawTexture(rect, TexDark);
            var fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height);
            var prevColor = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(fillRect, TexWhite);
            GUI.color = prevColor;

            GUILayout.Space(2);
        }

        private static GUIStyle GetCenteredLabelStyle()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize   = 20,
                fontStyle  = FontStyle.Bold
            };
            return style;
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}

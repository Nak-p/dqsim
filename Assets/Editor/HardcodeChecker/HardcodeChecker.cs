// Assets/Editor/HardcodeChecker/HardcodeChecker.cs
// AgentSim フレームワーク — ハードコーディング検知エディタスクリプト
//
// 動作:
//   - Unity メニュー [AgentSim > Check Hardcoding Now] で手動実行
//   - プロジェクト変更時（スクリプト保存後）に自動実行
//   - Assets/Scripts/Framework/ 配下のみチェック（Game/ は世界観固有コード許容）
//
// エラー(赤)  : フレームワーク設計を壊す違反（ゲーム固有 enum、世界観固有リテラル）
// 警告(黄)    : 設計上の懸念（パラメータ定数、テーマ依存ステータス名）

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace AgentSim.Editor
{
    [InitializeOnLoad]
    public static class HardcodeChecker
    {
        // ── 検知ルール定義 ────────────────────────────────────────────
        // (pattern, メッセージ, isError)
        // isError=true → LogError (赤), false → LogWarning (黄)
        private static readonly (Regex Pattern, string Message, bool IsError)[] Rules =
        {
            // ゲーム固有 enum
            (
                new Regex(@"\benum\s+(JobType|RaceType|RankType|Job|Race|Rank)\b",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "ゲーム固有 enum を検知。Job/Race/Rank は JSON の RoleDef/OriginDef/TierDef で定義し、" +
                "C# では string の id で参照してください（CLAUDE.md 禁止事項 §2）。",
                true
            ),

            // 世界観固有の文字列リテラル
            (
                new Regex(
                    "\"(Gold|HP|MP|Warrior|Priest|Mage|Monk|Rogue" +
                    "|Human|Elf|Dwarf|Orc" +
                    "|Copper|Iron|Silver|Mithril|Bronze|Platinum)\"",
                    RegexOptions.Compiled),
                "世界観固有の文字列リテラルを検知。SettingsRegistry.Current の display_name を使用してください" +
                "（CLAUDE.md 禁止事項 §2）。",
                true
            ),

            // ゲームパラメータ定数
            (
                new Regex(
                    @"const\s+\w+\s+\w*(Gold|Share|Reward|Speed|Quest|Guild|Party|Salary|Wage)\w*\s*=",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled),
                "ゲームパラメータの定数を検知。StreamingAssets/settings/ の JSON から読み込んでください" +
                "（CLAUDE.md 禁止事項 §1）。",
                false
            ),

            // テーマ依存ステータス名
            (
                new Regex(
                    @"\b(PhysicalAttack|MagicAttack|HealPower|SwordAttack|MagicPower" +
                    @"|GuildGold|GuildShare|guildGold)\b",
                    RegexOptions.Compiled),
                "テーマ依存のフィールド名を検知。melee_power / ranged_power / support_power / " +
                "OrgCurrency など機能レベルの名称を使用してください（CLAUDE.md 命名規則）。",
                false
            ),

            // マジックナンバー（ゲームパラメータ的な文脈の数値）
            (
                new Regex(
                    @"=\s*(1000|500|200|100|20|0\.2f?)\s*;" +
                    @"|new\s+\w+\([^)]{0,20}(1000|500|200|100)[^)]{0,20}\)",
                    RegexOptions.Compiled),
                "ゲームパラメータと思われる数値リテラルを検知。JSON から読み込んでください" +
                "（CLAUDE.md 禁止事項 §5）。",
                false
            ),
        };

        // ── 検査対象パス ───────────────────────────────────────────────
        // Framework/ のみ。Game/ は世界観固有コードなので除外。
        private static string FrameworkScriptsPath =>
            Path.Combine(Application.dataPath, "Scripts", "Framework");

        // ── 自動実行 ──────────────────────────────────────────────────
        static HardcodeChecker()
        {
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private static void OnProjectChanged()
        {
            // 頻繁に呼ばれる可能性があるため、遅延実行でまとめる
            EditorApplication.delayCall += RunCheck;
        }

        // ── 手動実行メニュー ───────────────────────────────────────────
        [MenuItem("AgentSim/Check Hardcoding Now", priority = 1)]
        public static void RunCheck()
        {
            if (!Directory.Exists(FrameworkScriptsPath))
            {
                Debug.Log($"[HardcodeCheck] Framework scripts フォルダが見つかりません: {FrameworkScriptsPath}");
                return;
            }

            var allFiles = Directory.GetFiles(FrameworkScriptsPath, "*.cs",
                SearchOption.AllDirectories);

            int errorCount = 0;
            int warnCount  = 0;
            var violations = new List<string>();

            foreach (var filePath in allFiles)
            {
                string relPath  = "Assets" + filePath.Replace(Application.dataPath, "").Replace("\\", "/");
                string content  = File.ReadAllText(filePath);
                string[] lines  = content.Split('\n');

                foreach (var (pattern, message, isError) in Rules)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        // コメント部分（// 以降）を除いたコードだけをチェックする
                        // 例: public string x; // "Gold" ← コメントは無視
                        string codePart = StripLineComment(lines[i]);
                        if (!pattern.IsMatch(codePart)) continue;

                        string location = $"{relPath}:{i + 1}";
                        string entry    = $"  {location}\n  → {message}";

                        if (isError)
                        {
                            Debug.LogError($"[HardcodeCheck] {location}\n{message}");
                            errorCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"[HardcodeCheck] {location}\n{message}");
                            warnCount++;
                        }
                        violations.Add(entry);
                    }
                }
            }

            // サマリー
            if (errorCount == 0 && warnCount == 0)
            {
                Debug.Log("[HardcodeCheck] ✓ ハードコーディング違反なし — Framework は正常です");
            }
            else
            {
                Debug.Log(
                    $"[HardcodeCheck] 検査完了: {errorCount} エラー, {warnCount} 警告 " +
                    $"(対象: {allFiles.Length} ファイル)\n" +
                    "詳細は上記のログおよび CLAUDE.md を参照してください。");
            }
        }

        // ── 内部ユーティリティ ────────────────────────────────────────
        /// <summary>
        /// 行コメント（// 以降）を除いたコード部分だけを返す。
        /// 文字列リテラル内の // は誤検知を避けるため簡易的に無視する
        /// （完全な C# パーサーではないが、実用上十分）。
        /// </summary>
        private static string StripLineComment(string line)
        {
            bool inString = false;
            for (int i = 0; i < line.Length - 1; i++)
            {
                char c = line[i];
                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                    inString = !inString;
                if (!inString && c == '/' && line[i + 1] == '/')
                    return line.Substring(0, i);
            }
            return line;
        }

        // ── ルールのリストを表示するデバッグメニュー ─────────────────
        [MenuItem("AgentSim/Show Checker Rules", priority = 2)]
        public static void ShowRules()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[HardcodeCheck] 現在のチェックルール一覧:");
            for (int i = 0; i < Rules.Length; i++)
            {
                var (pattern, message, isError) = Rules[i];
                string level = isError ? "ERROR" : "WARN";
                sb.AppendLine($"  [{i + 1}] [{level}] {message}");
                sb.AppendLine($"       Pattern: {pattern}");
            }
            Debug.Log(sb.ToString());
        }
    }
}

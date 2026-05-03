# AgentSim — Claude グラウンドルール

## プロジェクト概要

**AgentSim** は「JSON を差し替えるだけで世界観が変わる、汎用派遣型企業経営シミュレーションフレームワーク」です。
同じコードで「冒険者ギルド」「傭兵派遣会社」「ロボット派遣企業」など、どんな世界観のゲームも構築できます。

## 最重要ルール：ハードコーディング禁止

C# ファイルを作成・編集するときは、**以下のルールを必ず守ること。**
違反を検知した場合は実装をやり直すこと。

---

## ❌ 禁止事項

### 1. ゲームパラメータの直書き

```csharp
// ❌ 禁止
public int GuildGold = 1000;
const int OrgSharePercent = 20;
float rewardAmount = 500f;
int maxPartySize = 4;
float partySpeed = 2f;
```

→ **すべて JSON から読み込む**

```csharp
// ✅ 正しい
var cfg = SettingsRegistry.Current.Game;
int initialCurrency = cfg.initial_currency;
int orgShare        = cfg.org_share_percent;
int maxParty        = cfg.max_party_size;
```

---

### 2. 世界観固有の名称を C# に書く

```csharp
// ❌ 禁止
enum JobType  { Warrior, Priest, Mage }
enum RaceType { Human, Elf, Dwarf }
enum RankType { Copper, Iron, Silver, Gold, Mithril }
string jobName  = "Warrior";
string currency = "Gold";
string rankName = "Mithril";
```

→ **JSON の `id` / `display_name` として定義し、C# は `string` で参照する**

```csharp
// ✅ 正しい
// id はすべて小文字スネークケース (例: "warrior", "copper", "human")
string roleId      = agent.RoleId;
string displayName = SettingsRegistry.Current.Roles.GetById(roleId).display_name;
```

---

### 3. UI ラベルの直書き

```csharp
// ❌ 禁止
label.text  = "Guild Gold: " + gold + "G";
header.text = "HP / MP";
btn.text    = "Dispatch";
title.text  = "Adventurers";
```

→ **SettingsRegistry の config から取得する**

```csharp
// ✅ 正しい
var cfg = SettingsRegistry.Current.Game;
label.text  = $"{cfg.currency_name}: {gold}{cfg.currency_symbol}";
btn.text    = cfg.dispatch_term;
title.text  = cfg.agents_term;
```

---

### 4. ステータス名・導出式の直書き

```csharp
// ❌ 禁止
int hp       = 20 + vitality * 5;
int physAtk  = 5 + strength * 2;
int magicAtk = magic * 2 + wisdom / 2;
```

→ **`stat_definitions.json` の `formula` フィールドを `FormulaEvaluator` で評価する**

```csharp
// ✅ 正しい
int endurance   = stats.GetDerived("endurance");    // formula: "20 + vitality * 5"
int meleePower  = stats.GetDerived("melee_power");  // formula: "5 + strength * 2"
```

---

### 5. テーマ依存の命名

| ❌ テーマ依存（禁止）     | ✅ 機能レベル（必須）      |
|--------------------------|--------------------------|
| `sword_attack`           | `melee_attack`           |
| `magic_attack`           | `ranged_attack`          |
| `heal_spell`             | `support_action`         |
| `HP` / `MP`              | `endurance` / `focus`    |
| `Gold`                   | `currency`               |
| `Warrior` / `Mage`       | role id `"warrior"` 等    |
| `PhysicalAttack`         | `melee_power`            |
| `MagicAttack`            | `ranged_power`           |
| `HealPower`              | `support_power`          |

表示用の名前（HP, Gold, 戦士 など）はすべて JSON の `display_name` で定義し、Unity UI に渡す。

---

## ✅ 許可される定数・リテラル

- アルゴリズム内部の定数（π、配列インデックス、ビットフラグ等）
- Unity API の定数（Layer 番号、AnimatorHash、SceneIndex 等）
- `FormulaEvaluator` 内の演算子文字 (`+`, `-`, `*`, `/`)
- デバッグ・ログ文字列（ゲーム画面に表示されないもの）
- `Assets/Scripts/Game/` 配下のゲーム固有コード（ここだけ例外）

---

## ファイル配置ルール

```
Assets/Scripts/Framework/   ← フレームワーク本体（ハードコーディング一切禁止）
  Config/                   ← JSON マッピング用 C# クラス (GameConfig, TierConfig …)
  Core/                     ← Agent, Contract, AgentStats, ActiveMission …
  Battle/                   ← HexGrid, BattleUnit, BattlePathfinder（汎用）
  Map/                      ← WorldMapGenerator, MapData, TilemapRenderer
  Party/                    ← PartyController
  Pathfinding/              ← A*, TravelCalculator
  UI/                       ← UIBuilder, 汎用パネル基底クラス
  Systems/                  ← TimeManager

Assets/Scripts/Game/        ← 世界観固有コード（最小限。ここだけ固有名詞OK）

Assets/StreamingAssets/settings/
  adventurer_guild/         ← 冒険者ギルド設定 (game_config.json 等)
  robot_dispatch/           ← ロボット派遣設定
  {任意の setting_id}/      ← 新世界観を追加するときはここにフォルダを追加するだけ
```

---

## 世界観の切り替え方

`GameBootstrap.cs` の `settingId` フィールドを変えるだけ。C# の変更は不要。

```csharp
// GameBootstrap.cs
[SerializeField] private string settingId = "adventurer_guild";
// ↓ これを変えるだけで全 UI・全パラメータが切り替わる
[SerializeField] private string settingId = "robot_dispatch";
```

---

## コードレビューチェックリスト

C# ファイルを作成・編集したら以下を確認する：

- [ ] ゲームパラメータが JSON 経由か (`SettingsRegistry.Current.*`)
- [ ] 世界観固有の `enum` / `const` がないか
- [ ] UI ラベルが `config.*` 経由か
- [ ] ステータス名・導出式を C# に直書きしていないか
- [ ] `melee_attack` / `ranged_attack` / `support_action` の命名規則に従っているか
- [ ] `SettingsRegistry.Current` が `null` の場合の安全な処理があるか
- [ ] Unity メニュー `AgentSim > Check Hardcoding` を実行してエラー/警告がゼロか

---

## JSON ファイル一覧

| ファイル名                  | 内容                                      |
|-----------------------------|-------------------------------------------|
| `game_config.json`          | 組織名、通貨名、各種用語、初期パラメータ  |
| `stat_definitions.json`     | ステータス名 + 導出式 (formula 文字列)    |
| `tiers.json`                | ランク定義 (power profile, reward weight) |
| `roles.json`                | 職業/役割定義 (stat_weights, stat_ranges) |
| `origins.json`              | 出身/種族定義 (stat_bonuses, name_pool)   |
| `contract_templates.json`   | クエスト/案件テンプレート                 |
| `actions.json`              | バトルアクション (melee/ranged/support)   |

すべて `Assets/StreamingAssets/settings/{setting_id}/` に配置する。

---

## 参考：DQSim（旧コード）

旧 DQSim のソースコードは以下のブランチに保存されている（参照のみ）：

```
git checkout archive/dqsim-original
```

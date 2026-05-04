// Assets/Scripts/Framework/Battle/BattleActionResolver.cs
// AgentSim — アクション結果の計算・適用（Unity 非依存、決定論的）

using AgentSim.Config;

namespace AgentSim.Battle
{
    public static class BattleActionResolver
    {
        /// <summary>
        /// アクターの derived stat からダメージ/回復量を計算する。
        /// 将来は ActionDef に variance フィールドを追加することで乱数を導入できる。
        /// </summary>
        public static int Resolve(BattleUnit actor, ActionDef action)
            => actor.Stats.GetDerived(action.primary_stat);

        /// <summary>
        /// 計算済みの量をターゲットに適用する。
        /// support カテゴリは回復、それ以外はダメージ。
        /// </summary>
        public static void Apply(BattleUnit target, ActionDef action, int amount)
        {
            if (action.category == "support")
                target.RestoreHp(amount);
            else
                target.TakeDamage(amount);
        }
    }
}

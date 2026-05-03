// Assets/Scripts/Framework/Core/FormulaEvaluator.cs
// AgentSim — 数式文字列の評価器
//
// stat_definitions.json の formula フィールドを実行時に評価する。
// 対応する文法:
//   - 演算子: + - * / （優先順位あり）
//   - 整数・浮動小数点リテラル
//   - primary stat の id（例: "vitality", "strength"）
//   - 括弧
//
// 例: "20 + vitality * 5"  →  20 + (primary[vitality_index] * 5)
//
// 実装: Shunting-yard アルゴリズム（Dijkstra）
// 依存なし / WebGL 対応 / Unity 6 動作確認済み

using System;
using System.Collections.Generic;
using AgentSim.Config;

namespace AgentSim.Core
{
    public static class FormulaEvaluator
    {
        // ── 公開 API ─────────────────────────────────────────────────
        /// <summary>
        /// formula 文字列を AgentStats を使って評価し、結果を float で返す。
        /// </summary>
        public static float Evaluate(string formula, AgentStats stats, StatDefinitions statDefs)
        {
            var tokens = Tokenize(formula, stats, statDefs);
            var rpn    = ToRPN(tokens);
            return EvaluateRPN(rpn);
        }

        // ── トークン型 ────────────────────────────────────────────────
        private enum TokenKind { Number, Plus, Minus, Multiply, Divide, LParen, RParen }

        private struct Token
        {
            public TokenKind Kind;
            public float     Value; // Kind == Number のときのみ有効
        }

        // ── トークナイズ ──────────────────────────────────────────────
        private static List<Token> Tokenize(string formula, AgentStats stats, StatDefinitions defs)
        {
            var result = new List<Token>();
            int i = 0;
            while (i < formula.Length)
            {
                char c = formula[i];

                // 空白スキップ
                if (char.IsWhiteSpace(c)) { i++; continue; }

                // 数値（整数 or 浮動小数点）
                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.'))
                        i++;
                    float v = float.Parse(formula.Substring(start, i - start),
                        System.Globalization.CultureInfo.InvariantCulture);
                    result.Add(new Token { Kind = TokenKind.Number, Value = v });
                    continue;
                }

                // 識別子（stat id）
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_'))
                        i++;
                    string id = formula.Substring(start, i - start);

                    // primary stat の id か？
                    float statValue = ResolveStatId(id, stats, defs);
                    result.Add(new Token { Kind = TokenKind.Number, Value = statValue });
                    continue;
                }

                // 演算子・括弧
                switch (c)
                {
                    case '+': result.Add(new Token { Kind = TokenKind.Plus });     break;
                    case '-': result.Add(new Token { Kind = TokenKind.Minus });    break;
                    case '*': result.Add(new Token { Kind = TokenKind.Multiply }); break;
                    case '/': result.Add(new Token { Kind = TokenKind.Divide });   break;
                    case '(': result.Add(new Token { Kind = TokenKind.LParen });   break;
                    case ')': result.Add(new Token { Kind = TokenKind.RParen });   break;
                    default:
                        throw new FormatException(
                            $"[FormulaEvaluator] Unexpected character '{c}' in formula: {formula}");
                }
                i++;
            }
            return result;
        }

        private static float ResolveStatId(string id, AgentStats stats, StatDefinitions defs)
        {
            // primary stat の id として解決を試みる
            for (int i = 0; i < defs.primary_stats.Length; i++)
            {
                if (defs.primary_stats[i].id == id)
                    return stats.GetPrimary(i);
            }
            // derived stat の id として解決を試みる（再帰的）
            for (int i = 0; i < defs.derived_stats.Length; i++)
            {
                if (defs.derived_stats[i].id == id)
                    return Evaluate(defs.derived_stats[i].formula, stats, defs);
            }
            throw new ArgumentException(
                $"[FormulaEvaluator] Unknown stat id '{id}'. " +
                "stat_definitions.json の primary_stats または derived_stats に定義されているか確認してください。");
        }

        // ── Shunting-yard → 逆ポーランド記法変換 ─────────────────────
        private static int Precedence(TokenKind op)
        {
            return op == TokenKind.Multiply || op == TokenKind.Divide ? 2 : 1;
        }

        private static bool IsOperator(TokenKind k)
        {
            return k == TokenKind.Plus || k == TokenKind.Minus ||
                   k == TokenKind.Multiply || k == TokenKind.Divide;
        }

        private static List<Token> ToRPN(List<Token> tokens)
        {
            var output = new List<Token>();
            var ops    = new Stack<Token>();

            foreach (var tok in tokens)
            {
                if (tok.Kind == TokenKind.Number)
                {
                    output.Add(tok);
                }
                else if (IsOperator(tok.Kind))
                {
                    while (ops.Count > 0 && IsOperator(ops.Peek().Kind) &&
                           Precedence(ops.Peek().Kind) >= Precedence(tok.Kind))
                        output.Add(ops.Pop());
                    ops.Push(tok);
                }
                else if (tok.Kind == TokenKind.LParen)
                {
                    ops.Push(tok);
                }
                else if (tok.Kind == TokenKind.RParen)
                {
                    while (ops.Count > 0 && ops.Peek().Kind != TokenKind.LParen)
                        output.Add(ops.Pop());
                    if (ops.Count > 0) ops.Pop(); // LParen を捨てる
                }
            }
            while (ops.Count > 0) output.Add(ops.Pop());
            return output;
        }

        // ── RPN 評価 ──────────────────────────────────────────────────
        private static float EvaluateRPN(List<Token> rpn)
        {
            var stack = new Stack<float>();
            foreach (var tok in rpn)
            {
                if (tok.Kind == TokenKind.Number)
                {
                    stack.Push(tok.Value);
                }
                else
                {
                    float b = stack.Pop();
                    float a = stack.Pop();
                    switch (tok.Kind)
                    {
                        case TokenKind.Plus:     stack.Push(a + b); break;
                        case TokenKind.Minus:    stack.Push(a - b); break;
                        case TokenKind.Multiply: stack.Push(a * b); break;
                        case TokenKind.Divide:
                            if (b == 0f) throw new DivideByZeroException("[FormulaEvaluator] Division by zero.");
                            stack.Push(a / b);
                            break;
                    }
                }
            }
            return stack.Count == 1 ? stack.Pop() : 0f;
        }
    }
}

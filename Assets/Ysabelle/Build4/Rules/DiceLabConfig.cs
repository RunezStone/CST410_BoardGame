using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Convergence.Rules
{
    // "2d6", "1d4+2" ...
    [Serializable]
    public struct DiceSpec
    {
        public int count;
        public int sides;
        public int flat;

        public DiceSpec(int count, int sides, int flat = 0)
        {
            this.count = count;
            this.sides = sides;
            this.flat = flat;
        }

        public bool IsNone => count <= 0 && flat == 0;

        public override string ToString()
        {
            if (count <= 0) return flat.ToString();
            return flat == 0 ? $"{count}d{sides}" : $"{count}d{sides}+{flat}";
        }
    }

    // Every number the team set at the Thursday Dice and Chips Probability Lab (GDD v4.0, Section 4.7),
    // in one asset a person can open and read: Assets/Ysabelle/Build4/Resources/DiceLab.asset.
    // A plain-English copy with the computed odds is in Assets/Ysabelle/Build4/DICE_LAB.md.
    // Change a number here (the table) and the build follows; the code never hard-codes these.
    [CreateAssetMenu(menuName = "Convergence/Dice Lab Config", fileName = "DiceLab")]
    public class DiceLabConfig : ScriptableObject
    {
        public const int DefaultSeed = 410;

        [Header("Seed - every random thing in the game comes from this one number")]
        public int seed = DefaultSeed;

        [Header("Block A - the attack roll")]
        [Tooltip("d20 + attack bonus vs the target's Defense. Equal or higher hits.")]
        public int attackDie = 20;
        public bool natural20AlwaysHits = true;
        public bool natural1AlwaysMisses = true;
        [Tooltip("Echo token from a defeated hero: + this to the ally's next d20")]
        public int echoBonus = 1;

        [Header("Minion (Blocks A and B)")]
        [Tooltip("Defense 9: a +2 hero hits on 7+ (14 of 20, 70%)")]
        public int minionDefense = 9;
        [Tooltip("HP 6: 2d4 one-shots on 6 of 16 (37.5%)")]
        public int minionHitPoints = 6;
        public int minionAttackBonus = 1;
        public int minionConstitution = 0;
        public int minionMove = 4;
        public DiceSpec minionBite = new DiceSpec(1, 6);

        [Header("Block B - basic melee damage (Great Sword Attack, Unarmed Strike)")]
        public DiceSpec basicMeleeDamage = new DiceSpec(2, 4);

        [Header("Block C - healing (Heal, Meditation)")]
        public DiceSpec healDice = new DiceSpec(2, 6);
        [Tooltip("Reference target: heal at least this much (beats the largest Bite, 6)")]
        public int healReferenceTarget = 7;

        [Header("Block D - Stunning Strike save")]
        [Tooltip("Target rolls d20 + Constitution. BELOW this = stunned.")]
        public int stunSaveDC = 12;

        [Header("Block E - opening hand")]
        [Tooltip("Keep the basic attack, draw this many of the other cards")]
        public int openingDraw = 2;
        public int handLimit = 5;

        [Header("Generator check (2d6 x N, chi-square)")]
        public int histogramRolls = 200;
        [Tooltip("Chi-square critical value, 10 degrees of freedom, p = 0.05")]
        public float chiSquareCritical = 18.31f;

        // ------------------------------------------------------------------

        static DiceLabConfig cached;

        // Loads Resources/DiceLab.asset. Falls back to the defaults above if the asset is missing.
        public static DiceLabConfig Load()
        {
            if (cached != null) return cached;
            cached = Resources.Load<DiceLabConfig>("DiceLab");
            if (cached == null)
            {
                cached = CreateInstance<DiceLabConfig>();
                Debug.LogWarning("[DiceLab] Resources/DiceLab.asset not found, using built-in defaults.");
            }
            return cached;
        }

        public static void Use(DiceLabConfig config)
        {
            if (config != null) cached = config;
        }

        // ---------------- Odds, computed by counting outcomes ----------------

        // Faces of a d20 that hit: nat 20 always, nat 1 never, else d20 + bonus >= defense.
        public int AttackHitFaces(int attackBonus, int defense)
        {
            int n = 0;
            for (int f = 1; f <= attackDie; f++)
            {
                bool hit = (natural20AlwaysHits && f == attackDie) ||
                           (!(natural1AlwaysMisses && f == 1) && f + attackBonus >= defense);
                if (hit) n++;
            }
            return n;
        }

        // Ordered outcomes of NdS (+flat) that total at least `target`, out of S^N.
        public static (int ways, int total) WaysAtLeast(DiceSpec d, int target)
        {
            var dist = Distribution(d.count, d.sides);
            int ways = 0, total = 0;
            foreach (var kv in dist)
            {
                total += kv.Value;
                if (kv.Key + d.flat >= target) ways += kv.Value;
            }
            return (ways, total);
        }

        // sum -> number of ordered outcomes
        public static SortedDictionary<int, int> Distribution(int count, int sides)
        {
            var dist = new SortedDictionary<int, int> { { 0, 1 } };
            for (int i = 0; i < count; i++)
            {
                var next = new SortedDictionary<int, int>();
                foreach (var kv in dist)
                    for (int f = 1; f <= sides; f++)
                    {
                        next.TryGetValue(kv.Key + f, out int c);
                        next[kv.Key + f] = c + kv.Value;
                    }
                dist = next;
            }
            return dist;
        }

        static long Choose(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            long r = 1;
            for (int i = 1; i <= k; i++) r = r * (n - k + i) / i;
            return r;
        }

        static string Pct(int a, int b) => $"{a} of {b}, {100.0 * a / b:0.0}%";

        // Human-readable table of every dice-lab decision and its probability.
        public string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== DICE LAB (GDD v4.0 Section 4.7)  seed = {seed} ===");
            sb.AppendLine($"A  Attack d{attackDie}+bonus vs Defense (nat 20 hits, nat 1 misses)");
            sb.AppendLine($"     +2 hero vs minion (Def {minionDefense}):   {Pct(AttackHitFaces(2, minionDefense), attackDie)}");
            sb.AppendLine($"     +3 hero vs minion (Def {minionDefense}):   {Pct(AttackHitFaces(3, minionDefense), attackDie)}");
            sb.AppendLine($"     +2 hero vs Villain (Def 12):  {Pct(AttackHitFaces(2, 12), attackDie)}");
            sb.AppendLine($"     minion +{minionAttackBonus} vs Wizard (Def 10):  {Pct(AttackHitFaces(minionAttackBonus, 10), attackDie)}");
            sb.AppendLine($"     minion +{minionAttackBonus} vs Fighter (Def 13): {Pct(AttackHitFaces(minionAttackBonus, 13), attackDie)}");

            var b = WaysAtLeast(basicMeleeDamage, minionHitPoints);
            sb.AppendLine($"B  Basic melee {basicMeleeDamage} >= {minionHitPoints} (one-shot a minion): {Pct(b.ways, b.total)}");

            var c = WaysAtLeast(healDice, healReferenceTarget);
            sb.AppendLine($"C  Heal {healDice} >= {healReferenceTarget}: {Pct(c.ways, c.total)}");

            int stunMinion = 0, stunVillain = 0;
            for (int f = 1; f <= 20; f++)
            {
                if (f + minionConstitution < stunSaveDC) stunMinion++;
                if (f + 1 < stunSaveDC) stunVillain++;
            }
            sb.AppendLine($"D  Stunning Strike, stunned if d20+Con < DC {stunSaveDC}: minion {Pct(stunMinion, 20)}, Villain {Pct(stunVillain, 20)}");

            // Fighter: 5 non-basic cards, 2 of them heal (Second Wind, Health Pot)
            long none = Choose(3, openingDraw), all = Choose(5, openingDraw);
            sb.AppendLine($"E  Opening hand: basic + draw {openingDraw} of 5; Fighter holds a heal: {all - none} of {all}, {100.0 * (all - none) / all:0.0}%");
            return sb.ToString();
        }

        // Rolls 2d6 `histogramRolls` times on its own DiceService (same seed, so the game's
        // sequence is untouched) and compares with the expected 1-2-3-4-5-6-5-4-3-2-1 shape.
        public string RunHistogramCheck(out bool passed)
        {
            var dice = new DiceService(seed);
            int[] observed = new int[13];
            for (int i = 0; i < histogramRolls; i++) observed[dice.Roll(2, 6)]++;

            var sb = new StringBuilder($"2d6 x {histogramRolls} generator check (seed {seed})\n");
            sb.AppendLine("Total    :  " + Row(t => t.ToString()));
            sb.AppendLine("Expected :  " + Row(t => (histogramRolls * (6 - Mathf.Abs(t - 7)) / 36.0).ToString("0.0")));
            sb.AppendLine("Observed :  " + Row(t => observed[t].ToString()));

            double chi = 0;
            for (int t = 2; t <= 12; t++)
            {
                double e = histogramRolls * (6 - Mathf.Abs(t - 7)) / 36.0;
                chi += (observed[t] - e) * (observed[t] - e) / e;
            }
            passed = chi < chiSquareCritical;
            sb.Append($"Chi-square = {chi:0.00} (fail at {chiSquareCritical}) -> {(passed ? "PASS" : "FAIL")}");
            return sb.ToString();
        }

        static string Row(Func<int, string> cell)
        {
            var sb = new StringBuilder();
            for (int t = 2; t <= 12; t++) sb.Append(cell(t).PadLeft(6));
            return sb.ToString();
        }
    }
}

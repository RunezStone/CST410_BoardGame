using System.Collections.Generic;

namespace Convergence.Rules
{
    // The ONE place randomness comes from. Every die roll, shuffle, placeholder decision and
    // physical-dice throw force goes through DiceService.Main, which is seeded from a single seed
    // (DiceLab.asset -> seed). Same seed + same decisions = same game, so a run can be repeated.
    //
    // UnityEngine.Random and System.Random must not be used anywhere else in game code.
    public sealed class DiceService
    {
        static DiceService main;

        // Lazily seeded from the Dice Lab config the first time anything rolls.
        public static DiceService Main => main ??= new DiceService(DiceLabConfig.Load().seed);

        // Start a fresh, repeatable sequence (the driver calls this once at startup).
        public static void Reseed(int seed)
        {
            main = new DiceService(seed);
            UnityEngine.Debug.Log($"[DiceService] Seeded with {seed}.");
        }

        readonly System.Random rng;
        public int Seed { get; }
        public int RollCount { get; private set; }

        public DiceService(int seed)
        {
            Seed = seed;
            rng = new System.Random(seed);
        }

        // One die, 1..sides.
        public int Roll(int sides)
        {
            if (sides < 1) return 0;
            RollCount++;
            return rng.Next(1, sides + 1);
        }

        // count d sides, summed. Faces are written into `faces` if given.
        public int Roll(int count, int sides, List<int> faces = null)
        {
            int total = 0;
            for (int i = 0; i < count; i++)
            {
                int f = Roll(sides);
                faces?.Add(f);
                total += f;
            }
            return total;
        }

        public int Roll(DiceSpec spec, List<int> faces = null) => Roll(spec.count, spec.sides, faces) + spec.flat;

        // Integer in [minInclusive, maxExclusive).
        public int Range(int minInclusive, int maxExclusive) => rng.Next(minInclusive, maxExclusive);

        // Float in [min, max). Used for the physical dice throw forces.
        public float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        public T Pick<T>(IList<T> list) => list[rng.Next(list.Count)];

        // Fisher-Yates shuffle (opening hands).
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

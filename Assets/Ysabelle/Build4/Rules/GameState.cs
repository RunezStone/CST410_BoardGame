using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Rules
{
    public enum Role { Fighter, Wizard, Monk, Cleric, Villain, Minion }
    public enum Faction { Heroes, Villain }

    // How a card finds its targets (GDD v4.0 Section 4.1 "Range, lines, and adjacency").
    public enum Targeting
    {
        Adjacent,            // one character on the 8 squares around you
        AllAdjacent,         // everyone on the 8 squares around you (Roundhouse Kick)
        Range,               // one character within N orthogonal steps (ignores walls)
        Line,                // straight line, stops at the first character (Bow Shot, Force Blast)
        LinePierce,          // straight line, passes through characters (Bubble Beam) - pick a direction
        Self,
        Ally,                // one ally within N (0 = anywhere)
        Enemy,               // one enemy within N
        EmptySquare,         // teleport to an empty square within N
        AllLivingHeroes      // Mass Heal
    }

    public enum EffectTick { StartOfTurn, EndOfTurn }

    public class CardDef
    {
        public string name;
        public PlayerAction verb;
        public int cost;
        public Targeting targeting;
        public int range;
        public bool isBasic;

        public DiceSpec damage;
        public int push;
        public bool arcsToAdjacent;      // Lightning Strike

        public DiceSpec heal;
        public bool allyIncludesSelf;    // Heal can target the Cleric too
        public bool singleUse;           // Health Pot

        public string buffName;          // Support buffs and Judgement
        public int buffTurns;
        public int buffDamage, buffMove, buffDefense;
        public EffectTick buffTick = EffectTick.EndOfTurn;
        public bool buffStacksRefresh = true;   // false = can't be played while active (Rage)

        public bool stun;                // Stunning Strike
        public int restorePoints;        // Short Rest
        public bool swap;                // Swap

        public CardDef Clone() => (CardDef)MemberwiseClone();
    }

    // A timed token on a character (Rage 3, Surge 1, Defensive Stance 3, Haste 3, Frozen 2).
    public class Effect
    {
        public string name;
        public int counter;
        public EffectTick tick;
        public int damage, move, defense;
        public bool frozen;
    }

    public class Character
    {
        public string name;
        public Role role;
        public Faction faction;

        public int maxHp, hp, baseDefense, attackBonus, con, baseMove, points, maxPoints;

        public Vector2Int pos;
        public bool onBoard = true;

        public List<CardDef> hand = new List<CardDef>();
        public List<CardDef> deck = new List<CardDef>();   // cards not in hand = this hero's draft pool
        public List<Effect> effects = new List<Effect>();

        // Turn state (the action token and the move)
        public bool actionReady;
        public bool hasMoved;
        public bool stunned;           // Stun token
        public bool frozenThisTurn;    // Judgement: can't Move or Reposition this turn
        public int echoBonus;          // Echo token: + to next d20

        public bool IsDefeated => hp <= 0;
        public bool IsHero => faction == Faction.Heroes;
        public bool IsMinion => role == Role.Minion;
        public bool IsActive => onBoard && !IsDefeated;

        public int DamageBonus { get { int n = 0; foreach (var e in effects) n += e.damage; return n; } }
        public int Movement { get { int n = baseMove; foreach (var e in effects) n += e.move; return n; } }
        public int Defense { get { int n = baseDefense; foreach (var e in effects) n += e.defense; return n; } }

        public Effect GetEffect(string effectName) => effects.Find(e => e.name == effectName);
        public bool HasEffect(string effectName) => GetEffect(effectName) != null;
        public bool IsAllyOf(Character other) => other != null && other.faction == faction;

        public string Status() =>
            $"{name} HP {hp}/{maxHp}  pts {points}/{maxPoints}  at {GameAction.Sq(pos)}" +
            (effects.Count > 0 ? "  [" + string.Join(", ", effects.ConvertAll(e => $"{e.name} {e.counter}")) + "]" : "");
    }

    public class GameState
    {
        public const int Columns = 10, Rows = 15;
        public static readonly string[] Terrains = { "Plains", "Castle", "Volcano" };

        public readonly bool[,] blocked = new bool[Columns, Rows];
        public readonly List<Character> characters = new List<Character>();   // heroes, Villain, minions on the board
        public readonly List<Character> turnOrder = new List<Character>();    // Fighter, Villain, Wizard, Villain, Monk, Villain, Cleric, Villain

        public int turnIndex;
        public int round = 1;                   // round marker, 1..4 (passes 4 -> phase ends)
        public int villainTurnsThisRound;
        public int phaseNumber = 1;
        public int roundsCompletedTotal;        // across phases, for the driver
        public int turnsTaken;

        public int minionReserve;
        public int minionGraveyard;
        public int bonusReserveNextPhase;      // Villain draft: +2 minions
        int minionSerial;

        public Faction? previousPhaseLoser;
        public bool gameOver;
        public Faction? winner;

        // Draft bookkeeping (Check phase)
        public int heroDraftPicksLeft, villainDraftPicksLeft;
        public readonly Dictionary<Character, int> heroPicksThisDraft = new Dictionary<Character, int>();

        public DiceLabConfig lab;
        public DiceService dice;

        public string Terrain => Terrains[Mathf.Clamp(phaseNumber - 1, 0, 2)];
        public Character CurrentActor => turnOrder[turnIndex];
        public Character Villain => characters.Find(c => c.role == Role.Villain);
        public List<Character> Heroes => characters.FindAll(c => c.IsHero);
        public List<Character> MinionsOnBoard => characters.FindAll(c => c.IsMinion && c.IsActive);
        public bool DraftPending => heroDraftPicksLeft > 0 || villainDraftPicksLeft > 0;

        public GameState(DiceLabConfig lab, DiceService dice)
        {
            this.lab = lab;
            this.dice = dice;
        }

        // ---------------- Board ----------------

        public static bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Columns && p.y < Rows;
        public bool IsBlocked(Vector2Int p) => !InBounds(p) || blocked[p.x, p.y];

        public Character CharacterAt(Vector2Int p)
        {
            foreach (var c in characters)
                if (c.IsActive && c.pos == p) return c;
            return null;
        }

        public bool IsEmpty(Vector2Int p) => InBounds(p) && !IsBlocked(p) && CharacterAt(p) == null;

        public static int Orthogonal(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        public static bool Adjacent(Vector2Int a, Vector2Int b) =>
            a != b && Mathf.Abs(a.x - b.x) <= 1 && Mathf.Abs(a.y - b.y) <= 1;

        public static readonly Vector2Int[] Straight = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public IEnumerable<Vector2Int> AdjacentSquares(Vector2Int p)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var q = new Vector2Int(p.x + dx, p.y + dy);
                    if ((dx != 0 || dy != 0) && InBounds(q)) yield return q;
                }
        }

        // Orthogonal BFS; may not enter or pass through blocked or occupied squares.
        public List<Vector2Int> Reachable(Vector2Int from, int steps)
        {
            var result = new List<Vector2Int>();
            var dist = new Dictionary<Vector2Int, int> { { from, 0 } };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (dist[p] == steps) continue;
                foreach (var d in Straight)
                {
                    var q = p + d;
                    if (dist.ContainsKey(q) || !IsEmpty(q)) continue;
                    dist[q] = dist[p] + 1;
                    result.Add(q);
                    queue.Enqueue(q);
                }
            }
            result.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            return result;
        }

        // Squares along a straight line, stopping at walls / the edge.
        public List<Vector2Int> LineSquares(Vector2Int from, Vector2Int dir, int range)
        {
            var result = new List<Vector2Int>();
            for (int step = 1; step <= range; step++)
            {
                var q = from + dir * step;
                if (IsBlocked(q)) break;
                result.Add(q);
            }
            return result;
        }

        public Character NearestEnemy(Character c)
        {
            Character best = null;
            int bestDist = int.MaxValue;
            foreach (var o in characters)
            {
                if (!o.IsActive || o.IsAllyOf(c)) continue;
                int d = Orthogonal(c.pos, o.pos);
                if (d < bestDist) { bestDist = d; best = o; }
            }
            return best;
        }

        public Character SpawnMinion(Vector2Int at)
        {
            var m = new Character
            {
                name = $"Minion {++minionSerial}",
                role = Role.Minion,
                faction = Faction.Villain,
                maxHp = lab.minionHitPoints,
                hp = lab.minionHitPoints,
                baseDefense = lab.minionDefense,
                attackBonus = lab.minionAttackBonus,
                con = lab.minionConstitution,
                baseMove = lab.minionMove,
                pos = at
            };
            characters.Add(m);
            return m;
        }
    }
}

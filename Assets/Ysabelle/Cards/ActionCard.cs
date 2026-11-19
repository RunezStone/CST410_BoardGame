using UnityEngine;
using System.Collections.Generic;

// Base class for every card. Holds the shared rules (cost, attack roll, range search)
// so each card script only describes what is different about that card.
public abstract class ActionCard : MonoBehaviour
{
    public string cardName;
    [TextArea] public string cardDescription;

    [Tooltip("Points this card costs (Fighter = action points, Monk = focus, Warlock = spell points, etc.)")]
    public int points;

    [Tooltip("Does playing this card use up the character's action for the turn?")]
    public bool costsAction = true;

    // Hook your card's UI button to THIS, not to InitiateAction.
    public void Play(PlayerManager player)
    {
        if (!CanPlay(player)) return;
        InitiateAction(player);
    }

    // Start the card: either do it right away, or enter targeting mode.
    public abstract void InitiateAction(PlayerManager playerManager);

    // ---------------- Cost ----------------

    protected bool CanPlay(PlayerManager p)
    {
        if (!p.IsTurn) return false;
        if (costsAction && p.ActionsLeft <= 0)
        {
            Debug.Log($"{p.name} has no actions left this turn.");
            return false;
        }
        if (p.playerData.actionPoints < points)
        {
            Debug.Log($"{p.name} needs {points} point(s) for {cardName}.");
            return false;
        }
        return true;
    }

    // Call this at the moment the card actually happens, NOT in InitiateAction:
    // the player can still press Escape while targeting, and that shouldn't cost anything.
    protected bool TrySpend(PlayerManager p)
    {
        if (!CanPlay(p)) return false;
        if (costsAction) p.UseAction();
        p.playerData.actionPoints -= points;
        return true;
    }

    // ---------------- Attacks ----------------

    // Hit rule (CLAUDE.md §2 Attacks): d20 + attackBonus vs the target's defense.
    //   total > defense  -> FULL hit: full damage, and on-hit effects (push, stun, arc...) happen.
    //   otherwise        -> MISS: half damage (rounded down, minimum 1). No on-hit effects.
    //   natural 20 is always a full hit; natural 1 is always a miss. Total == defense is a miss.
    // Damage = flatDamage + (damageDiceCount d damageDieSides) + attacker's damageBonus (e.g. Rage).
    // onHit runs after the damage lands, for FULL hits only (used by Force Blast to push).
    // Static so the dice callbacks never depend on the card object still existing.
    protected static void ResolveAttack(PlayerManager attacker, PlayerManager target, int flatDamage, int damageDieSides,
                                        int damageDiceCount = 1, System.Action onHit = null)
    {
        ResolveAreaAttack(attacker, new List<PlayerManager> { target }, flatDamage, damageDieSides, damageDiceCount,
                          onHit == null ? null : (System.Action<PlayerManager, int>)((t, dmg) => onHit()));
    }

    // Same rules, several targets at once (Bubble Beam, Roundhouse Kick...).
    // ONE d20 is rolled and compared to each target's own defense, then ONE damage roll:
    // full hits take `damage`, misses take HalfDamage(damage). All dice are thrown once, not once per target.
    // onHit(target, damage) runs for each FULL hit, in list order, after all damage lands.
    protected static void ResolveAreaAttack(PlayerManager attacker, List<PlayerManager> targets, int flatDamage, int damageDieSides,
                                            int damageDiceCount = 1, System.Action<PlayerManager, int> onHit = null)
    {
        if (targets == null || targets.Count == 0) return;

        RollDie(20, d20 =>
        {
            List<PlayerManager> fullHits = new List<PlayerManager>();
            List<PlayerManager> halfHits = new List<PlayerManager>();

            foreach (PlayerManager target in targets)
            {
                if (IsFullHit(attacker, target, d20)) fullHits.Add(target);
                else halfHits.Add(target);
            }

            int extra = flatDamage + attacker.playerData.damageBonus;
            int diceCount = damageDieSides > 0 ? damageDiceCount : 0;   // RollDice(0, ...) reports 0 instantly

            RollDice(diceCount, damageDieSides, roll =>
            {
                int damage = roll + extra;
                int half = HalfDamage(damage);
                foreach (PlayerManager t in fullHits) ApplyDamage(attacker, t, damage);
                foreach (PlayerManager t in halfHits) ApplyDamage(attacker, t, half, "grazes");
                foreach (PlayerManager t in fullHits) onHit?.Invoke(t, damage);
            });
        });
    }

    // Compares an already-rolled d20 against one target and logs the result.
    // Use this when a card needs its own damage handling (e.g. Absorb) but the standard hit rule.
    protected static bool IsFullHit(PlayerManager attacker, PlayerManager target, int d20)
    {
        int bonus = attacker.playerData.attackBonus;
        int total = d20 + bonus;
        int defense = target.playerData.defense;

        bool full;
        if (d20 == 20) full = true;          // natural 20: always a full hit
        else if (d20 == 1) full = false;     // natural 1: always a miss
        else full = total > defense;         // must BEAT defense; equal is a miss

        Debug.Log($"{attacker.name} attacks {target.name}: {d20} + {bonus} = {total} vs Defense {defense} -> " +
                  $"{(full ? "HIT (full damage)" : "MISS (half damage)")}");
        return full;
    }

    // Damage a miss does: half, rounded down, but never 0 ("a miss never does 0").
    protected static int HalfDamage(int damage)
    {
        if (damage <= 0) return 0;
        return Mathf.Max(1, damage / 2);
    }

    // ---------------- Saving throws ----------------

    protected enum SaveStat { Brain, Constitution, Athletics }

    protected static int GetSaveStat(PlayerManager target, SaveStat stat)
    {
        PlayerData d = target.playerData;
        return stat switch
        {
            SaveStat.Brain => d.brain,
            SaveStat.Constitution => d.constitution,
            SaveStat.Athletics => d.athletics,
            _ => 0
        };
    }

    // The target rolls d20 + stat. The save FAILS if the total is below `dc` (Cards.xlsx: "stat < DC").
    // For an "or lower" rule like Athletics <= 12, pass dc = 13.
    protected static void RollSave(PlayerManager target, SaveStat stat, int dc, System.Action onFail, System.Action onSuccess = null)
    {
        RollDie(20, d20 =>
        {
            int mod = GetSaveStat(target, stat);
            int total = d20 + mod;
            bool failed = total < dc;
            Debug.Log($"{target.name} {stat} check: {d20} + {mod} = {total} vs DC {dc} -> {(failed ? "FAILED" : "RESISTED")}");
            if (failed) onFail?.Invoke();
            else onSuccess?.Invoke();
        });
    }

    protected static void ApplyDamage(PlayerManager attacker, PlayerManager target, int damage, string verb = "hits")
    {
        target.playerData.TakeDamage(damage);
        Debug.Log($"{attacker.name} {verb} {target.name} for {damage}! ({target.playerData.hitPoints} HP left)");
    }

    // ---------------- Ally targeting ----------------

    // Squares of living allies within range (plus the caster's own square if includeSelf).
    // Used by support cards (Heal, Haste, Beacon of Hope, Restore...).
    protected static List<Space> GetAllySpacesInRange(PlayerManager caster, int range, bool includeDiagonals, bool includeSelf)
    {
        List<Space> spaces = new List<Space>();
        foreach (Space s in GetSpacesInRange(caster.currSpace, range, includeDiagonals))
        {
            PlayerManager p = FindCharacterOn(s, caster);
            if (caster.IsAllyOf(p) && !p.playerData.IsDead) spaces.Add(s);
        }
        if (includeSelf) spaces.Add(caster.currSpace);
        return spaces;
    }

    // The character a support card was aimed at: the caster if they clicked their own square.
    protected static PlayerManager CharacterOnOrSelf(Space space, PlayerManager caster)
    {
        return space == caster.currSpace ? caster : FindCharacterOn(space, caster);
    }

    // Single die: same as RollDice with a count of 1.
    protected static void RollDie(int sides, System.Action<int> onResult)
    {
        RollDice(1, sides, onResult);
    }

    // Throws `count` dice at the same time (e.g. 4d4) and reports the total once they all land.
    // Physical dice when there's a prefab for that type, otherwise a virtual roll.
    protected static void RollDice(int count, int sides, System.Action<int> onTotal)
    {
        if (count <= 0) { onTotal(0); return; }

        int type = sides switch { 4 => 0, 6 => 1, 8 => 2, 12 => 3, 20 => 4, _ => -1 };

        SpaceButtonRoll roller = Object.FindAnyObjectByType<SpaceButtonRoll>();
        DiceScript dice = roller != null ? roller.diceScript : null;

        if (dice != null && dice.RollMany(type, count, faces =>
        {
            int total = 0;
            foreach (int f in faces) total += f;
            if (count > 1) Debug.Log($"{count}d{sides}: {string.Join(" + ", faces)} = {total}");
            onTotal(total);
        }))
            return;   // onTotal runs when the dice land

        int sum = 0;
        sum = Convergence.Rules.DiceService.Main.Roll(count, sides);   // one seeded service for all randomness
        onTotal(sum);
    }

    // ---------------- Board helpers ----------------
    // These use each Space's grid id (id[0] = column, id[1] = row) from TileManager,
    // so there's no tile-spacing number to keep in sync.

    // Any character (ally OR enemy -- friendly fire is a feature) standing on a space.
    protected static PlayerManager FindCharacterOn(Space space, PlayerManager ignore = null)
    {
        foreach (PlayerManager p in Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
            if (p != ignore && p.currSpace == space) return p;
        return null;
    }

    // The Space at grid (x, y), or null if that's off the board.
    protected static Space GetSpaceAt(int x, int y)
    {
        GameObject[,] grid = TileManager.instance.spaces;
        if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1)) return null;
        return grid[x, y] != null ? grid[x, y].GetComponent<Space>() : null;
    }

    // Spaces within `range` squares of center (ignores walls/blockers).
    // includeDiagonals: true = a diagonal step counts as 1, false = only up/down/left/right count.
    protected static List<Space> GetSpacesInRange(Space center, int range, bool includeDiagonals, bool emptyOnly = false)
    {
        List<Space> result = new List<Space>();
        int cx = center.id[0], cy = center.id[1];

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int dist = includeDiagonals ? Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) : Mathf.Abs(dx) + Mathf.Abs(dy);
                if (dist < 1 || dist > range) continue;

                Space s = GetSpaceAt(cx + dx, cy + dy);
                if (s == null) continue;
                if (emptyOnly && (s.IsBlocked() || s.CharacterExistsInSpace())) continue;
                result.Add(s);
            }
        }
        return result;
    }

    protected static readonly Vector2Int[] StraightDirections =
        { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
    protected static readonly Vector2Int[] DiagonalDirections =
        { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };

    // Walks outward from center along each direction, up to `range` squares.
    // A line stops at the board edge or a blocked tile (not targetable),
    // and at the first character (targetable -- you can shoot them, but not past them).
    // stopAtCharacters = false lets the line pass through people (beams like Bubble Beam).
    protected static List<Space> GetSpacesInLines(Space center, int range, Vector2Int[] directions, bool stopAtCharacters = true)
    {
        List<Space> result = new List<Space>();
        int cx = center.id[0], cy = center.id[1];

        foreach (Vector2Int dir in directions)
        {
            for (int step = 1; step <= range; step++)
            {
                Space s = GetSpaceAt(cx + dir.x * step, cy + dir.y * step);
                if (s == null || s.IsBlocked()) break;   // edge of board or a wall
                result.Add(s);
                if (stopAtCharacters && s.CharacterExistsInSpace()) break;   // can't shoot past someone
            }
        }
        return result;
    }

    // One grid step from `from` toward `to`, e.g. (1,0) or (1,-1). (0,0) if they're the same space.
    // Uses System.Math.Sign because Mathf.Sign(0) returns 1.
    protected static Vector2Int DirectionBetween(Space from, Space to)
    {
        return new Vector2Int(System.Math.Sign(to.id[0] - from.id[0]),
                              System.Math.Sign(to.id[1] - from.id[1]));
    }

    // Shoves a character up to `squares` spaces along `dir`.
    // Stops early at the board edge, a blocked tile, or another character (no damage for hitting them).
    // Does not use up the target's normal move.
    protected static void PushCharacter(PlayerManager target, Vector2Int dir, int squares)
    {
        if (target == null || target.playerData.IsDead || dir == Vector2Int.zero) return;

        Space start = target.currSpace;
        Space dest = start;

        for (int step = 0; step < squares; step++)
        {
            Space next = GetSpaceAt(dest.id[0] + dir.x, dest.id[1] + dir.y);
            if (next == null || next.IsBlocked() || next.CharacterExistsInSpace()) break;
            dest = next;
        }

        if (dest == start)
        {
            Debug.Log($"{target.name} is pinned and can't be pushed.");
            return;
        }

        target.Teleport(dest, forced: true);   // being shoved still works while Frozen
        Debug.Log($"{target.name} is pushed back to [{dest.id[0]}, {dest.id[1]}].");
    }

    // For one-shot cards like Health Pot. TODO: also remove it from CardInventory once that API exists.
    protected void ConsumeCard()
    {
        Destroy(gameObject);
    }
}
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

    // d20 + attackBonus vs defense. Nat 20 always hits, nat 1 always misses.
    // Damage = flatDamage + (damageDiceCount d damageDieSides) + attacker's damageBonus (e.g. Rage).
    // onHit runs after the damage lands (used by Force Blast to push).
    // Static so the dice callbacks never depend on the card object still existing.
    protected static void ResolveAttack(PlayerManager attacker, PlayerManager target, int flatDamage, int damageDieSides,
                                        int damageDiceCount = 1, System.Action onHit = null)
    {
        ResolveAreaAttack(attacker, new List<PlayerManager> { target }, flatDamage, damageDieSides, damageDiceCount,
                          onHit == null ? null : (System.Action<PlayerManager, int>)((t, dmg) => onHit()));
    }

    // Same rules, several targets at once (Bubble Beam, Roundhouse Kick...).
    // ONE d20 is rolled and compared to each target's own defense, then ONE damage roll
    // is applied to everyone it hit -- so all the dice are thrown once, not once per target.
    // onHit(target, damage) runs for each target hit, in list order, after all damage lands.
    protected static void ResolveAreaAttack(PlayerManager attacker, List<PlayerManager> targets, int flatDamage, int damageDieSides,
                                            int damageDiceCount = 1, System.Action<PlayerManager, int> onHit = null)
    {
        if (targets == null || targets.Count == 0) return;

        RollDie(20, d20 =>
        {
            int bonus = attacker.playerData.attackBonus;
            int total = d20 + bonus;
            List<PlayerManager> hits = new List<PlayerManager>();

            foreach (PlayerManager target in targets)
            {
                int defense = target.playerData.defense;
                bool hit = d20 == 20 || (d20 != 1 && total >= defense);
                Debug.Log($"{attacker.name} attacks {target.name}: {d20} + {bonus} = {total} vs Defense {defense} -> {(hit ? "HIT" : "MISS")}");
                if (hit) hits.Add(target);
            }
            if (hits.Count == 0) return;

            int extra = flatDamage + attacker.playerData.damageBonus;
            int diceCount = damageDieSides > 0 ? damageDiceCount : 0;   // RollDice(0, ...) reports 0 instantly

            RollDice(diceCount, damageDieSides, roll =>
            {
                int damage = roll + extra;
                foreach (PlayerManager t in hits) ApplyDamage(attacker, t, damage);
                foreach (PlayerManager t in hits) onHit?.Invoke(t, damage);
            });
        });
    }

    protected static void ApplyDamage(PlayerManager attacker, PlayerManager target, int damage)
    {
        target.playerData.TakeDamage(damage);
        Debug.Log($"{attacker.name} hits {target.name} for {damage}! ({target.playerData.hitPoints} HP left)");
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
        for (int i = 0; i < count; i++) sum += Random.Range(1, sides + 1);
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

        target.Teleport(dest);
        Debug.Log($"{target.name} is pushed back to [{dest.id[0]}, {dest.id[1]}].");
    }

    // For one-shot cards like Health Pot. TODO: also remove it from CardInventory once that API exists.
    protected void ConsumeCard()
    {
        Destroy(gameObject);
    }
}
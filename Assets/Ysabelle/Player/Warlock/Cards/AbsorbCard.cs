using UnityEngine;
using System.Collections.Generic;

// Absorb (2 pts): drain 1d6 HP from every character on the 8 squares around the Villain
// (rolled separately for each). Enemies get an attack roll first (one d20 vs each Defense:
// full hit = full drain, miss = half). Allies (minions) take it with no roll.
// The Villain heals the total HP actually taken, capped at max HP.
public class AbsorbCard : ActionCard
{
    [SerializeField] private int drainDieSides = 6;
    [SerializeField] private int drainDiceCount = 1;
    [Tooltip("true = all 8 surrounding squares (Cards.xlsx: Adjacent, 8 sq)")]
    [SerializeField] private bool includeDiagonals = true;

    void Reset() { points = 2; }

    public override void InitiateAction(PlayerManager player)
    {
        List<PlayerManager> targets = new List<PlayerManager>();
        foreach (Space s in GetSpacesInRange(player.currSpace, 1, includeDiagonals))
        {
            PlayerManager p = FindCharacterOn(s, player);
            if (p != null && !p.playerData.IsDead) targets.Add(p);   // nothing to drain from a downed character
        }

        if (targets.Count == 0) { Debug.Log("Nobody close enough to drain."); return; }   // free
        if (!TrySpend(player)) return;

        int sides = drainDieSides, count = drainDiceCount;

        RollDie(20, d20 =>
        {
            // full = no roll needed (ally) or a full hit; false = a miss (half drain)
            List<bool> full = new List<bool>();
            foreach (PlayerManager t in targets)
                full.Add(player.IsAllyOf(t) || IsFullHit(player, t, d20));

            DrainNext(player, targets, full, 0, 0, sides, count);
        });
    }

    // Rolls each target's drain one after another (physical dice land one set at a time).
    static void DrainNext(PlayerManager villain, List<PlayerManager> targets, List<bool> full, int index,
                          int totalTaken, int sides, int count)
    {
        if (index >= targets.Count)
        {
            int healed = villain.playerData.Heal(totalTaken);
            Debug.Log($"{villain.name} absorbs {totalTaken} HP and heals {healed} ({villain.playerData.hitPoints} HP).");
            return;
        }

        PlayerManager t = targets[index];
        RollDice(count, sides, roll =>
        {
            int drain = roll + villain.playerData.damageBonus;
            if (!full[index]) drain = HalfDamage(drain);

            int before = t.playerData.hitPoints;
            ApplyDamage(villain, t, drain, full[index] ? "drains" : "partly drains");
            int taken = before - t.playerData.hitPoints;

            DrainNext(villain, targets, full, index + 1, totalTaken + taken, sides, count);
        });
    }
}

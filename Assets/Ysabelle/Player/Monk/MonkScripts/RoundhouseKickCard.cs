using UnityEngine;
using System.Collections.Generic;

// Roundhouse Kick (2 focus points): hits EVERYONE on the 8 squares around the monk for 2d12.
// Friendly fire included -- check where your allies are standing first.
// One d20 is compared against each target's defense; everyone it beats takes the damage.
// No targeting: it fires as soon as you play it.
public class RoundhouseKickCard : ActionCard
{
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 12;
    [SerializeField] private int damageDiceCount = 2;
    [Tooltip("true = all 8 surrounding squares, false = only up/down/left/right")]
    [SerializeField] private bool includeDiagonals = true;

    void Reset() { points = 2; }

    public override void InitiateAction(PlayerManager player)
    {
        List<PlayerManager> targets = new List<PlayerManager>();
        foreach (Space s in GetSpacesInRange(player.currSpace, 1, includeDiagonals))
        {
            PlayerManager p = FindCharacterOn(s, player);
            if (p != null) targets.Add(p);
        }

        if (targets.Count == 0) { Debug.Log("Nobody close enough to kick."); return; }   // free
        if (!TrySpend(player)) return;

        Debug.Log($"{player.name} spins into a roundhouse kick!");
        ResolveAreaAttack(player, targets, flatDamage, damageDieSides, damageDiceCount);
    }
}
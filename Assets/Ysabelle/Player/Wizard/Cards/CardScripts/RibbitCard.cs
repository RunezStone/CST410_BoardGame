using UnityEngine;
using System.Collections.Generic;

// Ribbit (3 pts): a loud croak. Every character on the 8 squares around the Wizard
// (allies included) is attacked for 3d6. One d20 against each Defense, one damage roll.
public class RibbitCard : ActionCard
{
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 6;
    [SerializeField] private int damageDiceCount = 3;
    [Tooltip("true = all 8 surrounding squares (Cards.xlsx: Adjacent, 8 sq)")]
    [SerializeField] private bool includeDiagonals = true;

    void Reset() { points = 3; }

    public override void InitiateAction(PlayerManager player)
    {
        List<PlayerManager> targets = new List<PlayerManager>();
        foreach (Space s in GetSpacesInRange(player.currSpace, 1, includeDiagonals))
        {
            PlayerManager p = FindCharacterOn(s, player);
            if (p != null) targets.Add(p);
        }

        if (targets.Count == 0) { Debug.Log("Nobody close enough to hear the croak."); return; }   // free
        if (!TrySpend(player)) return;

        Debug.Log($"{player.name} lets out a deafening RIBBIT!");
        ResolveAreaAttack(player, targets, flatDamage, damageDieSides, damageDiceCount);
    }
}

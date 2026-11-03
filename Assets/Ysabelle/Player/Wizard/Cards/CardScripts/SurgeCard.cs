using UnityEngine;
using System.Collections.Generic;

// Surge (1 spell point): boost an ALLY's attacks by +3 damage.
// Only teammates in range light up red, so you can't surge an enemy by accident.
// Lasts until the end of that ally's next turn (durationTurns = 1), so they get to use it.
// Casting it again on someone already surged just refreshes the duration -- it doesn't stack.
public class SurgeCard : ActionCard
{
    [Tooltip("GDD doesn't give a range. 6 is a placeholder -- change it once the team decides.")]
    [SerializeField] private int range = 6;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int damageBonus = 3;
    [Tooltip("Counted on the TARGET's turns. 1 = through their next turn.")]
    [SerializeField] private int durationTurns = 1;
    [SerializeField] private bool canTargetSelf = false;

    void Reset() { points = 1; }   // default cost when the script is first added

    public override void InitiateAction(PlayerManager player)
    {
        List<Space> spaces = new List<Space>();

        // Only squares with a teammate on them are targetable
        foreach (Space s in GetSpacesInRange(player.currSpace, range, includeDiagonals))
        {
            PlayerManager p = FindCharacterOn(s, player);
            if (player.IsAllyOf(p)) spaces.Add(s);
        }
        if (canTargetSelf) spaces.Add(player.currSpace);

        if (spaces.Count == 0)
        {
            Debug.Log($"No allies within {range} squares to surge.");
            return;
        }

        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager caster)
    {
        PlayerManager target = space == caster.currSpace ? caster : FindCharacterOn(space, caster);
        if (target == null) { Debug.Log("Nobody there to surge."); return; }      // free
        if (!caster.IsAllyOf(target)) { Debug.Log("Surge only works on allies."); return; }
        if (!TrySpend(caster)) return;

        StatModifierEffect.Apply(target, "Surge", durationTurns, damage: damageBonus);
        Debug.Log($"{caster.name} surges {target.name}: +{damageBonus} damage.");
    }
}
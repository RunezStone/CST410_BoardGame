using UnityEngine;

// Haste (0 pts): one ally (or yourself) within range 5 gets +2 movement and 1 extra action
// per turn for 3 of its turns (ticks at the end of the ally's turn). Refreshes, doesn't stack.
// Cast on yourself, the current turn counts as turn 1 and you get the extra action right away.
public class HasteCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 5).")]
    [SerializeField] private int range = 5;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private bool canTargetSelf = true;
    [SerializeField] private int movementBonus = 2;
    [SerializeField] private int extraActions = 1;
    [Tooltip("Counted on the TARGET's turns (Cards.xlsx: 3).")]
    [SerializeField] private int durationTurns = 3;

    void Reset() { points = 0; }

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetAllySpacesInRange(player, range, includeDiagonals, canTargetSelf);
        if (spaces.Count == 0) { Debug.Log($"No allies within {range} squares to haste."); return; }
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager caster)
    {
        PlayerManager target = CharacterOnOrSelf(space, caster);
        if (target == null || !caster.IsAllyOf(target)) { Debug.Log("Haste only works on allies."); return; }   // free
        if (target.playerData.IsDead) { Debug.Log($"{target.name} is downed."); return; }
        if (!TrySpend(caster)) return;

        StatModifierEffect.Apply(target, "Haste", durationTurns, movement: movementBonus, actions: extraActions);
        Debug.Log($"{caster.name} hastes {target.name}: +{movementBonus} movement, +{extraActions} action per turn.");
    }
}

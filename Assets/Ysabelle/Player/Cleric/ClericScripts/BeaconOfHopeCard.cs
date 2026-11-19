using UnityEngine;

// Beacon of Hope (1 pt): one hero (or yourself) within range 5 gets +2 damage on attacks
// and spells for 4 of its turns (ticks at the end of the ally's turn). Refreshes, doesn't stack.
public class BeaconOfHopeCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 5).")]
    [SerializeField] private int range = 5;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private bool canTargetSelf = true;
    [SerializeField] private int damageBonus = 2;
    [Tooltip("Counted on the TARGET's turns (Cards.xlsx: 4).")]
    [SerializeField] private int durationTurns = 4;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetAllySpacesInRange(player, range, includeDiagonals, canTargetSelf);
        if (spaces.Count == 0) { Debug.Log($"No allies within {range} squares."); return; }
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager caster)
    {
        PlayerManager target = CharacterOnOrSelf(space, caster);
        if (target == null || !caster.IsAllyOf(target)) { Debug.Log("Beacon of Hope only works on allies."); return; }   // free
        if (target.playerData.IsDead) { Debug.Log($"{target.name} is downed."); return; }
        if (!TrySpend(caster)) return;

        StatModifierEffect.Apply(target, "Beacon of Hope", durationTurns, damage: damageBonus);
        Debug.Log($"{caster.name} lights a Beacon of Hope over {target.name}: +{damageBonus} damage.");
    }
}

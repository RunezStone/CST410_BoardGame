using UnityEngine;

// Heal (1 pt): one ally (or yourself) within range 10 heals 2d6 HP.
// Can't heal a downed hero, and can't be played on someone at full HP.
public class HealCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 10).")]
    [SerializeField] private int range = 10;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private bool canTargetSelf = true;
    [SerializeField] private int flatHeal = 0;
    [SerializeField] private int healDieSides = 6;
    [SerializeField] private int healDiceCount = 2;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetAllySpacesInRange(player, range, includeDiagonals, canTargetSelf);
        if (spaces.Count == 0) { Debug.Log($"No allies within {range} squares to heal."); return; }
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager caster)
    {
        PlayerManager target = CharacterOnOrSelf(space, caster);
        if (target == null || !caster.IsAllyOf(target)) { Debug.Log("Heal only works on allies."); return; }   // free
        if (target.playerData.IsDead) { Debug.Log($"{target.name} is downed and can't be healed."); return; }
        if (target.playerData.hitPoints >= target.playerData.maxHitPoints) { Debug.Log($"{target.name} is already at full health."); return; }
        if (!TrySpend(caster)) return;

        int flat = flatHeal;
        RollDice(healDiceCount, healDieSides, roll =>
        {
            int healed = target.playerData.Heal(roll + flat);
            Debug.Log($"{caster.name} heals {target.name}: +{healed} HP ({target.playerData.hitPoints} HP).");
        });
    }
}

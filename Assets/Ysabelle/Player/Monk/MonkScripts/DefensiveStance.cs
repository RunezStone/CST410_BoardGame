using UnityEngine;

// Defensive Stance (1 focus point): +3 defense for 3 rounds of combat.
// Counts down at the START of the monk's turns, so the bonus covers 3 full rounds of
// everyone else attacking. Playing it again while active refreshes it (no stacking).
public class DefensiveStanceCard : ActionCard
{
    [SerializeField] private int defenseBonus = 3;
    [SerializeField] private int durationRounds = 3;

    void Reset() { points = 1; }   // default cost when the script is first added

    public override void InitiateAction(PlayerManager player)
    {
        if (!TrySpend(player)) return;

        StatModifierEffect.Apply(player, "Defensive Stance", durationRounds,
                                 defense: defenseBonus, countsDownAtTurnStart: true);
        Debug.Log($"{player.name} takes a defensive stance (Defense {player.playerData.defense}).");
    }
}
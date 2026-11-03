using UnityEngine;

// Meditation (1 focus point): heal yourself for 2d6.
// Heal = flatHeal + (healDiceCount d healDieSides). Set dice count to 0 for a flat heal.
public class MeditationCard : ActionCard
{
    [SerializeField] private int flatHeal = 0;
    [SerializeField] private int healDieSides = 6;
    [SerializeField] private int healDiceCount = 2;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        if (player.playerData.hitPoints >= player.playerData.maxHitPoints)
        {
            Debug.Log($"{player.name} is already at full health.");   // don't waste the action
            return;
        }
        if (!TrySpend(player)) return;

        int flat = flatHeal;
        int count = healDieSides > 0 ? healDiceCount : 0;   // RollDice(0, ...) reports 0 instantly

        RollDice(count, healDieSides, roll =>
        {
            int healed = player.playerData.Heal(roll + flat);
            Debug.Log($"{player.name} meditates: +{healed} HP ({player.playerData.hitPoints} HP).");
        });
    }
}
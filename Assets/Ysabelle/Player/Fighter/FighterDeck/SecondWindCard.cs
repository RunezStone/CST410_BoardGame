using UnityEngine;

// Second Wind (1 AP): heal yourself 1d12 (GDD v4.0 Appendix A).
public class SecondWindCard : ActionCard
{
    [SerializeField] private int flatHeal = 0;
    [SerializeField] private int healDieSides = 12;
    [SerializeField] private int healDiceCount = 1;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        if (player.playerData.hitPoints >= player.playerData.maxHitPoints)
        {
            Debug.Log($"{player.name} is already at full health.");   // not a legal target: don't waste the action
            return;
        }
        if (!TrySpend(player)) return;

        int flat = flatHeal;
        RollDice(healDiceCount, healDieSides, roll =>
        {
            int healed = player.playerData.Heal(roll + flat);
            Debug.Log($"{player.name} used Second Wind: +{healed} HP ({player.playerData.hitPoints} HP).");
        });
    }
}

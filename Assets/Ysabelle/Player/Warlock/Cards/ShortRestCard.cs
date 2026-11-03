using UnityEngine;

// Short Rest (0 spell points): regain 2 spell points, up to the Warlock's max.
// Uses the turn's action, so resting means not attacking this turn.
public class ShortRestCard : ActionCard
{
    [SerializeField] private int pointsRestored = 2;

    public override void InitiateAction(PlayerManager player)
    {
        PlayerData data = player.playerData;
        if (data.actionPoints >= data.maxActionPoints)
        {
            Debug.Log($"{player.name} is already at full spell points.");  
            return;
        }
        if (!TrySpend(player)) return;

        int gained = data.RestorePoints(pointsRestored);
        Debug.Log($"{player.name} takes a short rest: +{gained} spell points ({data.actionPoints}/{data.maxActionPoints}).");
    }
}
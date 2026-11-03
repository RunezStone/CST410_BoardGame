using UnityEngine;

// Rage (1 AP): +5 damage and +1 movement for 3 turns.
public class RageCard : ActionCard
{
    [SerializeField] private int damageBonus = 5;
    [SerializeField] private int movementBonus = 1;
    [SerializeField] private int durationTurns = 3;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        if (player.GetComponent<RageEffect>() != null)
        {
            Debug.Log($"{player.name} is already raging.");
            return;
        }
        if (!TrySpend(player)) return;

        player.gameObject.AddComponent<RageEffect>().Begin(player, damageBonus, movementBonus, durationTurns);
    }
}
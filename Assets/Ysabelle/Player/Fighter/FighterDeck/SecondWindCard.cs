using UnityEngine;

public class SecondWindCard : ActionCard
{
    [SerializeField] private int healAmount = 10;

    public override void InitiateAction(PlayerManager playerManager)
    {
        // Instant action - no targeting needed
        playerManager.playerData.hitPoints += healAmount;
        Debug.Log($"{playerManager.gameObject.name} healed for {healAmount} HP!");

        
        playerManager.playerData.actionPoints -= 1;

       
    }
}
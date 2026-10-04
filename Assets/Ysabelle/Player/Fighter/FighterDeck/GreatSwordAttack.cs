using UnityEngine;

public class GreatSwordAttack : ActionCard
{
    [SerializeField] private int damage = 5;
    
    public override void InitiateAction(PlayerManager playerManager)
    {
       
        playerManager.EnterTargetingMode(GetAdjacentSpaces(playerManager.currSpace), ExecuteAttack, playerManager);
    }

    private void ExecuteAttack(Space targetSpace, PlayerManager playerManager)
    {
        // Check if enemy is on the space
        Collider[] colliders = Physics.OverlapSphere(targetSpace.transform.position, 0.5f);
        
        foreach (Collider col in colliders)
        {
            PlayerManager enemy = col.GetComponent<PlayerManager>();
            if (enemy != null && enemy != playerManager)
            {
                enemy.playerData.hitPoints -= damage;
                Debug.Log($"{playerManager.gameObject.name} attacked {enemy.gameObject.name} for {damage} damage!");
            }
        }
    }

    private System.Collections.Generic.List<Space> GetAdjacentSpaces(Space center)
    {
        // Use your TileManager to get adjacent squares
        return TileManager.instance.GetValidMovementSpaces(center, 1);
    }
}
using UnityEngine;

public class BowShotCard : ActionCard
{
    [SerializeField] private int damage = 4;
    [SerializeField] private int range = 5;

    public override void InitiateAction(PlayerManager playerManager)
    {
        // Show all spaces within 5 squares
        var validSpaces = TileManager.instance.GetValidMovementSpaces(playerManager.currSpace, range);
        playerManager.EnterTargetingMode(validSpaces, ExecuteShot, playerManager);
    }

    private void ExecuteShot(Space targetSpace, PlayerManager playerManager)
    {
        // Check for enemies at target
        Collider[] colliders = Physics.OverlapSphere(targetSpace.transform.position, 0.5f);

        foreach (Collider col in colliders)
        {
            PlayerManager enemy = col.GetComponent<PlayerManager>();
            if (enemy != null && enemy != playerManager)
            {
                enemy.playerData.hitPoints -= damage;
                Debug.Log($"{playerManager.gameObject.name} shot {enemy.gameObject.name} for {damage} damage!");
            }
        }
    }
}
using UnityEngine;

public class TacticalShiftCard : ActionCard
{
    [SerializeField] private int teleportRange = 5;

    public override void InitiateAction(PlayerManager playerManager)
    {
        var validSpaces = TileManager.instance.GetValidMovementSpaces(playerManager.currSpace, teleportRange);
        playerManager.EnterTargetingMode(validSpaces, ExecuteTeleport, playerManager);
    }

    private void ExecuteTeleport(Space targetSpace, PlayerManager playerManager)
    {
        playerManager.PlaceCharacter(targetSpace);
        Debug.Log($"{playerManager.gameObject.name} teleported!");


        playerManager.playerData.actionPoints -= 1;
    }
}
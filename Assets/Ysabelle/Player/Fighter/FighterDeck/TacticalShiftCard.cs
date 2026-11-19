using UnityEngine;

// Tactical Shift (1 pt): teleport to an empty square within range 5. You keep your normal move.
public class TacticalShiftCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 5).")]
    [SerializeField] private int teleportRange = 5;
    [SerializeField] private bool includeDiagonals = false;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        if (player.IsFrozen) { Debug.Log($"{player.name} is frozen and can't teleport."); return; }
        var spaces = GetSpacesInRange(player.currSpace, teleportRange, includeDiagonals, emptyOnly: true);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager player)
    {
        if (player.IsFrozen) return;
        if (!TrySpend(player)) return;
        player.Teleport(space);   // Teleport, not PlaceCharacter: the normal move is kept
        Debug.Log($"{player.name} shifts to [{space.id[0]}, {space.id[1]}].");
    }
}

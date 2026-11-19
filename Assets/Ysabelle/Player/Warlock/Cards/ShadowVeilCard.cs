using UnityEngine;

// Shadow Veil (1 pt): teleport to an empty square within range 7. You keep your normal move.
public class ShadowVeilCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 7).")]
    [SerializeField] private int range = 7;
    [SerializeField] private bool includeDiagonals = false;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        if (player.IsFrozen) { Debug.Log($"{player.name} is frozen and can't teleport."); return; }
        var spaces = GetSpacesInRange(player.currSpace, range, includeDiagonals, emptyOnly: true);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager player)
    {
        if (player.IsFrozen) return;
        if (!TrySpend(player)) return;
        player.Teleport(space);
        Debug.Log($"{player.name} slips through the shadows to [{space.id[0]}, {space.id[1]}].");
    }
}

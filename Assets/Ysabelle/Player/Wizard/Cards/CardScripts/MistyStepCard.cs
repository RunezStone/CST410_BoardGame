using UnityEngine;

public class MistyStepCard : ActionCard
{
    [SerializeField] private int range = 8;
    [SerializeField] private bool includeDiagonals = false;

    void Reset() { points = 1; }   // default cost when the script is first added

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInRange(player.currSpace, range, includeDiagonals, emptyOnly: true);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager player)
    {
        if (!TrySpend(player)) return;
        player.Teleport(space);
        Debug.Log($"{player.name} misty-steps to [{space.id[0]}, {space.id[1]}].");
    }
}
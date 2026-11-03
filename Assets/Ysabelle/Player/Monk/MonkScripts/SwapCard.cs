using UnityEngine;
using System.Collections.Generic;

// Swap (1 focus point): trade squares with one teammate.
// Only allies light up. Neither character uses up their normal move.
public class SwapCard : ActionCard
{
    [Tooltip("How far away the teammate can be. 0 = anywhere on the board (GDD doesn't give a range).")]
    [SerializeField] private int range = 0;
    [SerializeField] private bool includeDiagonals = false;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        List<Space> spaces = new List<Space>();

        foreach (PlayerManager p in Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
        {
            if (p == player || !player.IsAllyOf(p) || p.currSpace == null || p.playerData.IsDead) continue;
            if (range > 0 && !InRange(player.currSpace, p.currSpace)) continue;
            spaces.Add(p.currSpace);
        }

        if (spaces.Count == 0) { Debug.Log("No teammate to swap with."); return; }
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    bool InRange(Space a, Space b)
    {
        int dx = Mathf.Abs(a.id[0] - b.id[0]), dy = Mathf.Abs(a.id[1] - b.id[1]);
        int dist = includeDiagonals ? Mathf.Max(dx, dy) : dx + dy;
        return dist <= range;
    }

    private void OnTargetChosen(Space space, PlayerManager player)
    {
        PlayerManager ally = FindCharacterOn(space, player);
        if (!player.IsAllyOf(ally)) { Debug.Log("Swap only works with teammates."); return; }
        if (!TrySpend(player)) return;

        player.SwapPlaces(ally);
        Debug.Log($"{player.name} swaps places with {ally.name}.");
    }
}
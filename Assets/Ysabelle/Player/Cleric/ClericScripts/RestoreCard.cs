using UnityEngine;

// Restore (0 pts): one OTHER hero (Fighter, Monk or Wizard) within range 5 regains 1 action point.
// Can't be played on someone already at max action points.
public class RestoreCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 5).")]
    [SerializeField] private int range = 5;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int pointsRestored = 1;

    void Reset() { points = 0; }

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetAllySpacesInRange(player, range, includeDiagonals, includeSelf: false);
        if (spaces.Count == 0) { Debug.Log($"No other heroes within {range} squares."); return; }
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager caster)
    {
        PlayerManager target = FindCharacterOn(space, caster);
        if (target == null || !caster.IsAllyOf(target)) { Debug.Log("Restore only works on other heroes."); return; }   // free
        PlayerData d = target.playerData;
        if (d.actionPoints >= d.maxActionPoints) { Debug.Log($"{target.name} is already at full action points."); return; }
        if (!TrySpend(caster)) return;

        int gained = d.RestorePoints(pointsRestored);
        Debug.Log($"{caster.name} restores {target.name}: +{gained} action point(s) ({d.actionPoints}/{d.maxActionPoints}).");
    }
}

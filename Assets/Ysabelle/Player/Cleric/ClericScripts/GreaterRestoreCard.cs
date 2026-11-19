using UnityEngine;

// Greater Restore (2 pts): roll 1d4; one OTHER hero (Fighter, Monk or Wizard) within range 4
// regains that many action points, up to their max. Can't be played on someone already full.
public class GreaterRestoreCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 4).")]
    [SerializeField] private int range = 4;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int restoreDieSides = 4;
    [SerializeField] private int restoreDiceCount = 1;

    void Reset() { points = 2; }

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetAllySpacesInRange(player, range, includeDiagonals, includeSelf: false);
        if (spaces.Count == 0) { Debug.Log($"No other heroes within {range} squares."); return; }
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager caster)
    {
        PlayerManager target = FindCharacterOn(space, caster);
        if (target == null || !caster.IsAllyOf(target)) { Debug.Log("Greater Restore only works on other heroes."); return; }   // free
        PlayerData d = target.playerData;
        if (d.actionPoints >= d.maxActionPoints) { Debug.Log($"{target.name} is already at full action points."); return; }
        if (!TrySpend(caster)) return;

        RollDice(restoreDiceCount, restoreDieSides, roll =>
        {
            int gained = d.RestorePoints(roll);
            Debug.Log($"{caster.name} greater-restores {target.name}: rolled {roll}, +{gained} action point(s) ({d.actionPoints}/{d.maxActionPoints}).");
        });
    }
}

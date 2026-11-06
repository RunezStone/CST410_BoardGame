using UnityEngine;

// Bow Shot (0 AP): ranged attack up to 5 squares away for 1d4 (GDD v4.0 Appendix A).
// Fires up, down, left and right only (a + from the fighter).
// The first character in a line can be shot, but blocks everything behind them.
public class BowShotCard : ActionCard
{
    [SerializeField] private int range = 5;
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 4;

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInLines(player.currSpace, range, StraightDirections);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to shoot."); return; }
        if (!TrySpend(attacker)) return;

        ResolveAttack(attacker, target, flatDamage, damageDieSides);
    }
}
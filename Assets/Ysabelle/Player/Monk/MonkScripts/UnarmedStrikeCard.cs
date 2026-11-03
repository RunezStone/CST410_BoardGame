using UnityEngine;

// Unarmed Strike (0 focus points): punch a character on an adjacent square.
// 2d4 damage (GDD).
public class UnarmedStrikeCard : ActionCard
{
    [Header("Attack")]
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 4;
    [SerializeField] private int damageDiceCount = 2;
    [SerializeField] private bool includeDiagonals = true;

    public override void InitiateAction(PlayerManager player)
    {
        player.EnterTargetingMode(GetSpacesInRange(player.currSpace, 1, includeDiagonals), OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to hit."); return; }   // free: no cost spent
        if (!TrySpend(attacker)) return;

        ResolveAttack(attacker, target, flatDamage, damageDieSides, damageDiceCount);
    }
}
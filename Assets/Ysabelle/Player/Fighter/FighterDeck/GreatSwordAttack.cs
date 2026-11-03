using UnityEngine;

// Great Sword Attack (0 AP): melee attack on an adjacent square.
public class GreatSwordAttack : ActionCard
{
    [Header("Attack")]
    [Tooltip("GDD says flat 5. Set Flat 5 / Die 0 for that, or Flat 0 / Die 4 for 1d4.")]
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 4;
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

        ResolveAttack(attacker, target, flatDamage, damageDieSides);
    }
}
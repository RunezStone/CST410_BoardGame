using UnityEngine;

// Great Sword Attack (0 AP): melee attack on an adjacent square for 2d4.
// 2d4 is the dice lab's Block B decision (one-shots a 6 HP minion on 6 of 16, 37.5%).
public class GreatSwordAttack : ActionCard
{
    [Header("Attack (Dice Lab Block B: 2d4)")]
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

using UnityEngine;

// Staff Strike (0 pts): attack one adjacent character for 1d4 damage.
public class StaffStrikeCard : ActionCard
{
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 4;
    [SerializeField] private int damageDiceCount = 1;
    [Tooltip("true = all 8 surrounding squares (Cards.xlsx: Adjacent, 8 sq)")]
    [SerializeField] private bool includeDiagonals = true;

    void Reset() { points = 0; }

    public override void InitiateAction(PlayerManager player)
    {
        player.EnterTargetingMode(GetSpacesInRange(player.currSpace, 1, includeDiagonals), OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to hit."); return; }   // free
        if (!TrySpend(attacker)) return;

        ResolveAttack(attacker, target, flatDamage, damageDieSides, damageDiceCount);
    }
}

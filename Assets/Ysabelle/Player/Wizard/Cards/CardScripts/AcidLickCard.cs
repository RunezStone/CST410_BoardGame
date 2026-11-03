using UnityEngine;

// Acid Lick (0 spell points): lick a character within 2 squares for 1d6.
// Shorter range than Spitfire but a bigger die.
public class AcidLickCard : ActionCard
{
    [SerializeField] private int range = 2;
    [Tooltip("false = count squares like movement (up/down/left/right). true = a diagonal step counts as 1.")]
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 6;
    [SerializeField] private int damageDiceCount = 1;

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInRange(player.currSpace, range, includeDiagonals);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to lick."); return; }
        if (!TrySpend(attacker)) return;

        ResolveAttack(attacker, target, flatDamage, damageDieSides, damageDiceCount);
    }
}
using UnityEngine;

// Spitfire Cantrip (0 spell points): a small ball of fire at a character within 6 squares for 1d4.
public class SpitfireCantripCard : ActionCard
{
    [SerializeField] private int range = 6;
    [Tooltip("false = count squares like movement (up/down/left/right). true = a diagonal step counts as 1.")]
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 4;
    [SerializeField] private int damageDiceCount = 1;

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInRange(player.currSpace, range, includeDiagonals);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to burn."); return; }   // free: no cost spent
        if (!TrySpend(attacker)) return;

        ResolveAttack(attacker, target, flatDamage, damageDieSides, damageDiceCount);
    }
}
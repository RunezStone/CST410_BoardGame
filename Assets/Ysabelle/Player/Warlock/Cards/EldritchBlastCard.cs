using UnityEngine;

// Eldritch Blast (0 spell points): hit a character within 7 squares for 1d12.
// No line of sight needed: a blast of raw magic, so walls and other characters don't block it.
public class EldritchBlastCard : ActionCard
{
    [SerializeField] private int range = 7;
    [Tooltip("false = count squares like movement (up/down/left/right). true = a diagonal step counts as 1.")]
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 12;
    [SerializeField] private int damageDiceCount = 1;

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInRange(player.currSpace, range, includeDiagonals);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to blast."); return; }   // free: no cost spent
        if (!TrySpend(attacker)) return;

        ResolveAttack(attacker, target, flatDamage, damageDieSides, damageDiceCount);
    }
}
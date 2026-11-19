using UnityEngine;

// Bite (0 pts): the minion's only action. Attack roll (+2 from the minion's PlayerData)
// against one adjacent character for 2d4 damage. Friendly fire applies like every attack.
public class BiteCard : ActionCard
{
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 4;
    [SerializeField] private int damageDiceCount = 2;
    [Tooltip("true = all 8 surrounding squares (Cards.xlsx: Bite is adjacent)")]
    [SerializeField] private bool includeDiagonals = true;

    void Reset() { points = 0; cardName = "Bite"; }

    public override void InitiateAction(PlayerManager player)
    {
        player.EnterTargetingMode(GetSpacesInRange(player.currSpace, 1, includeDiagonals), OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager minion)
    {
        PlayerManager target = FindCharacterOn(space, minion);
        if (target == null) { Debug.Log("Nobody there to bite."); return; }   // free
        if (!TrySpend(minion)) return;

        ResolveAttack(minion, target, flatDamage, damageDieSides, damageDiceCount);
    }
}

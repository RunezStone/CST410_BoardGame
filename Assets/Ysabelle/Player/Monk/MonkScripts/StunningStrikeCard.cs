using UnityEngine;

// Stunning Strike (1 focus point): pick a character on an adjacent square.
// They make a constitution check: d20 + their constitution vs the DC.
// Fail = stunned until the monk's next turn, which means they lose their next turn.
// No attack roll and no damage (the GDD only describes the stun).
public class StunningStrikeCard : ActionCard
{
    [Tooltip("Number the target must reach on d20 + constitution to resist. GDD doesn't give one; 12 is a placeholder.")]
    [SerializeField] private int checkDC = 12;
    [SerializeField] private bool includeDiagonals = true;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        player.EnterTargetingMode(GetSpacesInRange(player.currSpace, 1, includeDiagonals), OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to stun."); return; }   // free
        if (target.GetComponent<StunEffect>() != null) { Debug.Log($"{target.name} is already stunned."); return; }
        if (!TrySpend(attacker)) return;

        int dc = checkDC;
        RollDie(20, d20 =>
        {
            int con = target.playerData.constitution;
            int total = d20 + con;
            bool resisted = total >= dc;

            Debug.Log($"{target.name} constitution check: {d20} + {con} = {total} vs DC {dc} -> {(resisted ? "RESISTED" : "STUNNED")}");
            if (!resisted && target.GetComponent<StunEffect>() == null)
                target.gameObject.AddComponent<StunEffect>();
        });
    }
}

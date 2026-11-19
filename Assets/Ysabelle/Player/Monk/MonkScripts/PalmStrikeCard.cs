using UnityEngine;

// Palm Strike (1 pt): attack one adjacent character for 2d6. On a FULL hit, the target makes an
// Athletics check (d20 + athletics); on 12 or lower it is pushed 6 squares directly away from the Monk.
public class PalmStrikeCard : ActionCard
{
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 6;
    [SerializeField] private int damageDiceCount = 2;
    [Tooltip("true = all 8 surrounding squares (Cards.xlsx: Adjacent, 8 sq)")]
    [SerializeField] private bool includeDiagonals = true;

    [Header("Push")]
    [Tooltip("Pushed if d20 + Athletics is at or below this (Cards.xlsx: Athletics <= 12).")]
    [SerializeField] private int pushIfAtOrBelow = 12;
    [SerializeField] private int pushSquares = 6;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        player.EnterTargetingMode(GetSpacesInRange(player.currSpace, 1, includeDiagonals), OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager monk)
    {
        PlayerManager target = FindCharacterOn(space, monk);
        if (target == null) { Debug.Log("Nobody there to strike."); return; }   // free
        if (!TrySpend(monk)) return;

        // Copy Inspector values into locals for the dice callbacks
        Vector2Int pushDir = DirectionBetween(monk.currSpace, target.currSpace);
        int dc = pushIfAtOrBelow + 1;   // RollSave fails on "total < dc", so <= 12 means dc 13
        int squares = pushSquares;

        ResolveAttack(monk, target, flatDamage, damageDieSides, damageDiceCount,
            onHit: () => RollSave(target, SaveStat.Athletics, dc,
                                  onFail: () => PushCharacter(target, pushDir, squares)));
    }
}

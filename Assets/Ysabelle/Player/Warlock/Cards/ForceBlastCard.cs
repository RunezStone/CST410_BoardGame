using UnityEngine;

public class ForceBlastCard : ActionCard
{

    [SerializeField] private int range = 3;
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 4;
    [SerializeField] private int damageDiceCount = 4;
    [SerializeField] private int pushSquares = 2;

    void Reset() { points = 1; }   // default cost when the script is first added

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInLines(player.currSpace, range, StraightDirections);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to blast."); return; }
        if (!TrySpend(attacker)) return;


        Vector2Int pushDir = DirectionBetween(attacker.currSpace, target.currSpace);
        int squares = pushSquares;

        ResolveAttack(attacker, target, flatDamage, damageDieSides, damageDiceCount,
                      onHit: () => PushCharacter(target, pushDir, squares));
    }
}
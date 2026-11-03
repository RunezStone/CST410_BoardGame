using UnityEngine;
using System.Collections.Generic;

public class BubbleBeamCard : ActionCard
{
    [SerializeField] private int range = 5;
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 8;
    [SerializeField] private int damageDiceCount = 1;
    [SerializeField] private int pushSquares = 1;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInLines(player.currSpace, range, StraightDirections, stopAtCharacters: false);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        Vector2Int dir = DirectionBetween(attacker.currSpace, space);


        List<Space> beam = GetSpacesInLines(attacker.currSpace, range, new[] { dir }, stopAtCharacters: false);
        List<PlayerManager> targets = new List<PlayerManager>();
        for (int i = beam.Count - 1; i >= 0; i--)
        {
            PlayerManager p = FindCharacterOn(beam[i], attacker);
            if (p != null) targets.Add(p);
        }

        if (targets.Count == 0) { Debug.Log("Nobody in the beam's path."); return; }   // free
        if (!TrySpend(attacker)) return;

        int squares = pushSquares;
        ResolveAreaAttack(attacker, targets, flatDamage, damageDieSides, damageDiceCount,
                          onHit: (target, damage) => PushCharacter(target, dir, squares));
    }
}
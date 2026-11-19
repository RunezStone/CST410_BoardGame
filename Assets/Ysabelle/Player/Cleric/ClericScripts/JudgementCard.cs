using UnityEngine;
using System.Collections.Generic;

// Judgement (1 pt): one enemy within range 6 makes a Constitution check (d20 + Con).
// Below 13, it's Frozen for 2 of its turns: no normal move, no card teleports, no Swap.
// No attack roll and no damage.
public class JudgementCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 6).")]
    [SerializeField] private int range = 6;
    [SerializeField] private bool includeDiagonals = false;
    [Tooltip("Frozen if d20 + Constitution is below this (Cards.xlsx: Con < 13).")]
    [SerializeField] private int checkDC = 13;
    [Tooltip("Counted on the TARGET's turns (Cards.xlsx: 2).")]
    [SerializeField] private int durationTurns = 2;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        List<Space> spaces = new List<Space>();
        foreach (Space s in GetSpacesInRange(player.currSpace, range, includeDiagonals))
        {
            PlayerManager p = FindCharacterOn(s, player);
            if (p != null && !player.IsAllyOf(p) && !p.playerData.IsDead) spaces.Add(s);   // enemies only
        }

        if (spaces.Count == 0) { Debug.Log($"No enemies within {range} squares to judge."); return; }
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager caster)
    {
        PlayerManager target = FindCharacterOn(space, caster);
        if (target == null || caster.IsAllyOf(target)) { Debug.Log("Judgement only works on enemies."); return; }   // free
        if (!TrySpend(caster)) return;

        int turns = durationTurns;
        RollSave(target, SaveStat.Constitution, checkDC, onFail: () => FrozenEffect.Apply(target, turns));
    }
}

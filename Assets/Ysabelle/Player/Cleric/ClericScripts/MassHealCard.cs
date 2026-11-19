using UnityEngine;
using System.Collections.Generic;

// Mass Heal (3 pts): roll 3d12 once; every living hero on the board (the Cleric's team,
// Cleric included) heals that many HP. No targeting: it fires as soon as you play it.
public class MassHealCard : ActionCard
{
    [SerializeField] private int flatHeal = 0;
    [SerializeField] private int healDieSides = 12;
    [SerializeField] private int healDiceCount = 3;

    void Reset() { points = 3; }

    public override void InitiateAction(PlayerManager player)
    {
        List<PlayerManager> targets = new List<PlayerManager>();
        bool anyoneHurt = false;

        foreach (PlayerManager p in Object.FindObjectsByType<PlayerManager>())
        {
            if (!player.IsAllyOf(p) || p.playerData.IsDead) continue;   // downed heroes aren't healed
            targets.Add(p);
            if (p.playerData.hitPoints < p.playerData.maxHitPoints) anyoneHurt = true;
        }

        if (!anyoneHurt) { Debug.Log("Every living hero is already at full health."); return; }   // free
        if (!TrySpend(player)) return;

        int flat = flatHeal;
        RollDice(healDiceCount, healDieSides, roll =>
        {
            int amount = roll + flat;
            Debug.Log($"{player.name} casts Mass Heal for {amount}!");
            foreach (PlayerManager t in targets)
            {
                int healed = t.playerData.Heal(amount);
                Debug.Log($"  {t.name}: +{healed} HP ({t.playerData.hitPoints} HP).");
            }
        });
    }
}

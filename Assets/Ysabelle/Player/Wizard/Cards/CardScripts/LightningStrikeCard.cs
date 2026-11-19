using UnityEngine;
using System.Collections.Generic;

// Lightning Strike (1 spell point): hit a character within 6 for 2d12. On a FULL hit, the lightning
// arcs to everyone adjacent to the target except the caster (friendly fire included).
// Arc targets take half the damage, rounded up, with no attack roll.
public class LightningStrikeCard : ActionCard
{
    [Tooltip("Orthogonal range (Cards.xlsx: 6).")]
    [SerializeField] private int range = 6;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int flatDamage = 0;
    [SerializeField] private int damageDieSides = 12;
    [SerializeField] private int damageDiceCount = 2;

    [Header("Chain")]
    [Tooltip("1 = only people next to the main target. 2+ = it keeps jumping to people next to THEM, and so on.")]
    [SerializeField] private int chainJumps = 1;
    [Tooltip("Arc damage as a % of the main hit, rounded UP. Cards.xlsx: 50.")]
    [Range(0, 100)][SerializeField] private int chainDamagePercent = 50;
    [Tooltip("Does 'adjacent' include diagonal squares?")]
    [SerializeField] private bool chainIncludesDiagonals = true;
    [Tooltip("Can the arc hit the wizard who cast it?")]
    [SerializeField] private bool chainCanHitCaster = false;

    void Reset() { points = 1; }

    public override void InitiateAction(PlayerManager player)
    {
        var spaces = GetSpacesInRange(player.currSpace, range, includeDiagonals);
        player.EnterTargetingMode(spaces, OnTargetChosen, player);
    }

    private void OnTargetChosen(Space space, PlayerManager attacker)
    {
        PlayerManager target = FindCharacterOn(space, attacker);
        if (target == null) { Debug.Log("Nobody there to strike."); return; }
        if (!TrySpend(attacker)) return;

        // Copy settings into locals so the dice callback doesn't depend on this card still existing
        int jumps = chainJumps, percent = chainDamagePercent;
        bool diag = chainIncludesDiagonals, hitCaster = chainCanHitCaster;

        ResolveAreaAttack(attacker, new List<PlayerManager> { target }, flatDamage, damageDieSides, damageDiceCount,
            onHit: (mainTarget, damage) =>
                Chain(attacker, mainTarget, Mathf.CeilToInt(damage * percent / 100f), jumps, diag, hitCaster));
    }

    // Spreads outward from the main target one "ring" of neighbours at a time.
    // Each character is only shocked once.
    static void Chain(PlayerManager caster, PlayerManager mainTarget, int damage, int jumps, bool diag, bool hitCaster)
    {
        if (damage <= 0 || jumps <= 0) return;

        HashSet<PlayerManager> shocked = new HashSet<PlayerManager> { mainTarget };
        if (!hitCaster) shocked.Add(caster);   // marked as "done" so the arc skips them

        List<PlayerManager> frontier = new List<PlayerManager> { mainTarget };

        for (int jump = 0; jump < jumps && frontier.Count > 0; jump++)
        {
            List<PlayerManager> next = new List<PlayerManager>();

            foreach (PlayerManager from in frontier)
            {
                foreach (Space s in GetSpacesInRange(from.currSpace, 1, diag))
                {
                    PlayerManager p = FindCharacterOn(s);
                    if (p == null || shocked.Contains(p)) continue;

                    shocked.Add(p);
                    next.Add(p);
                }
            }

            foreach (PlayerManager p in next)
            {
                Debug.Log($"Lightning arcs to {p.name}!");
                ApplyDamage(caster, p, damage);
            }
            frontier = next;
        }
    }
}
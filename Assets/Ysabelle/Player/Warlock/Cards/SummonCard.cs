using UnityEngine;

// Summon (Must Have) (4 pts): roll 1d4 and summon that many minions onto empty squares
// within range 4. Max minions on the board = number of heroes on the board (extra rolls are lost).
// Minions act right after the Villain's turn (see MinionManager).
//
// Flow: pick the first square (that's when the cost is paid and the d4 is rolled),
// then pick a square for each remaining minion. Escape on a later pick forfeits the rest.
// TODO (needs a hand system): Summon always starts in the Villain's opening hand.
public class SummonCard : ActionCard
{
    [Tooltip("Orthogonal range from the Villain (Cards.xlsx: 4).")]
    [SerializeField] private int range = 4;
    [SerializeField] private bool includeDiagonals = false;
    [SerializeField] private int countDieSides = 4;
    [SerializeField] private int countDiceCount = 1;

    void Reset() { points = 4; cardName = "Summon"; }

    public override void InitiateAction(PlayerManager player)
    {
        MinionManager mm = MinionManager.instance;
        if (mm == null) { Debug.LogError("No MinionManager in the scene."); return; }
        if (mm.SlotsLeft <= 0) { Debug.Log($"Already at the minion cap ({mm.Cap})."); return; }

        var spaces = GetSpacesInRange(player.currSpace, range, includeDiagonals, emptyOnly: true);
        if (spaces.Count == 0) { Debug.Log($"No empty squares within {range} to summon onto."); return; }

        Debug.Log("Summon: pick a square for the first minion.");
        player.EnterTargetingMode(spaces, OnFirstSquare, player);
    }

    private void OnFirstSquare(Space space, PlayerManager villain)
    {
        MinionManager mm = MinionManager.instance;
        if (mm == null || mm.SlotsLeft <= 0) return;   // free
        if (!TrySpend(villain)) return;

        int r = range;
        bool diag = includeDiagonals;

        RollDice(countDiceCount, countDieSides, roll =>
        {
            int count = Mathf.Min(roll, mm.SlotsLeft);
            Debug.Log($"{villain.name} summons: rolled {roll}, {count} minion(s) " +
                      $"({(count < roll ? $"capped at {mm.Cap}" : "under the cap")}).");

            mm.Spawn(space);
            PlaceRemaining(villain, count - 1, r, diag);
        });
    }

    static void PlaceRemaining(PlayerManager villain, int left, int range, bool diag)
    {
        if (left <= 0) return;

        var spaces = GetSpacesInRange(villain.currSpace, range, diag, emptyOnly: true);
        if (spaces.Count == 0) { Debug.Log($"No room for {left} more minion(s); they're lost."); return; }

        Debug.Log($"Summon: pick a square for {left} more minion(s). (Escape forfeits them.)");
        villain.EnterTargetingMode(spaces, (s, v) =>
        {
            if (MinionManager.instance != null) MinionManager.instance.Spawn(s);
            PlaceRemaining(v, left - 1, range, diag);
        }, villain);
    }
}

namespace Convergence.Rules
{
    // One entry per phase on the turn phase card (GDD v4.0, Section 4.4), in the printed order.
    // If this order and the card ever disagree, the card wins until the team changes it at the table.
    public enum TurnPhase
    {
        StartOfTurn = 1,  // Phase 1: automatic. Ready token, tick start-of-turn effects, Stun / Echo skip to Phase 5
        Choice      = 2,  // Phase 2: pick Move, one action, or End turn
        Resolution  = 3,  // Phase 3: move the figure / roll the dice
        Payment     = 4,  // Phase 4: pay points, spend the token, defeated characters leave the board
        EndOfTurn   = 5,  // Phase 5: tick end-of-turn effects, hand limit, round marker, PASS to next player
        Check       = 6   // Phase 6: game end / phase end tests, draft, next phase setup
    }
}

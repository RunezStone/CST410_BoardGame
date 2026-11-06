using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Convergence.Rules
{
    // "Give it the game state and a player and get back concrete actions with their parameters."
    // An illegal move is never in the list; nothing is added and rejected later.
    //
    // Phase card, Phase 2: an action is legal only if ALL FOUR are true:
    //   1. the card is in your hand
    //   2. your action token is ready
    //   3. your point tracker shows at least the card's cost
    //   4. there is a legal target in range
    // Plus: one Move per turn, in one piece; Judgement (Frozen) blocks Move and Reposition;
    // a Stunned or defeated character has no choices at all.
    public static class LegalMoves
    {
        public static List<GameAction> For(GameState s, Character actor, TurnPhase phase)
        {
            var list = new List<GameAction>();

            switch (phase)
            {
                case TurnPhase.Choice:
                    if (actor.IsDefeated || actor.stunned) return list;
                    AddMoves(s, actor, list);
                    if (actor.actionReady) AddActions(s, actor, list);
                    list.Add(GameAction.EndTurn(actor));   // "End turn, at any point"
                    return list;

                case TurnPhase.Check:
                    AddDraft(s, list);
                    return list;

                // Phases 1, 3, 4 and 5 are automatic on the phase card: the player makes no choice.
                default:
                    return list;
            }
        }

        // ---------------- Move ----------------

        static void AddMoves(GameState s, Character a, List<GameAction> list)
        {
            if (a.hasMoved || a.frozenThisTurn) return;
            foreach (var sq in s.Reachable(a.pos, a.Movement))
                list.Add(new GameAction { verb = PlayerAction.Move, actor = a, square = sq });
        }

        // ---------------- One action ----------------

        static void AddActions(GameState s, Character a, List<GameAction> list)
        {
            foreach (var card in a.hand)                       // (1) card is in hand
            {
                if (a.points < card.cost) continue;            // (3) enough points
                if (card.verb == PlayerAction.Reposition && a.frozenThisTurn) continue;
                AddCardTargets(s, a, card, list);              // (4) only targets that exist are added
            }

            // Rest: basic action for everyone with a point pool (+1 up to the maximum)
            if (a.maxPoints > 0 && a.points < a.maxPoints)
                list.Add(new GameAction { verb = PlayerAction.Rest, actor = a });

            if (a.role != Role.Villain) return;

            // Summon: 1 spell point, a minion in reserve, an empty square adjacent to the Villain
            if (a.points >= 1 && s.minionReserve > 0)
                foreach (var sq in s.AdjacentSquares(a.pos))
                    if (s.IsEmpty(sq))
                        list.Add(new GameAction { verb = PlayerAction.Summon, actor = a, square = sq });

            // Command: up to two minions on the board
            var minions = s.MinionsOnBoard;
            for (int i = 0; i < minions.Count; i++)
            {
                list.Add(new GameAction { verb = PlayerAction.Command, actor = a, minions = new List<Character> { minions[i] } });
                for (int j = i + 1; j < minions.Count; j++)
                    list.Add(new GameAction { verb = PlayerAction.Command, actor = a, minions = new List<Character> { minions[i], minions[j] } });
            }
        }

        static void AddCardTargets(GameState s, Character a, CardDef card, List<GameAction> list)
        {
            GameAction Make() => new GameAction { verb = card.verb, actor = a, card = card };

            switch (card.targeting)
            {
                case Targeting.Adjacent:
                    foreach (var c in s.characters)
                    {
                        if (c == a || !c.IsActive || !GameState.Adjacent(a.pos, c.pos)) continue;
                        if (card.stun && c.stunned) continue;              // already stunned
                        var act = Make(); act.target = c; list.Add(act);
                    }
                    break;

                case Targeting.AllAdjacent:
                    if (s.characters.Any(c => c != a && c.IsActive && GameState.Adjacent(a.pos, c.pos)))
                        list.Add(Make());
                    break;

                case Targeting.Range:
                    foreach (var c in s.characters)
                        if (c != a && c.IsActive && GameState.Orthogonal(a.pos, c.pos) <= card.range)
                        { var act = Make(); act.target = c; list.Add(act); }
                    break;

                case Targeting.Line:
                    foreach (var dir in GameState.Straight)
                        foreach (var sq in s.LineSquares(a.pos, dir, card.range))
                        {
                            var c = s.CharacterAt(sq);
                            if (c == null) continue;
                            var act = Make(); act.target = c; act.direction = dir; list.Add(act);
                            break;                                          // first character blocks the rest
                        }
                    break;

                case Targeting.LinePierce:
                    foreach (var dir in GameState.Straight)
                        if (s.LineSquares(a.pos, dir, card.range).Any(sq => s.CharacterAt(sq) != null))
                        { var act = Make(); act.direction = dir; list.Add(act); }
                    break;

                case Targeting.Self:
                    if (SelfCardHasEffect(a, card)) list.Add(Make());
                    break;

                case Targeting.Ally:
                    foreach (var c in s.characters)
                    {
                        if (!c.IsActive || !a.IsAllyOf(c) || c.IsMinion) continue;
                        if (c == a && !card.allyIncludesSelf) continue;
                        if (card.range > 0 && GameState.Orthogonal(a.pos, c.pos) > card.range) continue;
                        if (!card.heal.IsNone && c.hp >= c.maxHp) continue;    // nothing to heal
                        var act = Make(); act.target = c; list.Add(act);
                    }
                    break;

                case Targeting.Enemy:
                    foreach (var c in s.characters)
                    {
                        if (!c.IsActive || a.IsAllyOf(c)) continue;
                        if (card.range > 0 && GameState.Orthogonal(a.pos, c.pos) > card.range) continue;
                        if (card.buffName != null && c.HasEffect(card.buffName)) continue;   // already Frozen
                        var act = Make(); act.target = c; list.Add(act);
                    }
                    break;

                case Targeting.EmptySquare:
                    for (int x = 0; x < GameState.Columns; x++)
                        for (int y = 0; y < GameState.Rows; y++)
                        {
                            var sq = new Vector2Int(x, y);
                            int d = GameState.Orthogonal(a.pos, sq);
                            if (d >= 1 && d <= card.range && s.IsEmpty(sq))
                            { var act = Make(); act.square = sq; list.Add(act); }
                        }
                    break;

                case Targeting.AllLivingHeroes:
                    if (s.Heroes.Any(h => h.IsActive && h.hp < h.maxHp)) list.Add(Make());
                    break;
            }
        }

        // A self card is only offered if it would do something (don't waste the action).
        static bool SelfCardHasEffect(Character a, CardDef card)
        {
            if (card.restorePoints > 0) return a.points < a.maxPoints;                  // Short Rest at full
            if (!card.heal.IsNone) return a.hp < a.maxHp;                               // heal at full HP
            if (card.buffName != null && !card.buffStacksRefresh) return !a.HasEffect(card.buffName);  // Rage doesn't stack
            return true;
        }

        // ---------------- Draft (Check phase, only after a phase ends) ----------------

        static void AddDraft(GameState s, List<GameAction> list)
        {
            if (s.heroDraftPicksLeft > 0)
            {
                foreach (var hero in s.Heroes)
                {
                    s.heroPicksThisDraft.TryGetValue(hero, out int taken);
                    if (taken >= 2) continue;                       // no hero takes more than 2 of the 3 picks
                    foreach (var card in hero.deck)
                        list.Add(new GameAction { verb = PlayerAction.Draft, actor = hero, draftFor = hero, draftCard = card });
                }
            }
            if (s.villainDraftPicksLeft > 0)
            {
                foreach (var opt in CardCatalog.VillainDraftOptions)
                {
                    if (opt.StartsWith("+1 max") && s.Villain.maxPoints >= 5) continue;
                    list.Add(new GameAction { verb = PlayerAction.Draft, actor = s.Villain, villainDraftOption = opt });
                }
            }
        }

        // ---------------- Printing ----------------

        // Compact listing: the move squares are folded onto one line so the rest stays readable.
        public static string Format(List<GameAction> actions, bool full = true)
        {
            if (actions.Count == 0) return "    (none - this phase is automatic)";

            var sb = new StringBuilder();
            var moves = actions.Where(x => x.verb == PlayerAction.Move).ToList();
            var others = actions.Where(x => x.verb != PlayerAction.Move).ToList();

            if (!full)
            {
                var counts = actions.GroupBy(x => x.IsEndTurn ? "End turn" : x.verb.ToString())
                                    .Select(g => $"{g.Key} x{g.Count()}");
                return $"    {actions.Count} legal: " + string.Join(", ", counts);
            }

            if (moves.Count > 0)
                sb.AppendLine($"    Move to any of {moves.Count} squares: " +
                              string.Join(" ", moves.Select(m => GameAction.Sq(m.square.Value))));
            foreach (var a in others) sb.AppendLine("    " + a);
            return sb.ToString().TrimEnd();
        }
    }
}

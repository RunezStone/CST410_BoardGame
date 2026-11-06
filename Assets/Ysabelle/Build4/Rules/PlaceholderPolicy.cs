using System.Collections.Generic;
using System.Linq;

namespace Convergence.Rules
{
    // Placeholder decisions so the state machine can run unattended. NOT balanced, NOT smart:
    // it only ever picks from the legal list it is handed, and every tie is broken by the seeded
    // DiceService so the whole run repeats exactly for the same seed.
    //
    //   1. Take a useful action if there is one (hit an enemy, Command, Summon, heal/buff, Disable).
    //   2. Otherwise walk toward the nearest enemy.
    //   3. Otherwise Rest if low on points, else End turn.
    public class PlaceholderPolicy : IDecisionMaker
    {
        public GameAction Choose(GameState s, Character actor, TurnPhase phase, List<GameAction> legal)
        {
            if (legal.Count == 0) return null;
            if (phase == TurnPhase.Check) return s.dice.Pick(legal);   // Draft

            var scored = legal.Where(a => !a.IsEndTurn && a.verb != PlayerAction.Move)
                              .Select(a => (a, score: Score(s, a)))
                              .Where(x => x.score > 0)
                              .ToList();
            if (scored.Count > 0)
            {
                int best = scored.Max(x => x.score);
                var top = scored.Where(x => x.score == best).Select(x => x.a).ToList();
                if (best >= 2 || actor.hasMoved) return s.dice.Pick(top);
            }

            var moves = legal.Where(a => a.verb == PlayerAction.Move).ToList();
            var enemy = s.NearestEnemy(actor);
            if (moves.Count > 0 && enemy != null)
            {
                int now = GameState.Orthogonal(actor.pos, enemy.pos);
                int bestDist = moves.Min(m => GameState.Orthogonal(m.square.Value, enemy.pos));
                if (bestDist < now && now > 1)
                    return s.dice.Pick(moves.Where(m => GameState.Orthogonal(m.square.Value, enemy.pos) == bestDist).ToList());
            }

            if (scored.Count > 0) return s.dice.Pick(scored.Where(x => x.score == scored.Max(y => y.score)).Select(x => x.a).ToList());
            return legal.First(a => a.IsEndTurn);
        }

        static int Score(GameState s, GameAction a)
        {
            var me = a.actor;
            switch (a.verb)
            {
                case PlayerAction.Strike:
                    if (a.card.targeting == Targeting.AllAdjacent || a.card.targeting == Targeting.LinePierce)
                    {
                        var hit = a.card.targeting == Targeting.AllAdjacent
                            ? s.characters.Where(c => c != me && c.IsActive && GameState.Adjacent(me.pos, c.pos))
                            : s.LineSquares(me.pos, a.direction, a.card.range).Select(s.CharacterAt).Where(c => c != null);
                        int net = hit.Count(c => !c.IsAllyOf(me)) - hit.Count(c => c.IsAllyOf(me));
                        return net > 0 ? 3 : 0;
                    }
                    return a.target.IsAllyOf(me) ? 0 : 3;               // don't hit friends on purpose
                case PlayerAction.Command:  return a.minions.Count == 2 || s.MinionsOnBoard.Count == 1 ? 3 : 0;
                case PlayerAction.Summon:   return s.MinionsOnBoard.Count < 3 ? 2 : 0;
                case PlayerAction.Disable:  return a.target.IsAllyOf(me) ? 0 : 2;
                case PlayerAction.Support:
                    if (!a.card.heal.IsNone)
                    {
                        var t = a.target ?? me;
                        return t.hp <= t.maxHp / 2 ? 2 : 0;
                    }
                    return 1;
                case PlayerAction.Rest:     return me.points == 0 ? 1 : 0;
                default:                    return 0;
            }
        }
    }
}

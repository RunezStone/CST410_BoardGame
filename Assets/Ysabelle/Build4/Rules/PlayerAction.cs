using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Convergence.Rules
{
    // One entry per verb kept on the Week 5 verb wall (GDD v4.0, Section 4.3), named as the wall names it.
    // Nine verbs survived the cut. Anything not on the wall (Bite, Echo, Push...) is NOT a verb:
    // Bite happens inside Command, Echo is automatic upkeep in Phase 1, pushes are part of a Strike.
    public enum PlayerAction
    {
        Move,        // MOVE pile    - move up to your Movement, once per turn
        Strike,      // REMOVE pile  - attack a character with an attack card
        Reposition,  // MOVE pile    - Tactical Shift, Misty Step, Swap
        Support,     // CONVERT pile - heal or buff an ally (or yourself)
        Disable,     // REMOVE pile  - Stunning Strike, Judgement
        Rest,        // ACQUIRE pile - +1 point (Villain's Short Rest card: +2)
        Summon,      // ACQUIRE pile - Villain only: minion from reserve to a square next to the Villain
        Command,     // COMMIT pile  - Villain only: up to two minions move 4 and may Bite
        Draft        // ACQUIRE pile - end of phase: take a reward card
    }

    // A concrete, fully-parameterised move: the verb plus everything needed to resolve it
    // (which card, which square, which character, which direction, which minions...).
    // LegalMoves only ever returns actions that are legal right now.
    public class GameAction
    {
        // null = "End turn". The phase card lists End turn as a Phase 2 choice, but it is not a verb
        // on the wall, so it is not in the PlayerAction enum.
        public PlayerAction? verb;
        public Character actor;

        public CardDef card;                 // card being played (null for Move, basic Rest, Summon, Command)
        public Vector2Int? square;           // Move / teleport / Summon destination
        public Character target;             // single target (Strike, Support, Disable, Swap)
        public Vector2Int direction;         // line cards that pick a direction (Bubble Beam)
        public List<Character> minions;      // Command: the (up to two) minions chosen
        public Character draftFor;           // Draft: who receives the card
        public CardDef draftCard;            // Draft: hero card picked
        public string villainDraftOption;    // Draft: Villain option picked

        public bool IsEndTurn => verb == null;
        public int Cost => card != null ? card.cost : (verb == PlayerAction.Summon ? 1 : 0);

        public static GameAction EndTurn(Character actor) => new GameAction { verb = null, actor = actor };

        public override string ToString()
        {
            if (IsEndTurn) return "End turn";

            var sb = new StringBuilder(verb.ToString());
            if (card != null) sb.Append(" - ").Append(card.name);

            switch (verb)
            {
                case PlayerAction.Move:
                    sb.Append(" to ").Append(Sq(square.Value));
                    break;
                case PlayerAction.Summon:
                    sb.Append(" minion at ").Append(Sq(square.Value));
                    break;
                case PlayerAction.Command:
                    var names = new List<string>();
                    foreach (var m in minions) names.Add(m.name);
                    sb.Append(" ").Append(string.Join(" + ", names));
                    break;
                case PlayerAction.Draft:
                    if (draftCard != null) sb.Append(" ").Append(draftCard.name).Append(" for ").Append(draftFor.name);
                    else sb.Append(" (Villain) ").Append(villainDraftOption);
                    break;
                default:
                    if (target != null) sb.Append(" -> ").Append(target.name).Append(" ").Append(Sq(target.pos));
                    else if (square.HasValue) sb.Append(" -> ").Append(Sq(square.Value));
                    else if (direction != Vector2Int.zero) sb.Append(" toward ").Append(DirName(direction));
                    break;
            }

            if (Cost > 0) sb.Append(" (").Append(Cost).Append(" pt)");
            return sb.ToString();
        }

        public static string Sq(Vector2Int p) => $"[{p.x},{p.y}]";

        public static string DirName(Vector2Int d)
        {
            if (d == Vector2Int.up) return "up";
            if (d == Vector2Int.down) return "down";
            if (d == Vector2Int.left) return "left";
            if (d == Vector2Int.right) return "right";
            return d.ToString();
        }
    }
}

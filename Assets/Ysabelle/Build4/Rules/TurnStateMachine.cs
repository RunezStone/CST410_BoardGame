using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Convergence.Rules
{
    // Anything that picks from the legal list: the placeholder policy now, real UI/players later.
    public interface IDecisionMaker
    {
        GameAction Choose(GameState state, Character actor, TurnPhase phase, List<GameAction> legal);
    }

    // Walks the six phases of the turn phase card (GDD v4.0 Section 4.4) in printed order.
    // Each call to Advance() runs exactly one phase and makes exactly one explicit transition.
    public class TurnStateMachine
    {
        // Every transition the phase card allows, and nothing else.
        public static readonly Dictionary<TurnPhase, TurnPhase[]> Transitions = new Dictionary<TurnPhase, TurnPhase[]>
        {
            { TurnPhase.StartOfTurn, new[] { TurnPhase.Choice, TurnPhase.EndOfTurn } },   // Stun / Echo skip to Phase 5
            { TurnPhase.Choice,      new[] { TurnPhase.Resolution, TurnPhase.EndOfTurn } }, // End turn skips to Phase 5
            { TurnPhase.Resolution,  new[] { TurnPhase.Payment } },
            { TurnPhase.Payment,     new[] { TurnPhase.Choice, TurnPhase.EndOfTurn } },   // back to 2 if a move or action is left
            { TurnPhase.EndOfTurn,   new[] { TurnPhase.Check } },                         // passes play to the next player
            { TurnPhase.Check,       new[] { TurnPhase.StartOfTurn } },                   // next player's turn begins
        };

        // The turn order from the phase card: the Villain goes between every pair of heroes.
        public static readonly Role[] TurnOrder =
            { Role.Fighter, Role.Villain, Role.Wizard, Role.Villain, Role.Monk, Role.Villain, Role.Cleric, Role.Villain };

        public GameState State { get; }
        public TurnPhase Phase { get; private set; } = TurnPhase.StartOfTurn;
        public Character Active { get; private set; }
        public List<GameAction> LastLegal { get; private set; } = new List<GameAction>();
        public GameAction LastChosen { get; private set; }
        public bool GameOver => State.gameOver;

        readonly IDecisionMaker decider;
        readonly Action<string> log;
        GameAction pending;

        public TurnStateMachine(GameState state, IDecisionMaker decider, Action<string> log)
        {
            State = state;
            this.decider = decider;
            this.log = log ?? (_ => { });
        }

        // ================= Setup =================

        public static GameState NewGame(DiceLabConfig lab, DiceService dice, Action<string> log)
        {
            var s = new GameState(lab, dice);
            foreach (var role in new[] { Role.Fighter, Role.Wizard, Role.Monk, Role.Cleric, Role.Villain })
            {
                var c = CardCatalog.Create(role);
                c.deck = CardCatalog.DeckFor(role, lab);
                s.characters.Add(c);
            }
            foreach (var role in TurnOrder) s.turnOrder.Add(s.characters.First(c => c.role == role));
            SetUpPhase(s, 1, log ?? (_ => { }));
            return s;
        }

        // "Setup for each phase" (Section 4.1)
        public static void SetUpPhase(GameState s, int phase, Action<string> log)
        {
            s.phaseNumber = phase;
            s.round = 1;
            s.villainTurnsThisRound = 0;
            s.turnIndex = 0;                   // begin with the Fighter
            s.heroPicksThisDraft.Clear();

            // Clear last phase's minions; new allotment: plains 2+2, castle 3+3, volcano 4+4 (+ drafted reinforcements)
            s.characters.RemoveAll(c => c.IsMinion);
            s.minionGraveyard = 0;
            int allot = phase + 1;
            s.minionReserve = allot + s.bonusReserveNextPhase;
            s.bonusReserveNextPhase = 0;

            // Heroes on rows 0-1, Villain on row 13 (placeholder squares within the legal setup rows)
            var heroSquares = new Dictionary<Role, Vector2Int>
            {
                { Role.Fighter, new Vector2Int(3, 1) }, { Role.Wizard, new Vector2Int(4, 0) },
                { Role.Monk, new Vector2Int(5, 1) },    { Role.Cleric, new Vector2Int(6, 0) },
                { Role.Villain, new Vector2Int(4, 13) },
            };
            foreach (var c in s.characters)
            {
                c.pos = heroSquares[c.role];
                c.onBoard = true;
                c.effects.Clear();
                c.stunned = false;
                c.echoBonus = 0;
                c.maxPoints = CardCatalog.MaxPointsFor(c, phase);
                c.points = c.maxPoints;                         // everyone refills points at phase start
                if (c.IsHero) c.hp = c.maxHp;                   // heroes reset HP; the Villain's carries over
            }

            // Villain places the starting minions anywhere in rows 10-14 (placeholder: random empty squares)
            for (int i = 0; i < allot; i++)
            {
                Vector2Int sq;
                do sq = new Vector2Int(s.dice.Range(0, GameState.Columns), s.dice.Range(10, GameState.Rows));
                while (!s.IsEmpty(sq));
                s.SpawnMinion(sq);
            }

            // Phase 1 only: keep the basic attack, shuffle the rest, draw `openingDraw` (Block E). Villain gets all 3.
            if (phase == 1)
            {
                foreach (var c in s.characters.Where(c => !c.IsMinion))
                {
                    var basic = c.deck.First(card => card.isBasic);
                    c.deck.Remove(basic);
                    c.hand.Add(basic);
                    if (c.role == Role.Villain) { c.hand.AddRange(c.deck); c.deck.Clear(); continue; }
                    s.dice.Shuffle(c.deck);
                    for (int i = 0; i < s.lab.openingDraw && c.deck.Count > 0; i++)
                    {
                        c.hand.Add(c.deck[0]);
                        c.deck.RemoveAt(0);
                    }
                }
            }

            log($"=== PHASE {phase}: {s.Terrain}. {allot} minions on the board, {s.minionReserve} in reserve. Round marker = 1 ===");
            foreach (var c in s.characters.Where(c => !c.IsMinion))
                log($"    {c.Status()}  hand: {string.Join(", ", c.hand.Select(h => h.name))}");
            foreach (var m in s.MinionsOnBoard) log($"    {m.Status()}");
        }

        // ================= The state machine =================

        void TransitionTo(TurnPhase next)
        {
            if (!Transitions[Phase].Contains(next))
                throw new InvalidOperationException($"Illegal phase transition {Phase} -> {next}");
            Phase = next;
        }

        // Runs the current phase, then moves to the next one. Returns false once the game is over.
        public bool Advance()
        {
            if (State.gameOver) return false;

            LastChosen = null;
            LastLegal = LegalMoves.For(State, State.CurrentActor, Phase);

            switch (Phase)
            {
                case TurnPhase.StartOfTurn: StartOfTurn(); break;
                case TurnPhase.Choice:      Choice(); break;
                case TurnPhase.Resolution:
                    ActionResolver.Resolve(State, pending, log);
                    TransitionTo(TurnPhase.Payment);
                    break;
                case TurnPhase.Payment:     Payment(); break;
                case TurnPhase.EndOfTurn:   EndOfTurn(); break;
                case TurnPhase.Check:       Check(); break;
            }
            return !State.gameOver;
        }

        // ---- Phase 1 ----
        void StartOfTurn()
        {
            Active = State.CurrentActor;
            Active.actionReady = true;            // flip the action token to "ready"
            Active.hasMoved = false;
            Active.frozenThisTurn = false;

            // Start-of-turn counters (Defensive Stance, Judgement). Frozen applies to THIS turn before it ticks.
            foreach (var e in Active.effects.Where(e => e.tick == EffectTick.StartOfTurn).ToList())
            {
                if (e.frozen) Active.frozenThisTurn = true;
                e.counter--;
                if (e.counter <= 0) { Active.effects.Remove(e); log($"{Active.name}'s {e.name} wears off"); }
            }

            if (Active.stunned)
            {
                Active.stunned = false;
                log($"{Active.name} is STUNNED and loses this turn -> Phase 5");
                TransitionTo(TurnPhase.EndOfTurn);
                return;
            }

            if (Active.IsHero && Active.IsDefeated)
            {
                // Echo turn: placeholder picks the first living ally
                var ally = State.Heroes.FirstOrDefault(h => h.IsActive);
                if (ally != null) { ally.echoBonus = State.lab.echoBonus; log($"{Active.name} (defeated) takes an Echo turn: {ally.name} gets +{State.lab.echoBonus} on its next d20"); }
                TransitionTo(TurnPhase.EndOfTurn);
                return;
            }

            if (Active.frozenThisTurn) log($"{Active.name} is Frozen this turn: no Move or Reposition");
            TransitionTo(TurnPhase.Choice);
        }

        // ---- Phase 2 ----
        void Choice()
        {
            var choice = decider.Choose(State, Active, Phase, LastLegal);
            if (choice == null || !LastLegal.Contains(choice))
                throw new InvalidOperationException($"Decision maker returned an action that is not in the legal list: {choice}");

            LastChosen = choice;
            if (choice.IsEndTurn)
            {
                TransitionTo(TurnPhase.EndOfTurn);
                return;
            }
            pending = choice;
            TransitionTo(TurnPhase.Resolution);
        }

        // ---- Phase 4 ----
        void Payment()
        {
            ActionResolver.Pay(State, pending, log);
            pending = null;

            bool somethingLeft = Active.IsActive && (!Active.hasMoved || Active.actionReady);
            TransitionTo(somethingLeft ? TurnPhase.Choice : TurnPhase.EndOfTurn);
        }

        // ---- Phase 5 ----
        void EndOfTurn()
        {
            foreach (var e in Active.effects.Where(e => e.tick == EffectTick.EndOfTurn).ToList())
            {
                e.counter--;
                if (e.counter <= 0) { Active.effects.Remove(e); log($"{Active.name}'s {e.name} wears off"); }
            }

            // Hand limit 5: return extras to the deck
            while (Active.hand.Count > State.lab.handLimit)
            {
                var extra = Active.hand[Active.hand.Count - 1];
                Active.hand.RemoveAt(Active.hand.Count - 1);
                Active.deck.Add(extra);
                log($"{Active.name} is over the hand limit and returns {extra.name}");
            }

            // Round marker moves after the 4th Villain turn of the round
            if (Active.role == Role.Villain && ++State.villainTurnsThisRound == 4)
            {
                State.villainTurnsThisRound = 0;
                State.round++;
                State.roundsCompletedTotal++;
                log($"--- Round complete. Round marker -> {State.round} ---");
            }

            // PASS: after a hero, always the Villain; after the Villain, the next hero.
            State.turnsTaken++;
            State.turnIndex = (State.turnIndex + 1) % State.turnOrder.Count;
            log($"Play passes to the {State.CurrentActor.name}");
            TransitionTo(TurnPhase.Check);
        }

        // ---- Phase 6 ----
        void Check()
        {
            var s = State;

            // A draft from an ended phase is still being picked: one pick per Advance()
            if (s.DraftPending)
            {
                var pick = decider.Choose(s, s.CurrentActor, Phase, LastLegal);
                if (pick != null && LastLegal.Contains(pick)) { LastChosen = pick; ActionResolver.Resolve(s, pick, log); }
                else { s.heroDraftPicksLeft = 0; s.villainDraftPicksLeft = 0; }   // no legal pick left (empty decks)
                if (!s.DraftPending) FinishPhase();
                return;   // stay in Check until the draft is done
            }

            if (s.Villain.IsDefeated) { EndGame(Faction.Heroes, "The Villain is at 0 HP"); return; }

            bool heroesWin = s.MinionsOnBoard.Count == 0;
            bool villainWins = s.Heroes.All(h => h.IsDefeated);
            bool roundCap = s.round > 4;

            if (heroesWin || villainWins || roundCap)
            {
                Faction phaseWinner;
                if (heroesWin != villainWins && !roundCap) phaseWinner = heroesWin ? Faction.Heroes : Faction.Villain;
                else phaseWinner = TieBreak(s);

                log($"=== Phase {s.phaseNumber} ({s.Terrain}) ends: {phaseWinner} win the phase " +
                    $"({(heroesWin ? "no minions on the board" : villainWins ? "all heroes defeated" : "round cap")}) ===");
                s.previousPhaseLoser = phaseWinner == Faction.Heroes ? Faction.Villain : Faction.Heroes;

                if (s.phaseNumber == 3) { EndGame(s.Villain.IsDefeated ? Faction.Heroes : Faction.Villain, "The third phase has ended"); return; }

                // Winner drafts 3, loser 1
                s.heroDraftPicksLeft = phaseWinner == Faction.Heroes ? 3 : 1;
                s.villainDraftPicksLeft = phaseWinner == Faction.Villain ? 3 : 1;
                s.heroPicksThisDraft.Clear();
                log("Draft: heroes " + s.heroDraftPicksLeft + " pick(s), Villain " + s.villainDraftPicksLeft + " pick(s)");
                return;   // stay in Check; the next Advance() makes the picks
            }

            TransitionTo(TurnPhase.StartOfTurn);
        }

        void FinishPhase()
        {
            SetUpPhase(State, State.phaseNumber + 1, log);
            TransitionTo(TurnPhase.StartOfTurn);
        }

        static Faction TieBreak(GameState s)
        {
            int heroes = s.Heroes.Count(h => h.IsActive);
            int villainSide = s.MinionsOnBoard.Count + (s.Villain.IsDefeated ? 0 : 1);
            if (heroes != villainSide) return heroes > villainSide ? Faction.Heroes : Faction.Villain;
            return s.previousPhaseLoser ?? Faction.Villain;   // phase 1 tie: the Villain
        }

        void EndGame(Faction winner, string reason)
        {
            State.gameOver = true;
            State.winner = winner;
            log($"=== GAME OVER: {reason}. {winner} win. ===");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Convergence.Rules
{
    // Phase 3 (Resolution) and Phase 4 (Payment) of the turn phase card.
    // Every roll goes through state.dice (the one seeded DiceService).
    public static class ActionResolver
    {
        // ================= Phase 3: Resolution =================

        public static void Resolve(GameState s, GameAction act, Action<string> log)
        {
            var a = act.actor;
            switch (act.verb)
            {
                case PlayerAction.Move:
                    log($"{a.name} moves {GameAction.Sq(a.pos)} -> {GameAction.Sq(act.square.Value)}");
                    a.pos = act.square.Value;
                    a.hasMoved = true;
                    break;

                case PlayerAction.Strike: Strike(s, act, log); break;

                case PlayerAction.Reposition:
                    if (act.card.swap)
                    {
                        (a.pos, act.target.pos) = (act.target.pos, a.pos);
                        log($"{a.name} swaps places with {act.target.name}");
                    }
                    else
                    {
                        log($"{a.name} teleports to {GameAction.Sq(act.square.Value)} (normal move kept)");
                        a.pos = act.square.Value;
                    }
                    break;

                case PlayerAction.Support: Support(s, act, log); break;
                case PlayerAction.Disable: Disable(s, act, log); break;

                case PlayerAction.Rest:
                    int restore = act.card != null ? act.card.restorePoints : 1;
                    int before = a.points;
                    a.points = Mathf.Min(a.maxPoints, a.points + restore);
                    log($"{a.name} rests: +{a.points - before} point(s) ({a.points}/{a.maxPoints})");
                    break;

                case PlayerAction.Summon:
                    s.minionReserve--;
                    var m = s.SpawnMinion(act.square.Value);
                    log($"{a.name} summons {m.name} at {GameAction.Sq(m.pos)} ({s.minionReserve} left in reserve)");
                    break;

                case PlayerAction.Command:
                    foreach (var minion in act.minions) CommandMinion(s, minion, log);
                    break;

                case PlayerAction.Draft: Draft(s, act, log); break;
            }
        }

        // ---------------- Strike ----------------

        static void Strike(GameState s, GameAction act, Action<string> log)
        {
            var a = act.actor;
            var card = act.card;
            var targets = StrikeTargets(s, act);

            // One d20 for every target of the card
            int d20 = s.dice.Roll(s.lab.attackDie);
            int echo = a.echoBonus;
            a.echoBonus = 0;
            int total = d20 + a.attackBonus + echo;

            var hits = new List<Character>();
            foreach (var t in targets)
            {
                bool hit = Hits(s.lab, d20, total, t.Defense);
                log($"{a.name} {card.name} vs {t.name}: d20 {d20} + {a.attackBonus}{(echo > 0 ? $" + echo {echo}" : "")} = {total} vs Def {t.Defense} -> {(hit ? "HIT" : "MISS")}");
                if (hit) hits.Add(t);
            }
            if (hits.Count == 0) return;

            // One damage roll for everyone hit
            var faces = new List<int>();
            int dmg = s.dice.Roll(card.damage, faces) + a.DamageBonus;
            log($"    damage {card.damage} [{string.Join("+", faces)}]{(a.DamageBonus > 0 ? $" + {a.DamageBonus} bonus" : "")} = {dmg}");
            foreach (var t in hits) Damage(t, dmg, log);

            // Pushes, farthest from the attacker first
            if (card.push > 0)
            {
                foreach (var t in hits.OrderByDescending(t => GameState.Orthogonal(a.pos, t.pos)))
                {
                    if (!t.IsActive) continue;
                    var dir = act.direction != Vector2Int.zero ? act.direction : Dir(a.pos, t.pos);
                    Push(s, t, dir, card.push, log);
                }
            }

            // Lightning Strike: same damage to everyone adjacent to the main target, except the caster, no roll
            if (card.arcsToAdjacent && hits.Count > 0)
            {
                var main = hits[0];
                foreach (var c in s.characters.ToList())
                {
                    if (c == a || c == main || !c.IsActive || !GameState.Adjacent(main.pos, c.pos)) continue;
                    log($"    lightning arcs to {c.name}");
                    Damage(c, dmg, log);
                }
            }
        }

        static List<Character> StrikeTargets(GameState s, GameAction act)
        {
            var a = act.actor;
            switch (act.card.targeting)
            {
                case Targeting.AllAdjacent:
                    return s.characters.Where(c => c != a && c.IsActive && GameState.Adjacent(a.pos, c.pos)).ToList();
                case Targeting.LinePierce:
                    return s.LineSquares(a.pos, act.direction, act.card.range)
                            .Select(s.CharacterAt).Where(c => c != null).ToList();
                default:
                    return new List<Character> { act.target };
            }
        }

        public static bool Hits(DiceLabConfig lab, int d20, int total, int defense)
        {
            if (lab.natural20AlwaysHits && d20 == lab.attackDie) return true;
            if (lab.natural1AlwaysMisses && d20 == 1) return false;
            return total >= defense;
        }

        static void Damage(Character t, int dmg, Action<string> log)
        {
            t.hp = Mathf.Max(0, t.hp - dmg);
            log($"    {t.name} takes {dmg} ({t.hp}/{t.maxHp} HP)");
        }

        static Vector2Int Dir(Vector2Int from, Vector2Int to) =>
            new Vector2Int(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));

        static void Push(GameState s, Character t, Vector2Int dir, int squares, Action<string> log)
        {
            var dest = t.pos;
            for (int i = 0; i < squares; i++)
            {
                var next = dest + dir;
                if (!s.IsEmpty(next)) break;      // wall, edge or another character
                dest = next;
            }
            if (dest == t.pos) { log($"    {t.name} is pinned and can't be pushed"); return; }
            log($"    {t.name} is pushed to {GameAction.Sq(dest)}");
            t.pos = dest;
        }

        // ---------------- Support ----------------

        static void Support(GameState s, GameAction act, Action<string> log)
        {
            var a = act.actor;
            var card = act.card;

            if (!card.heal.IsNone)
            {
                var faces = new List<int>();
                int amount = s.dice.Roll(card.heal, faces);
                IEnumerable<Character> who = card.targeting == Targeting.AllLivingHeroes ? s.Heroes.Where(h => h.IsActive)
                                           : card.targeting == Targeting.Self ? new[] { a }
                                           : new[] { act.target };
                foreach (var t in who)
                {
                    int before = t.hp;
                    t.hp = Mathf.Min(t.maxHp, t.hp + amount);
                    log($"{a.name} {card.name}: {card.heal} [{string.Join("+", faces)}] -> {t.name} +{t.hp - before} HP ({t.hp}/{t.maxHp})");
                }
            }

            if (card.buffName != null)
            {
                var target = card.targeting == Targeting.Self ? a : act.target;
                ApplyEffect(target, card, log);
            }
        }

        static void ApplyEffect(Character t, CardDef card, Action<string> log)
        {
            var existing = t.GetEffect(card.buffName);
            if (existing != null)
            {
                existing.counter = Mathf.Max(existing.counter, card.buffTurns);   // refresh, never stack
                log($"    {t.name}'s {card.buffName} refreshed ({existing.counter})");
                return;
            }
            t.effects.Add(new Effect
            {
                name = card.buffName, counter = card.buffTurns, tick = card.buffTick,
                damage = card.buffDamage, move = card.buffMove, defense = card.buffDefense,
                frozen = card.buffName == "Frozen"
            });
            log($"    {t.name} gains {card.buffName} ({card.buffTurns})");
        }

        // ---------------- Disable ----------------

        static void Disable(GameState s, GameAction act, Action<string> log)
        {
            var t = act.target;
            if (act.card.stun)
            {
                int d20 = s.dice.Roll(20);
                int total = d20 + t.con;
                bool stunned = total < s.lab.stunSaveDC;
                log($"{act.actor.name} Stunning Strike: {t.name} saves d20 {d20} + {t.con} = {total} vs DC {s.lab.stunSaveDC} -> {(stunned ? "STUNNED" : "resisted")}");
                if (stunned) t.stunned = true;
            }
            else
            {
                log($"{act.actor.name} {act.card.name} on {t.name}");
                ApplyEffect(t, act.card, log);
            }
        }

        // ---------------- Command (placeholder minion steering) ----------------

        // Each minion moves up to 4 toward the nearest hero, then Bites one adjacent hero if it can.
        // Where it walks and who it bites are placeholder decisions, not rules.
        static void CommandMinion(GameState s, Character m, Action<string> log)
        {
            if (!m.IsActive) return;
            var prey = s.NearestEnemy(m);
            if (prey != null && !GameState.Adjacent(m.pos, prey.pos))
            {
                var options = s.Reachable(m.pos, m.Movement);
                if (options.Count > 0)
                {
                    var best = options.OrderBy(p => GameState.Orthogonal(p, prey.pos)).ThenBy(p => p.y).ThenBy(p => p.x).First();
                    if (GameState.Orthogonal(best, prey.pos) < GameState.Orthogonal(m.pos, prey.pos))
                    {
                        log($"{m.name} (commanded) moves {GameAction.Sq(m.pos)} -> {GameAction.Sq(best)}");
                        m.pos = best;
                    }
                }
            }

            var victim = s.characters.FirstOrDefault(c => c.IsActive && c.IsHero && GameState.Adjacent(m.pos, c.pos));
            if (victim == null) { log($"{m.name} has nobody to bite"); return; }

            int d20 = s.dice.Roll(s.lab.attackDie);
            int total = d20 + m.attackBonus;
            bool hit = Hits(s.lab, d20, total, victim.Defense);
            log($"{m.name} bites {victim.name}: d20 {d20} + {m.attackBonus} = {total} vs Def {victim.Defense} -> {(hit ? "HIT" : "MISS")}");
            if (hit) Damage(victim, s.dice.Roll(s.lab.minionBite), log);
        }

        // ---------------- Draft ----------------

        static void Draft(GameState s, GameAction act, Action<string> log)
        {
            if (act.draftCard != null)
            {
                var h = act.draftFor;
                h.deck.Remove(act.draftCard);
                h.hand.Add(act.draftCard);
                s.heroPicksThisDraft.TryGetValue(h, out int n);
                s.heroPicksThisDraft[h] = n + 1;
                s.heroDraftPicksLeft--;
                log($"Heroes draft {act.draftCard.name} for the {h.name}");
                return;
            }

            var v = s.Villain;
            switch (Array.IndexOf(CardCatalog.VillainDraftOptions, act.villainDraftOption))
            {
                case 0: s.bonusReserveNextPhase += 2; break;
                case 1: v.hp = Mathf.Min(v.maxHp, v.hp + 8); break;
                case 2: v.maxPoints = Mathf.Min(5, v.maxPoints + 1); break;
            }
            s.villainDraftPicksLeft--;
            log($"Villain drafts: {act.villainDraftOption}");
        }

        // ================= Phase 4: Payment or consequence =================

        public static void Pay(GameState s, GameAction act, Action<string> log)
        {
            var a = act.actor;

            if (act.verb != PlayerAction.Move && act.verb != PlayerAction.Draft)
            {
                if (act.Cost > 0)
                {
                    a.points -= act.Cost;
                    log($"{a.name} pays {act.Cost} point(s) ({a.points}/{a.maxPoints} left)");
                }
                a.actionReady = false;
                log($"{a.name}'s action token flips to SPENT");

                if (act.card != null && act.card.singleUse)
                {
                    a.hand.Remove(act.card);
                    log($"{act.card.name} goes back to the box");
                }
            }

            RemoveDefeated(s, log);
        }

        public static void RemoveDefeated(GameState s, Action<string> log)
        {
            foreach (var c in s.characters.ToList())
            {
                if (!c.onBoard || !c.IsDefeated) continue;

                if (c.IsMinion)
                {
                    s.characters.Remove(c);
                    s.minionGraveyard++;
                    log($"{c.name} is defeated -> Villain's graveyard ({s.minionGraveyard})");
                }
                else if (c.IsHero)
                {
                    c.onBoard = false;
                    c.effects.Clear();
                    log($"{c.name} is defeated -> Fallen area");
                    if (s.minionGraveyard > 0)
                    {
                        s.minionGraveyard--;
                        s.minionReserve++;
                        log($"    Villain returns a minion from the graveyard to the reserve ({s.minionReserve})");
                    }
                }
                // Villain at 0 HP stays put; Phase 6 ends the game.
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Convergence.Rules
{
    // Character stats (GDD v4.0 Section 4.1) and decks (Part 8, Appendix A).
    // Dice that the dice lab decided (basic melee, Heal/Meditation, minion stats, Stun DC)
    // are read from DiceLabConfig so there is only one place to change them.
    public static class CardCatalog
    {
        public static List<CardDef> DeckFor(Role role, DiceLabConfig lab)
        {
            switch (role)
            {
                case Role.Fighter: return new List<CardDef>
                {
                    Attack("Great Sword Attack", 0, Targeting.Adjacent, 0, lab.basicMeleeDamage, basic: true),
                    SelfHeal("Second Wind", 1, new DiceSpec(1, 12)),
                    Teleport("Tactical Shift", 1, 5),
                    Attack("Bow Shot", 0, Targeting.Line, 5, new DiceSpec(1, 4)),
                    SelfBuff("Rage", 1, "Rage", 3, damage: 5, move: 1, refresh: false),
                    SingleUse(SelfHeal("Health Pot", 0, new DiceSpec(0, 0, 2))),
                };

                case Role.Monk: return new List<CardDef>
                {
                    Attack("Unarmed Strike", 0, Targeting.Adjacent, 0, lab.basicMeleeDamage, basic: true),
                    SelfBuff("Defensive Stance", 1, "Defensive Stance", 3, defense: 3, tick: EffectTick.StartOfTurn),
                    new CardDef { name = "Stunning Strike", verb = PlayerAction.Disable, cost = 1, targeting = Targeting.Adjacent, stun = true },
                    SelfHeal("Meditation", 1, lab.healDice),
                    Attack("Roundhouse Kick", 2, Targeting.AllAdjacent, 0, new DiceSpec(2, 12)),
                    new CardDef { name = "Swap", verb = PlayerAction.Reposition, cost = 1, targeting = Targeting.Ally, range = 0, swap = true },
                };

                case Role.Wizard: return new List<CardDef>
                {
                    Attack("Spitfire Cantrip", 0, Targeting.Range, 6, new DiceSpec(1, 4), basic: true),
                    Attack("Acid Lick", 0, Targeting.Range, 2, new DiceSpec(1, 6)),
                    Teleport("Misty Step", 1, 8),
                    Push(Attack("Bubble Beam", 1, Targeting.LinePierce, 5, new DiceSpec(1, 8)), 1),
                    Arc(Attack("Lightning Strike", 1, Targeting.Range, 6, new DiceSpec(2, 6))),
                    new CardDef { name = "Surge", verb = PlayerAction.Support, cost = 1, targeting = Targeting.Ally, range = 6,
                                  buffName = "Surge", buffTurns = 1, buffDamage = 3 },
                };

                // Block E says the Cleric deck is 5 cards (basic + draw 2 of the other 4). Appendix A has since
                // grown Beacon of Hope / Restore / Greater Restore, but those were not on the dice lab sheet,
                // so they stay out until the team updates Block E at the table.
                case Role.Cleric: return new List<CardDef>
                {
                    Attack("Staff Strike", 0, Targeting.Adjacent, 0, new DiceSpec(1, 4), basic: true),
                    new CardDef { name = "Heal", verb = PlayerAction.Support, cost = 1, targeting = Targeting.Ally, range = 10,
                                  heal = lab.healDice, allyIncludesSelf = true },
                    new CardDef { name = "Mass Heal", verb = PlayerAction.Support, cost = 3, targeting = Targeting.AllLivingHeroes,
                                  heal = new DiceSpec(3, 12) },
                    new CardDef { name = "Haste", verb = PlayerAction.Support, cost = 0, targeting = Targeting.Ally, range = 0,
                                  buffName = "Haste", buffTurns = 3, buffMove = 2 },
                    new CardDef { name = "Judgement", verb = PlayerAction.Disable, cost = 1, targeting = Targeting.Enemy, range = 6,
                                  buffName = "Frozen", buffTurns = 2, buffTick = EffectTick.StartOfTurn },
                };

                case Role.Villain: return new List<CardDef>
                {
                    Attack("Eldritch Blast", 0, Targeting.Range, 7, new DiceSpec(1, 12), basic: true),
                    Push(Attack("Force Blast", 1, Targeting.Line, 3, new DiceSpec(4, 4)), 2),
                    new CardDef { name = "Short Rest", verb = PlayerAction.Rest, cost = 0, targeting = Targeting.Self, restorePoints = 2 },
                };
            }
            return new List<CardDef>();
        }

        // name, HP, Defense, attack bonus, Con, Move, max points (Section 4.1 table)
        public static Character Create(Role role)
        {
            switch (role)
            {
                case Role.Fighter: return Make("Fighter", role, Faction.Heroes, 30, 13, 3, 2, 5, 2);
                case Role.Monk:    return Make("Monk", role, Faction.Heroes, 24, 12, 3, 1, 6, 1);   // 1/2/3 by phase
                case Role.Cleric:  return Make("Cleric", role, Faction.Heroes, 24, 11, 2, 1, 5, 3);
                case Role.Wizard:  return Make("Wizard", role, Faction.Heroes, 18, 10, 2, -1, 5, 3);
                default:           return Make("Villain", Role.Villain, Faction.Villain, 40, 12, 2, 1, 4, 3);
            }
        }

        public static int MaxPointsFor(Character c, int phase) => c.role == Role.Monk ? Mathf.Clamp(phase, 1, 3) : c.maxPoints;

        public static readonly string[] VillainDraftOptions = { "+2 minions in next reserve", "Restore 8 HP", "+1 max spell point" };

        // ---------------- helpers ----------------

        static Character Make(string name, Role role, Faction f, int hp, int def, int atk, int con, int move, int pts) =>
            new Character { name = name, role = role, faction = f, maxHp = hp, hp = hp, baseDefense = def,
                            attackBonus = atk, con = con, baseMove = move, maxPoints = pts, points = pts };

        static CardDef Attack(string name, int cost, Targeting t, int range, DiceSpec dmg, bool basic = false) =>
            new CardDef { name = name, verb = PlayerAction.Strike, cost = cost, targeting = t, range = range, damage = dmg, isBasic = basic };

        static CardDef SelfHeal(string name, int cost, DiceSpec heal) =>
            new CardDef { name = name, verb = PlayerAction.Support, cost = cost, targeting = Targeting.Self, heal = heal };

        static CardDef SelfBuff(string name, int cost, string buff, int turns, int damage = 0, int move = 0, int defense = 0,
                                EffectTick tick = EffectTick.EndOfTurn, bool refresh = true) =>
            new CardDef { name = name, verb = PlayerAction.Support, cost = cost, targeting = Targeting.Self, buffName = buff,
                          buffTurns = turns, buffDamage = damage, buffMove = move, buffDefense = defense, buffTick = tick,
                          buffStacksRefresh = refresh };

        static CardDef Teleport(string name, int cost, int range) =>
            new CardDef { name = name, verb = PlayerAction.Reposition, cost = cost, targeting = Targeting.EmptySquare, range = range };

        static CardDef SingleUse(CardDef c) { c.singleUse = true; return c; }
        static CardDef Push(CardDef c, int squares) { c.push = squares; return c; }
        static CardDef Arc(CardDef c) { c.arcsToAdjacent = true; return c; }
    }
}

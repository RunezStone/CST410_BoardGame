# Convergence — Dice Lab numbers used by Build 4

Source: Thursday Dice and Chips Probability Lab, GDD v4.0 Section 4.7.
**Change the numbers in `Resources/DiceLab.asset` (select it in the Project window and use the Inspector), not in code.**
The driver prints this table, computed live from the asset, at the start of every run.

| Block | What is random | Die | Target | Odds (counted) |
|---|---|---|---|---|
| A | Attack roll | d20 + attack bonus | ≥ Defense; nat 20 hits, nat 1 misses | +2 vs minion (Def 9): 14/20 = 70.0% · +3 vs minion: 15/20 = 75.0% · +2 vs Villain (Def 12): 11/20 = 55.0% · minion +1 vs Wizard (Def 10): 12/20 = 60.0% · vs Fighter (Def 13): 9/20 = 45.0% |
| B | Basic melee vs minion (Great Sword, Unarmed Strike) | 2d4 | ≥ 6 (minion HP 6) | 6/16 = 37.5% |
| C | Healing (Heal, Meditation) | 2d6 | ≥ 7 | 21/36 = 58.3% |
| D | Stunning Strike save | d20 + Con | stunned if **below 12** | minion (Con 0): 11/20 = 55.0% · Villain (Con +1): 10/20 = 50.0% |
| E | Opening hand | shuffle 5 non-basic cards, draw 2 | Fighter holds a heal | 1 − C(3,2)/C(5,2) = 7/10 = 70.0% |

Other values in the asset: minion Defense 9, HP 6, attack +1, Move 4, Bite 1d6; hand limit 5; Echo token +1.

## One seed

Every random number in the game comes from `DiceService.Main`, seeded from `seed` in `DiceLab.asset`
(default 410; the driver's *Seed Override* field can change it for one run). This covers card dice, virtual
rolls in `ActionCard`, the throw forces in `DiceScript`, opening-hand shuffles, minion setup squares and
the placeholder decisions. Same seed → same game, line for line (the headless check runs it twice and compares).

The physical dice prefabs still land wherever Unity's physics puts them, so a game that uses the 3D dice is
not frame-perfect repeatable; the Build 4 driver uses only the seeded service, so it is.

## Generator check

The driver rolls 2d6 × 200 and compares against the expected 1-2-3-4-5-6-5-4-3-2-1 shape
(5.6, 11.1, 16.7, 22.2, 27.8, 33.3, 27.8, …). It fails if chi-square reaches 18.31 (10 d.f., p = 0.05).

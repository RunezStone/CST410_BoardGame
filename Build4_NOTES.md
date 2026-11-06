# Build 4 — Turn structure, legal moves, dice lab

Tag: `build-4` on `main`.
Driver scene: `Assets/Ysabelle/Build4/Build4Driver.unity` (press Play).
Code: `Assets/Ysabelle/Build4/` (rules in `Rules/`, driver in `TurnDriver.cs`, editor menu in `Editor/Build4Tools.cs`).

## What maps to which table artifact

| Requirement | Table artifact | Where in the build |
|---|---|---|
| Action enum | Verb wall (9 verbs: Move, Strike, Reposition, Support, Disable, Rest, Summon, Command, Draft) | `Rules/PlayerAction.cs` → `PlayerAction` |
| Legal moves | Phase card, Phase 2 (card in hand, token ready, enough points, legal target in range) | `Rules/LegalMoves.cs` → `LegalMoves.For(state, player, phase)` |
| Turn state machine | Turn phase card (1 Start, 2 Choice, 3 Resolution, 4 Payment, 5 End of turn, 6 Check) and turn order F-V-W-V-M-V-C-V | `Rules/TurnPhase.cs`, `Rules/TurnStateMachine.cs` (`Transitions` table, `Advance()`) |
| Dice lab numbers | Probability lab Blocks A–E | `Resources/DiceLab.asset` (Inspector), `DICE_LAB.md`, `Rules/DiceLabConfig.cs` |
| One seeded RNG | — | `Rules/DiceService.cs`; `ActionCard.RollDice` and `DiceScript` throw forces now use it too |
| Driver | — | `TurnDriver.cs` + `Build4Driver.unity`; `Convergence > Build 4 > Run Headless` runs 2 rounds twice and checks the logs match |

## Where the table and the code disagreed

- **"End turn" is on the phase card but not on the verb wall.** It is a Phase 2 choice but not a verb, so it is not in `PlayerAction`; it is a separate `GameAction.EndTurn` entry in the legal list.
- **Bite is not a verb.** At the table it felt like one, but it only happens inside Command, so it lives in the Command resolution.
- **Rest is two different actions once written down:** the basic Rest (+1, everyone) and the Villain's Short Rest card (+2). Both are `PlayerAction.Rest`; the card version carries its own restore amount.
- **Phases 2–4 loop.** The phase card prints 1→6 in order, but "one move and one action, in either order" means a turn can pass through Choice → Resolution → Payment twice. The state machine allows exactly one extra transition, Payment → Choice, and only while a move or the action is still unused.
- **Stunning Strike DC:** Appendix A says "below 15", the dice lab (Block D) and the v4.0 change log say DC 12. The build uses 12 (dice lab) until the team fixes Appendix A.
- **Cleric deck size:** Block E assumes a 5-card Cleric deck; Appendix A now lists 8 (Beacon of Hope, Restore, Greater Restore added). The build uses the 5 from the dice lab.
- **Judgement "2 of its turns" vs. a start-of-turn counter:** ticking 2 → 1 → 0 at the start of turns would only freeze one turn, so the freeze applies to the turn in which the counter ticks.
- **Live Fighter cards** didn't match v4.0 (Great Sword 1d4 → 2d4 per Block B, Second Wind flat 10 → 1d12, Bow Shot flat 4 / range 15 → 1d4 / range 5). Fixed in the scripts, prefabs and SampleScene.

## Credit note

- **Unity 6.6 (6000.6.0f1)** and its built-in packages (TextMesh Pro, Input System, ProBuilder, Test Framework).
- **QuickOutline** (`Assets/QuickOutline`), Chris Nolet, Unity Asset Store (free), MIT license.
- **Hand Painted Grass Texture** (`Assets/Art/Textures/Hand Painted Grass  Texture`), Unity Asset Store. [TEAM: confirm publisher]
- **Claude Code (Anthropic)** helped draft the Build 4 scripts in `Assets/Ysabelle/Build4/` and the Fighter card fixes; the team reviewed, tested and owns the result.
- Chi-square critical value 18.31 (10 d.f., p = 0.05) from a standard chi-square table.
- [TEAM: add any tutorials, forum snippets or sample code used this week]

## Contribution note

[TEAM: one sentence each, naming branches / scripts / assets. Edit before submitting.]

- **Ysabelle Trinidad** — Build 4 rules layer and driver (`Assets/Ysabelle/Build4/`: `PlayerAction`, `LegalMoves`, `TurnStateMachine`, `DiceService`, `DiceLabConfig`, `TurnDriver`), Fighter card fixes, merges to `main` and the `build-4` tag.
- **Jacob Napoles** — physical dice system (`Assets/Jacob/Scripts/DiceScript.cs`, `NumberLink.cs`, `SpaceButtonRoll.cs`, branches `Jacob`/`JacobDice`); [this week: …]
- **Jorge Salzar** — [branches `JorgeBranch`/`JorgeBranch2`; this week: …]
- **Gabriel Veltri** — board and player stats (`Assets/Scripts/TileManager.cs`, `Space.cs`, branch `Gabe`); [this week: …]

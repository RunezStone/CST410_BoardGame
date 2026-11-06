using System.Collections;
using System.Collections.Generic;
using System.Text;
using Convergence.Rules;
using UnityEngine;

// Build 4 driver (scene: Assets/Ysabelle/Build4/Build4Driver.unity).
// Seeds the one DiceService, prints the dice-lab numbers, then plays turns through every phase of
// the turn phase card, printing the legal action list at each phase. Placeholder decisions only.
// Output goes to the Console AND to the on-screen log (for the video).
public class TurnDriver : MonoBehaviour
{
    [Tooltip("Leave empty to load Resources/DiceLab.asset")]
    [SerializeField] DiceLabConfig diceLab;

    [Tooltip("Overrides the seed in DiceLab.asset when not 0")]
    [SerializeField] int seedOverride = 0;

    [Tooltip("Run at least this many full rounds (8 turns each)")]
    [SerializeField] int roundsToRun = 2;

    [Tooltip("Print every legal action at every phase of EVERY turn (long). Off = full list on the first turn, counts after.")]
    [SerializeField] bool fullListEveryTurn = false;

    [Tooltip("Pause between phases so the on-screen log can be followed. 0 = run instantly.")]
    [SerializeField] float secondsPerPhase = 0.25f;

    [Header("On-screen log")]
    [SerializeField] int fontSize = 15;
    [Tooltip("IMGUI can only draw ~16k characters in one label, so only the latest lines stay on screen (the Console keeps everything)")]
    [SerializeField] int maxLines = 120;

    readonly List<string> lines = new List<string>();
    Vector2 scroll;
    int errorCount;
    bool autoScroll = true;

    void OnEnable() => Application.logMessageReceived += CountErrors;
    void OnDisable() => Application.logMessageReceived -= CountErrors;

    void CountErrors(string msg, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errorCount++;
    }

    IEnumerator Start()
    {
        var lab = diceLab != null ? diceLab : DiceLabConfig.Load();
        DiceLabConfig.Use(lab);
        int seed = seedOverride != 0 ? seedOverride : lab.seed;

        foreach (var step in Run(lab, seed, roundsToRun, fullListEveryTurn, Print))
            if (secondsPerPhase > 0f && step) yield return new WaitForSeconds(secondsPerPhase);

        Print(errorCount == 0 ? "No errors logged during the run." : $"{errorCount} error(s) logged - see the Console.");
    }

    // The whole run as an iterator (yields true after each phase) so the Editor can also run it
    // headless without a scene: see Build4Tools.RunHeadless.
    public static IEnumerable<bool> Run(DiceLabConfig lab, int seed, int rounds, bool fullListEveryTurn, System.Action<string> print)
    {
        DiceService.Reseed(seed);
        var dice = DiceService.Main;

        print(lab.BuildReport());
        print(lab.RunHistogramCheck(out _));
        print("");
        print("Turn order (phase card): " + string.Join(" -> ", TurnStateMachine.TurnOrder));
        print("Phases (phase card): 1 StartOfTurn -> 2 Choice -> 3 Resolution -> 4 Payment -> (back to 2 while a move/action is left) -> 5 EndOfTurn [pass] -> 6 Check");
        print("");

        var state = TurnStateMachine.NewGame(lab, dice, print);
        var machine = new TurnStateMachine(state, new PlaceholderPolicy(), msg => print("      " + msg));

        int turnNumber = 0;
        while (!machine.GameOver && state.roundsCompletedTotal < rounds)
        {
            bool fullList = fullListEveryTurn || turnNumber == 0;
            var phase = machine.Phase;

            if (phase == TurnPhase.StartOfTurn)
            {
                turnNumber++;
                var who = state.CurrentActor;
                print("");
                print($"########## TURN {turnNumber}  |  Phase {state.phaseNumber} {state.Terrain}, round {state.round}  |  {who.name} ##########");
                print("    " + who.Status());
            }

            // Header + legal list first (same call the machine makes), then run the phase so its
            // dice and effects print underneath the phase they belong to.
            var actor = state.CurrentActor;
            string actorLabel = phase == TurnPhase.Check ? "the table" : actor.name;
            print($"  [{(int)phase}] {phase,-11} legal actions for {actorLabel}:");
            print(LegalMoves.Format(LegalMoves.For(state, actor, phase), fullList || phase == TurnPhase.Check));

            machine.Advance();

            if (machine.LastChosen != null) print($"    >> chose: {machine.LastChosen}");
            print($"    -> next phase: {machine.Phase}");

            yield return true;
        }

        print("");
        print(machine.GameOver
            ? $"Game ended after {turnNumber} turns."
            : $"Finished {state.roundsCompletedTotal} full round(s) = {state.turnsTaken} turns, through every phase, seed {seed}.");
        foreach (var c in state.characters) print("    " + c.Status());
        print($"    Dice rolled: {dice.RollCount}. Re-run with seed {seed} to get exactly this game again.");
    }

    void Print(string msg)
    {
        Debug.Log(msg);
        foreach (var line in msg.Split('\n'))
        {
            lines.Add(line.Length > 220 ? line.Substring(0, 217) + "..." : line);
            if (lines.Count > maxLines) lines.RemoveAt(0);
        }
        if (autoScroll) scroll.y = float.MaxValue;
    }

    GUIStyle style;

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = false, wordWrap = false };
            style.normal.textColor = Color.white;
            style.font = Font.CreateDynamicFontFromOSFont("Consolas", fontSize);
        }

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
        autoScroll = GUILayout.Toggle(autoScroll, " follow output");
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label(string.Join("\n", lines), style);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}

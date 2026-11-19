using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Keeps track of the Villain's minions: spawns them (Summon), enforces the cap,
// runs their turns right after the Villain's turn, and clears away dead ones.
// Put ONE of these in the scene and assign the minion prefab.
//
// Minion turns: each living minion gets its own turn (move once + 1 action: Bite), one after another,
// as part of the Villain's turn. They don't use the Villain's actions. The Villain player controls them:
// click a green square to move, then press the Bite button (or the bite key) and click a red square.
public class MinionManager : MonoBehaviour
{
    public static MinionManager instance;

    [Tooltip("Prefab with PlayerData (minion stats), PlayerManager and BiteCard. See Minion.prefab.")]
    [SerializeField] PlayerManager minionPrefab;

    [Tooltip("Shared canvas shown during each minion's turn, laid out like the players' canvases. " +
             "Build it with Convergence > Minions > Build Minion Canvas (copies the Warlock's canvas; its card slots " +
             "show the active minion's Bite and its End Turn calls EndActiveMinionTurn).")]
    [SerializeField] Canvas minionUICanvas;

    [Header("Keyboard fallback (works with or without the canvas)")]
    [SerializeField] KeyCode biteKey = KeyCode.B;
    [SerializeField] KeyCode endTurnKey = KeyCode.Return;

    readonly List<PlayerManager> minions = new List<PlayerManager>();
    PlayerManager activeMinion;
    int spawnCount;

    public PlayerManager ActiveMinion => activeMinion;

    void Awake()
    {
        instance = this;
        if (minionUICanvas != null) minionUICanvas.gameObject.SetActive(false);
    }

    // ---------------- Counting / cap ----------------

    public int Count { get { RemoveDeadMinions(); return minions.Count; } }

    // Max minions on the board = number of heroes on the board.
    // Downed heroes still stand on the board, so they count too.
    public int Cap
    {
        get
        {
            int heroes = 0;
            foreach (PlayerManager p in Object.FindObjectsByType<PlayerManager>())
                if (p.team == Team.HERO && p.currSpace != null) heroes++;
            return heroes;
        }
    }

    public int SlotsLeft => Mathf.Max(0, Cap - Count);

    // ---------------- Spawning ----------------

    public PlayerManager Spawn(Space space)
    {
        if (minionPrefab == null) { Debug.LogError("MinionManager has no minion prefab assigned."); return null; }
        if (space == null || space.IsBlocked() || space.CharacterExistsInSpace()) { Debug.Log("That square isn't free."); return null; }
        if (SlotsLeft <= 0) { Debug.Log($"Minion cap reached ({Cap})."); return null; }

        PlayerManager m = Instantiate(minionPrefab);
        m.name = $"Minion {++spawnCount}";
        m.team = Team.VILLAIN;
        m.isMinion = true;
        if (minionUICanvas != null) m.playerUICanvas = minionUICanvas;
        m.PlaceCharacter(space);

        minions.Add(m);
        Debug.Log($"{m.name} appears at [{space.id[0]}, {space.id[1]}]. ({minions.Count}/{Cap} minions)");
        return m;
    }

    // ---------------- Turns ----------------

    // Called by GameManager right after the Villain's turn.
    // onMinionTurnStart lets GameManager update the turn display / shared End Turn button.
    // stop is checked after each minion (e.g. the game ended).
    public IEnumerator RunMinionTurns(System.Action<PlayerManager> onMinionTurnStart, System.Func<bool> stop)
    {
        RemoveDeadMinions();

        foreach (PlayerManager m in new List<PlayerManager>(minions))
        {
            if (m == null || m.playerData.IsDead) continue;   // bitten to death earlier this round

            activeMinion = m;
            onMinionTurnStart?.Invoke(m);
            Debug.Log($"{m.name}'s turn: move, then Bite ({biteKey}) and End Turn ({endTurnKey}).");

            yield return m.PlayerTurn();

            activeMinion = null;
            if (stop != null && stop()) yield break;
        }
    }

    // Hook the minion canvas's Bite button here.
    public void BiteWithActiveMinion()
    {
        if (activeMinion == null) return;
        BiteCard bite = activeMinion.GetComponent<BiteCard>();
        if (bite == null) { Debug.LogError($"{activeMinion.name} has no BiteCard."); return; }
        bite.Play(activeMinion);
    }

    // Hook the minion canvas's End Turn button here.
    public void EndActiveMinionTurn()
    {
        if (activeMinion != null) activeMinion.EndTurn();
    }

    void Update()
    {
        if (activeMinion == null || !activeMinion.IsTurn) return;
        if (Input.GetKeyDown(biteKey)) BiteWithActiveMinion();
        else if (Input.GetKeyDown(endTurnKey)) EndActiveMinionTurn();
    }

    // ---------------- Cleanup ----------------

    // Defeated minions are removed from the board (never in the middle of their own turn).
    void LateUpdate()
    {
        RemoveDeadMinions();
    }

    void RemoveDeadMinions()
    {
        for (int i = minions.Count - 1; i >= 0; i--)
        {
            PlayerManager m = minions[i];
            if (m == null) { minions.RemoveAt(i); continue; }
            if (!m.playerData.IsDead || m.IsTurn) continue;

            if (m.currSpace != null) m.currSpace.RemoveObject();
            Debug.Log($"{m.name} is destroyed.");
            minions.RemoveAt(i);
            Destroy(m.gameObject);
        }
    }
}

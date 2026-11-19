using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public enum Team
{
    HERO,
    VILLAIN
}

public class PlayerManager : MonoBehaviour
{
    [Tooltip("Set this per character in the Inspector (Wizard, Fighter, Monk, Cleric = HERO; Warlock = VILLAIN)")]
    public Team team;

    [Tooltip("Set by MinionManager when it spawns a minion. Minions take their turns right after the Villain's.")]
    public bool isMinion = false;

    [SerializeField] bool isTurn = false;
    [SerializeField] bool hasMoved = false;          // one move per turn
    [SerializeField] bool turnEnded = false;         // set only by End Turn
    [SerializeField] bool isInTargetingMode = false;
    [SerializeField] bool skipNextTurn = false;      // set by Stun

    [Header("Actions")]
    [Tooltip("GDD: every character gets a single action per turn")]
    [SerializeField] int actionsPerTurn = 1;
    [SerializeField] int actionsLeft = 0;

    [Header("CurrentSpace")]
    public Space currSpace;

    [Header("User Interface")]
    public Canvas playerUICanvas;

    [Header("Player Reference Scripts")]
    public PlayerData playerData;
    [SerializeField] CardInventory inventory;
    [SerializeField] UIManager uiManager;

    [Header("Highlight Colors")]
    [SerializeField] Color moveColor = Color.green;
    [SerializeField] Color targetColor = Color.red;
    [SerializeField] Color normalColor = Color.white;

    private List<Space> validMovementSpaces = new List<Space>();
    private List<Space> validTargetSpaces = new List<Space>();
    private System.Action<Space, PlayerManager> targetingCallback;

    public bool IsTurn => isTurn;
    public int ActionsLeft => actionsLeft;
    public int ActionsPerTurn => actionsPerTurn;

    // Judgement: can't walk or teleport itself while this is on (pushes still move it).
    public bool IsFrozen => GetComponent<FrozenEffect>() != null;

    // True if `other` is on the same team (false for null). Attacks ignore this on purpose --
    // friendly fire is part of the game. Use it for support cards (Surge, Heal, Haste, Swap).
    public bool IsAllyOf(PlayerManager other) => other != null && other.team == team;

    void Update()
    {
        if (!isTurn) return;

        // Escape cancels targeting (right mouse is used by the camera)
        if (isInTargetingMode && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitTargetingMode();
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;

        // Ignore clicks on UI (End Turn, cards, etc.) so they don't also click the board
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Space clicked = GetClickedSpace();
        if (clicked == null) return;

        if (isInTargetingMode)
        {
            if (validTargetSpaces.Contains(clicked))
            {
                var callback = targetingCallback;
                ExitTargetingMode();             // exit first so the card can start a new targeting if it wants
                callback?.Invoke(clicked, this);
            }
        }
        else if (!hasMoved && validMovementSpaces.Contains(clicked))
        {
            PlaceCharacter(clicked);
        }
    }

    // Returns the tile under the mouse. Clicking a player counts as clicking the tile they stand on.
    Space GetClickedSpace()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return null;

        PlayerManager clickedPlayer = hit.collider.GetComponentInParent<PlayerManager>();
        if (clickedPlayer != null) return clickedPlayer.currSpace;

        return hit.collider.GetComponentInParent<Space>();
    }

    // Normal walk: uses up this turn's move.
    public void PlaceCharacter(Space space)
    {
        MoveTo(space);
        hasMoved = true;
        ClearMovementSpaces();   // only clears movement; targeting is untouched
    }

    // Card-driven move (Tactical Shift, Misty Step, Shadow Veil): does NOT use up the normal move.
    // Frozen characters can't reposition themselves; forced = true is for pushes, which still work.
    // Returns false if the move was refused.
    public bool Teleport(Space space, bool forced = false)
    {
        if (!forced && IsFrozen)
        {
            Debug.Log($"{name} is frozen and can't reposition.");
            return false;
        }
        MoveTo(space);
        RefreshMovementSpaces();   // green tiles follow the new position
        return true;
    }

    // Swap: both characters trade squares. Neither uses up their normal move.
    // Done in one step so neither square ever "holds" two characters at once.
    public void SwapPlaces(PlayerManager other)
    {
        if (other == null || other == this) return;
        if (IsFrozen || other.IsFrozen)
        {
            Debug.Log("A frozen character can't be swapped.");
            return;
        }

        Space mine = currSpace, theirs = other.currSpace;
        mine.RemoveObject();
        theirs.RemoveObject();

        currSpace = theirs;
        theirs.PlaceObject(gameObject);
        other.currSpace = mine;
        mine.PlaceObject(other.gameObject);

        RefreshMovementSpaces();
        other.RefreshMovementSpaces();
    }

    // Stun: this character loses their next turn (effects still tick, so Rage etc. count it).
    public void SkipThisTurn()
    {
        skipNextTurn = true;
    }

    void MoveTo(Space space)
    {
        if (currSpace != null)
            currSpace.RemoveObject();

        currSpace = space;
        currSpace.PlaceObject(gameObject);
    }

    // Recalculate the green move tiles (after a teleport, or when movement changes mid-turn).
    public void RefreshMovementSpaces()
    {
        if (!isTurn || hasMoved) return;

        Paint(validMovementSpaces, normalColor);
        validMovementSpaces = ComputeMovementSpaces();

        if (isInTargetingMode) Paint(validTargetSpaces, targetColor);   // aiming: keep only red showing
        else Paint(validMovementSpaces, moveColor);
    }

    // Where this character can walk this turn. Empty while Frozen.
    List<Space> ComputeMovementSpaces()
    {
        if (IsFrozen) return new List<Space>();
        return new List<Space>(TileManager.instance.GetValidMovementSpaces(currSpace, playerData.movement));
    }

    public void UseAction()
    {
        actionsLeft = Mathf.Max(0, actionsLeft - 1);
    }

    // For cards that grant an extra action this turn
    public void GrantAction(int amount = 1)
    {
        actionsLeft += amount;
    }

    // Timed effects (Haste) that change how many actions this character gets each turn.
    // If it's their turn right now, the change applies to this turn too.
    public void ModifyActionsPerTurn(int amount)
    {
        actionsPerTurn = Mathf.Max(0, actionsPerTurn + amount);
        if (isTurn) actionsLeft = Mathf.Max(0, actionsLeft + amount);
    }

    public void EndTurn()
    {
        if (!isTurn) return;
        if (isInTargetingMode) ExitTargetingMode();
        turnEnded = true;
    }

    // ---------------- Targeting ----------------

    public void EnterTargetingMode(List<Space> targetSpaces, System.Action<Space, PlayerManager> callback, PlayerManager playerManager)
    {
        if (!isTurn) return;
        if (isInTargetingMode) ExitTargetingMode();

        isInTargetingMode = true;
        validTargetSpaces = new List<Space>(targetSpaces);   // copy so we never clear the caller's list
        targetingCallback = callback;

        // Hide the green move tiles while aiming so only red targets show
        Paint(validMovementSpaces, normalColor);
        Paint(validTargetSpaces, targetColor);
    }

    public void ExitTargetingMode()
    {
        Paint(validTargetSpaces, normalColor);
        validTargetSpaces.Clear();
        targetingCallback = null;
        isInTargetingMode = false;

        // Bring the move tiles back if they haven't moved yet (attack first, then move)
        if (!hasMoved) Paint(validMovementSpaces, moveColor);
    }

    // ---------------- Turn ----------------

    public IEnumerator PlayerTurn()
    {
        isTurn = true;
        hasMoved = false;
        turnEnded = false;
        actionsLeft = actionsPerTurn;

        foreach (ITurnEffect effect in GetComponents<ITurnEffect>())
            effect.OnTurnStart();

        // Stunned: show it briefly, tick end-of-turn effects, and hand over to the next player
        if (skipNextTurn)
        {
            skipNextTurn = false;
            Debug.Log($"{name} is stunned and loses this turn!");
            yield return new WaitForSeconds(1f);

            foreach (ITurnEffect effect in GetComponents<ITurnEffect>())
                effect.OnTurnEnd();

            isTurn = false;
            yield break;
        }

        DisplayPlayerUI();

        if (IsFrozen) Debug.Log($"{name} is frozen and can't move this turn.");
        validMovementSpaces = ComputeMovementSpaces();
        Paint(validMovementSpaces, moveColor);

        // Player can move and play cards freely until they press End Turn
        yield return new WaitUntil(() => turnEnded);

        if (isInTargetingMode) ExitTargetingMode();
        ClearMovementSpaces();

        foreach (ITurnEffect effect in GetComponents<ITurnEffect>())
            effect.OnTurnEnd();

        isTurn = false;
        HidePlayerUI();
    }

    // ---------------- Highlighting ----------------

    void ClearMovementSpaces()
    {
        Paint(validMovementSpaces, normalColor);
        validMovementSpaces.Clear();

        // Re-apply red in case a target tile overlapped a move tile
        if (isInTargetingMode) Paint(validTargetSpaces, targetColor);
    }

    void Paint(List<Space> spaces, Color color)
    {
        foreach (Space space in spaces)
        {
            if (space == null) continue;
            Renderer r = space.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
        }
    }

    // ---------------- UI ----------------

    // Null-safe: minions may not have a canvas (they can use MinionManager's keyboard fallback).
    public void DisplayPlayerUI()
    {
        if (playerUICanvas != null) playerUICanvas.gameObject.SetActive(true);
    }

    public void HidePlayerUI()
    {
        // Close any open card panel too (the player's own, or the shared minion canvas's),
        // so it doesn't stay on screen into the next character's turn.
        CardUIManager cards = GetComponent<CardUIManager>();
        if (cards != null) cards.HideCardPanel();
        if (playerUICanvas == null) return;
        CardUIManager canvasCards = playerUICanvas.GetComponent<CardUIManager>();
        if (canvasCards != null) canvasCards.HideCardPanel();
        playerUICanvas.gameObject.SetActive(false);
    }
}
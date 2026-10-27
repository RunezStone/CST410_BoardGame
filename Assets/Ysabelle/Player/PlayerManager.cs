using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] bool isTurn = false;
    [SerializeField] bool hasMoved = false;      // NEW: one move per turn
    [SerializeField] bool turnEnded = false;     // NEW: set only by End Turn button
    [SerializeField] bool isInTargetingMode = false;

    [Header("CurrentSpace")]
    public Space currSpace;

    [Header("User Interface")]
    public Canvas playerUICanvas;

    [Header("Player Reference Scripts")]
    public PlayerData playerData;
    [SerializeField] CardInventory inventory;
    [SerializeField] UIManager uiManager;

    private List<Space> validMovementSpaces = new List<Space>();
    private List<Space> validTargetSpaces = new List<Space>();
    private System.Action<Space, PlayerManager> targetingCallback;

    void Update()
    {
        if (isInTargetingMode && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Space spaceScript = hit.collider.GetComponent<Space>();

                if (spaceScript != null && validTargetSpaces.Contains(spaceScript))
                {
                    
                    targetingCallback?.Invoke(spaceScript, this);
                    ExitTargetingMode();
                }
            }
        }
        else if (isTurn && !hasMoved && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Space spaceScript = hit.collider.GetComponent<Space>();

                if (spaceScript != null && validMovementSpaces.Contains(spaceScript))
                {
                    PlaceCharacter(spaceScript);
                }
            }
        }
    }

    public void PlaceCharacter(Space space)
    {
        if (currSpace != null)
            currSpace.RemoveObject();

        currSpace = space;
        currSpace.PlaceObject(gameObject);

        hasMoved = true;          // was: isTurn = false;
        ClearHighlighting();      // remove the green tiles once they've moved
    }

    public void EndTurn()
    {
        if (!isTurn) return;      // ignore clicks when it's not this player's turn
        if (isInTargetingMode) ExitTargetingMode();
        turnEnded = true;
    }

    public void EnterTargetingMode(List<Space> targetSpaces, System.Action<Space, PlayerManager> callback, PlayerManager playerManager)
    {
        isInTargetingMode = true;
        validTargetSpaces = targetSpaces;
        targetingCallback = callback;

        HighlightValidSpaces(targetSpaces, Color.red); // Red for targeting instead of green
    }

    public void ExitTargetingMode()
    {
        isInTargetingMode = false;
        ClearHighlighting();
    }

    public IEnumerator PlayerTurn()
    {
        isTurn = true;
        hasMoved = false;
        turnEnded = false;

        DisplayPlayerUI();

        validMovementSpaces = TileManager.instance.GetValidMovementSpaces(currSpace, playerData.movement);
        HighlightValidSpaces(validMovementSpaces, Color.green);

        // Player can move and play cards freely until they press End Turn
        yield return new WaitUntil(() => turnEnded);

        isTurn = false;
        ClearHighlighting();
        HidePlayerUI();
    }

    void HighlightValidSpaces(List<Space> spaces, Color color)
    {
        foreach (Space space in spaces)
        {
            space.GetComponent<Renderer>().material.color = color;
        }
    }

    void ClearHighlighting()
    {
        foreach (Space space in validMovementSpaces)
        {
            space.GetComponent<Renderer>().material.color = Color.white;
        }
        foreach (Space space in validTargetSpaces)
        {
            space.GetComponent<Renderer>().material.color = Color.white;
        }
        validMovementSpaces.Clear();
        validTargetSpaces.Clear();
    }

    public void DisplayPlayerUI()
    {
        playerUICanvas.gameObject.SetActive(true);
    }

    public void HidePlayerUI()
    {
        playerUICanvas.gameObject.SetActive(false);
    }
}
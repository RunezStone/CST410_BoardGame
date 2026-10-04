using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] bool isTurn = false;
    [SerializeField] bool isInTargetingMode = false;

    [Header("CurrentSpace")]
    public Space currSpace;

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
        else if (isTurn && Input.GetMouseButtonDown(0))
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
        // Remove from old space
        if (currSpace != null)
        {
            currSpace.RemoveObject();
        }

        // Place on new space
        currSpace = space;
        currSpace.PlaceObject(gameObject);

        isTurn = false;
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

       
        validMovementSpaces = TileManager.instance.GetValidMovementSpaces(currSpace, playerData.movement);

        
        HighlightValidSpaces(validMovementSpaces, Color.green);

        yield return new WaitUntil(() => !isTurn); 

        ClearHighlighting();
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
}
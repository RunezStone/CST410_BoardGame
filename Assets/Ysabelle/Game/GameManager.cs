using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public TileManager tileManager;
    public GameUIManager gameUIManager;

    [Header("Players")]
    public List<PlayerManager> players = new List<PlayerManager>();

    [Header("Startup")]
    [Tooltip("Extra pause after everything has loaded, before the first turn starts")]
    [SerializeField] float startDelay = 0.5f;

    private PlayerManager currentPlayer;
    private bool gameReady = false;

    public bool GameReady => gameReady;

    // Start can be a coroutine: Unity runs it over several frames.
    IEnumerator Start()
    {
        // Hide every player's UI so nothing is clickable while loading
        foreach (var player in players) player.HidePlayerUI();

        // 1. Build the board and place the pieces
        tileManager.InitializeBoard();
        players[0].PlaceCharacter(tileManager.spaces[0, 0].GetComponent<Space>());
        players[1].PlaceCharacter(tileManager.spaces[5, 5].GetComponent<Space>());
        players[2].PlaceCharacter(tileManager.spaces[1, 0].GetComponent<Space>());
        players[3].PlaceCharacter(tileManager.spaces[3, 0].GetComponent<Space>());

        // 2. Tell physics about the new tiles right away, so mouse raycasts can hit them
        Physics.SyncTransforms();

        // 3. Let every other script finish its own Start(), then one physics step
        yield return null;
        yield return new WaitForFixedUpdate();

        // 4. Short breather so the first click isn't swallowed by the scene loading
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        gameReady = true;
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while (!SomeoneDead())
        {
            foreach (var player in players)
            {
                currentPlayer = player;
                gameUIManager.ChangePlayerTurnDisplay(player.gameObject.name);

                // Waits here until this player presses End Turn (PlayerTurn shows/hides its own UI)
                yield return StartCoroutine(player.PlayerTurn());

                if (SomeoneDead()) break;
            }
        }

        currentPlayer = null;
        Debug.Log("Game over");   // replace with your game-over screen
    }

    // Hook a shared End Turn button's OnClick to this
    public void OnEndTurnPressed()
    {
        if (currentPlayer != null)
            currentPlayer.EndTurn();
    }

    bool SomeoneDead()
    {
        foreach (var player in players)
        {
            if (player.playerData.hitPoints <= 0) return true;
        }
        return false;
    }
}
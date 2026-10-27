using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public TileManager tileManager;
    public GameUIManager gameUIManager;

    [Header("Players")]
    public List<PlayerManager> players = new List<PlayerManager>();

    private PlayerManager currentPlayer;

    void Start()
    {
        TileManager.instance.InitializeBoard();
        players[0].PlaceCharacter(tileManager.spaces[0, 0].GetComponent<Space>());
        players[1].PlaceCharacter(tileManager.spaces[5, 5].GetComponent<Space>());
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while (!SomeoneDead())
        {
            foreach (var player in players)
            {
                currentPlayer = player;

                player.DisplayPlayerUI();
                gameUIManager.ChangePlayerTurnDisplay(player.gameObject.name);

                // Waits here until this player presses End Turn
                yield return StartCoroutine(player.PlayerTurn());

                player.HidePlayerUI();

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
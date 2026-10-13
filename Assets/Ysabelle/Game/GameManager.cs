using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public TileManager tileManager;
    public GameUIManager gameUIManager;

    [Header("Players")]
    public List<PlayerManager> players = new List<PlayerManager>();

    void Start()
    {
        TileManager.instance.InitializeBoard();
        players[0].PlaceCharacter(tileManager.spaces[0, 0].GetComponent<Space>());
        players[1].PlaceCharacter(tileManager.spaces[5, 5].GetComponent<Space>());
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while(!SomeoneDead())
        {
            foreach (var player in players)
            { 
                SwitchPlayerUI(player);
                Type type = player.GetType();
                gameUIManager.ChangePlayerTurnDisplay(type.Name);

                yield return StartCoroutine(player.PlayerTurn());
                player.HidePlayerUI();
            }
        }
        yield return null;
    }

    private void SwitchPlayerUI(PlayerManager player)
    {
        player.DisplayPlayerUI();
    }

    bool SomeoneDead()
    {
        foreach (var player in players)
        {
            if (player.playerData.hitPoints <= 0) { return true; }
        }
        return false;
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public TileManager tileManager;

    [Header("Players")]
    public List<PlayerManager> players = new List<PlayerManager>();

    void Start()
    {
        TileManager.instance.InitializeBoard();
        players[0].PlaceCharacter(tileManager.spaces[0, 0].GetComponent<Space>());
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while(true)
        {
            foreach (var player in players)
            {
                yield return StartCoroutine(player.PlayerTurn());
            }
        }
        //yield return null;
    }
}
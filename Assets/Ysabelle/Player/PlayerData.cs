using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [Header("Player Base Data")]
    public int defense = 10;
    public int hitPoints = 20;
    public int actionPoints = 1;
    public int attackBonus = 2;
    public int movement = 5;

    [Header("Player Base Checks")]
    public int brain = 0;
    public int constitution = 0;
    public int athletics = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DiplayCurrPlayerDataUI()
    {

    }
}

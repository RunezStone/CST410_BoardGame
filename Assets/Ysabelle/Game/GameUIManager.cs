using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerTurnDisplay;


    public void ChangePlayerTurnDisplay(string name)
    {
        Debug.Log("Current Manager: " + name);
        if (name == "WarlockManager") { playerTurnDisplay.text = "Warlock's Turn"; }
        if (name == "FighterManager") { playerTurnDisplay.text = "Fighter's Turn"; }

    }

}


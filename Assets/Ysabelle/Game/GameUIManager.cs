using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerTurnDisplay;


    public void ChangePlayerTurnDisplay(string name)
    {
        Debug.Log("Current Manager: " + name);
        if (name == "Player_Warlock") { playerTurnDisplay.text = "Warlock's Turn"; }
        else if (name == "Player_Fighter") { playerTurnDisplay.text = "Fighter's Turn"; }
        else if (name == "Player_Wizard") { playerTurnDisplay.text = "Wizard's Turn"; }
        else if (name == "Player_Monk") { playerTurnDisplay.text = "Monk's Turn"; }
    }

}


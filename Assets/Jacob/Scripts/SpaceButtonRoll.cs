using UnityEngine;
using UnityEngine.InputSystem;

public class SpaceButtonRoll : MonoBehaviour
{
    [SerializeField] public DiceScript diceScript;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            diceScript.DiceRoll();
        }
    }
}

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DiceScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] public GameObject[] baseDice;
    [SerializeField] public bool twoDice;
    [SerializeField] public int diceType;
    /*
    NOTE REGARDING DICE TYPE:
    Until I can make the other dice, set Dice Type = 0 on Unity.
    FUTURE PLAN:
    d4 = 0
    d6 = 1
    d8 = 2
    d12 = 3
    d20 = 4
    */
    private GameObject dice1;
    private GameObject dice2;
    public int value1;
    public int value2;
    public int state = 0; // 0 = Dormant, 1 = Rolling, 2 = Launch
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case 0:
                // Dormant, waiting for dice to be launcher (press space)
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    state = 1;
                    if (twoDice) dice1 = Object.Instantiate(baseDice[diceType], new Vector3(this.gameObject.transform.position.x - 1, this.gameObject.transform.position.y, this.gameObject.transform.position.z), new Quaternion(0,0,0,0));
                    else dice1 = Object.Instantiate(baseDice[diceType], new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y, this.gameObject.transform.position.z), new Quaternion(0,0,0,0));
                    dice1.transform.Rotate(new Vector3(225, 0, 225));
                    dice1.GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-50, 50), Random.Range(-20, 0), Random.Range(-50, 50)), ForceMode.Impulse);
                    dice1.GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)), ForceMode.Impulse);
                    if (twoDice)
                    {
                        dice2 = Object.Instantiate(baseDice[diceType], new Vector3(this.gameObject.transform.position.x + 1, this.gameObject.transform.position.y, this.gameObject.transform.position.z), new Quaternion(0,0,0,0));
                        dice2.transform.Rotate(new Vector3(225, 0, 225));
                        dice2.GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-50, 50), Random.Range(5, 20), Random.Range(-50, 50)), ForceMode.Impulse);
                        dice2.GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)), ForceMode.Impulse);
                    }
                }
                break;
            case 1:
                // Launched, waiting for dice to stop moving, then get results and destroy dice
                if (dice1.GetComponent<Rigidbody>().IsSleeping() && (!twoDice || dice2.GetComponent<Rigidbody>().IsSleeping()))
                {
                    value1 = dice1.GetComponent<NumberLink>().GetValue();
                    if (twoDice) value2 = dice2.GetComponent<NumberLink>().GetValue();
                    else value2 = 0;
                    Object.Destroy(dice1);
                    if (twoDice) Object.Destroy(dice2);
                    // REPLACE LOG WITH CODE TO SEND VALUES.
                    Debug.Log("Dice 1: " + value1 + ", Dice 2: " + value2);
                    state = 0;
                }
                break;
        }
    }
}

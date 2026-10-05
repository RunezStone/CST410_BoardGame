using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DiceScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] public GameObject baseDice;
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
                // Dormant, waiting for dice to be summoned (testing input: space)
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    state = 1;
                    dice1 = Object.Instantiate(baseDice, new Vector3(this.gameObject.transform.position.x - 1, this.gameObject.transform.position.y, this.gameObject.transform.position.z), new Quaternion(0,0,0,0));
                    dice1.GetComponent<Rigidbody>().useGravity = false;
                    dice1.transform.Rotate(new Vector3(45, 0, 45));
                    dice2 = Object.Instantiate(baseDice, new Vector3(this.gameObject.transform.position.x + 1, this.gameObject.transform.position.y, this.gameObject.transform.position.z), new Quaternion(0,0,0,0));
                    dice2.GetComponent<Rigidbody>().useGravity = false;
                    dice2.transform.Rotate(new Vector3(225, 0, 225));
                }
                break;
            case 1:
                // Rolling, waiting for dice to be launched (press enter for now)
                dice1.transform.Rotate(new Vector3(360 * Time.deltaTime, 360 * Time.deltaTime, 360 * Time.deltaTime));
                dice2.transform.Rotate(new Vector3(360 * Time.deltaTime, 360 * Time.deltaTime, 360 * Time.deltaTime));
                if (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    state = 2;
                    dice1.GetComponent<Rigidbody>().useGravity = true;
                    dice1.GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-50, 50), Random.Range(5, 20), Random.Range(-50, 50)), ForceMode.Impulse);
                    dice1.GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)), ForceMode.Impulse);
                    dice2.GetComponent<Rigidbody>().useGravity = true;
                    dice2.GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-50, 50), Random.Range(5, 20), Random.Range(-50, 50)), ForceMode.Impulse);
                    dice2.GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)), ForceMode.Impulse);
                }
                break;
            case 2:
                // Launched, waiting for dice to stop moving, then get results and destroy dice
                if (dice1.GetComponent<Rigidbody>().IsSleeping() && dice2.GetComponent<Rigidbody>().IsSleeping())
                {
                    value1 = dice1.GetComponent<NumberLink>().GetValue();
                    value2 = dice2.GetComponent<NumberLink>().GetValue();
                    Object.Destroy(dice1);
                    Object.Destroy(dice2);
                    // REPLACE LOG WITH CODE TO SEND VALUES.
                    Debug.Log("Dice 1: " + value1 + ", Dice 2: " + value2);
                    state = 0;
                }
                break;
        }
    }
}

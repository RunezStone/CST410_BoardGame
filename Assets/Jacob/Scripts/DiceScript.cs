using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceScript : MonoBehaviour
{
    [SerializeField] public GameObject[] baseDice;
    [SerializeField] public bool twoDice;
    [SerializeField] public int diceType;
    /*
    NOTE REGARDING DICE TYPE:
    d4 = 0
    d6 = 1
    d8 = 2
    d12 = 3
    d20 = 4
    */

    [Tooltip("How long the dice stay on the board after they stop, before the result is used")]
    [SerializeField] private float holdSeconds = 2f;

    [Tooltip("Gap between dice when several are thrown at once")]
    [SerializeField] private float spawnSpacing = 1.5f;

    [Tooltip("If a die is still moving after this long (e.g. balanced on an edge), read it anyway")]
    [SerializeField] private float maxRollSeconds = 8f;

    private readonly List<GameObject> activeDice = new List<GameObject>();

    // Results of the last roll. value1/value2 kept for anything that already reads them.
    public int value1;
    public int value2;
    public int[] values = new int[0];

    public int state = 0; // 0 = Dormant, 1 = Rolling, 2 = Showing result

    private float rollStartTime;
    private System.Action<int[]> onRolled;

    public bool IsBusy => state != 0;

    void Update()
    {
        if (state != 1) return;

        // Rolling: wait until EVERY die has stopped (or we've waited too long)
        bool timedOut = Time.time - rollStartTime > maxRollSeconds;
        if (!timedOut)
        {
            foreach (GameObject die in activeDice)
                if (!die.GetComponent<Rigidbody>().IsSleeping()) return;
        }

        values = new int[activeDice.Count];
        for (int i = 0; i < activeDice.Count; i++)
            values[i] = activeDice[i].GetComponent<NumberLink>().GetValue();

        value1 = values.Length > 0 ? values[0] : 0;
        value2 = values.Length > 1 ? values[1] : 0;
        Debug.Log("Dice: " + string.Join(", ", values));

        state = 2;
        StartCoroutine(ShowResultThenFinish());
    }

    // Leave the dice where they landed so players can read them, then clean up and report.
    private IEnumerator ShowResultThenFinish()
    {
        yield return new WaitForSeconds(holdSeconds);

        foreach (GameObject die in activeDice)
            if (die != null) Destroy(die);
        activeDice.Clear();

        state = 0;   // free BEFORE the callback, so the callback can roll the next dice

        var callback = onRolled;
        onRolled = null;
        callback?.Invoke(values);
    }

    // Original entry point (Space button): rolls diceType, one or two dice depending on twoDice.
    public void DiceRoll(System.Action<int, int> callback = null)
    {
        Throw(diceType, twoDice ? 2 : 1,
              callback == null ? null : (System.Action<int[]>)(v => callback(v[0], v.Length > 1 ? v[1] : 0)));
    }

    // One die of a given type. Returns false if it can't roll (busy or no prefab).
    public bool RollType(int type, System.Action<int, int> callback)
    {
        return RollMany(type, 1, callback == null ? null : (System.Action<int[]>)(v => callback(v[0], 0)));
    }

    // Several dice of the same type thrown together (e.g. 4d4). Callback gets every face.
    // Returns false if it can't roll (busy or no prefab), so the caller can roll virtually.
    public bool RollMany(int type, int count, System.Action<int[]> callback)
    {
        if (state != 0 || count < 1 || type < 0 || type >= baseDice.Length || baseDice[type] == null)
            return false;

        Throw(type, count, callback);
        return true;
    }

    private void Throw(int type, int count, System.Action<int[]> callback)
    {
        if (state != 0) return;
        if (type < 0 || type >= baseDice.Length || baseDice[type] == null)
        {
            Debug.LogError($"DiceScript: no prefab for dice type {type}.");
            return;
        }

        onRolled = callback;   // remember who asked
        state = 1;
        rollStartTime = Time.time;

        Vector3 pos = transform.position;
        for (int i = 0; i < count; i++)
        {
            // Spread the dice out in a row, centered on this object, so they don't spawn inside each other
            float offset = (i - (count - 1) / 2f) * spawnSpacing;

            GameObject die = Instantiate(baseDice[type], pos + Vector3.right * offset, Quaternion.identity);
            die.transform.Rotate(new Vector3(225, 0, 225));

            Rigidbody rb = die.GetComponent<Rigidbody>();
            rb.AddForce(new Vector3(Random.Range(-50, 50), Random.Range(-20, 0), Random.Range(-50, 50)), ForceMode.Impulse);
            rb.AddTorque(new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)), ForceMode.Impulse);

            activeDice.Add(die);
        }
    }
}
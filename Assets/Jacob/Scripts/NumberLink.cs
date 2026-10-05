using UnityEngine;

public class NumberLink : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] public GameObject[] diceFaces;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetValue()
    {
        float highestY = float.MinValue;
        int i;
        int value = 0;
        for (i = 0; i < 6; i++)
        {
            if (diceFaces[i].transform.position.y > highestY || highestY == float.MinValue)
            {
                highestY = diceFaces[i].transform.position.y;
                value = i + 1;
            }
        }
        return value;
    }
}

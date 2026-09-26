using UnityEngine;

public class TileManager : MonoBehaviour
{
    [SerializeField] public GameObject baseSpace;
    public GameObject[,] spaces;
    void Start()
    {
        spaces = new GameObject[10, 15];
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                spaces[i,j] = Object.Instantiate(baseSpace);
                spaces[i,j].GetComponent<Space>().id[0] = i;
                spaces[i,j].GetComponent<Space>().id[1] = j;
                spaces[i,j].transform.position = new UnityEngine.Vector3(i - (float)4.5, 0, j - 7);
            }
        }
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                if (j + 1 < 15)
                {
                    spaces[i, j].GetComponent<Space>().neighboringIds[0, 0] = spaces[i, j + 1].GetComponent<Space>().id[0];
                    spaces[i, j].GetComponent<Space>().neighboringIds[0, 1] = spaces[i, j + 1].GetComponent<Space>().id[1];
                }
                if (i - 1 >= 0)
                {
                    spaces[i, j].GetComponent<Space>().neighboringIds[1, 0] = spaces[i - 1, j].GetComponent<Space>().id[0];
                    spaces[i, j].GetComponent<Space>().neighboringIds[1, 1] = spaces[i - 1, j].GetComponent<Space>().id[1];
                }
                if (i + 1 < 10)
                {
                    spaces[i, j].GetComponent<Space>().neighboringIds[2, 0] = spaces[i + 1, j].GetComponent<Space>().id[0];
                    spaces[i, j].GetComponent<Space>().neighboringIds[2, 1] = spaces[i + 1, j].GetComponent<Space>().id[1];
                }
                if (j - 1 >= 0)
                {
                    spaces[i, j].GetComponent<Space>().neighboringIds[3, 0] = spaces[i, j - 1].GetComponent<Space>().id[0];
                    spaces[i, j].GetComponent<Space>().neighboringIds[3, 1] = spaces[i, j - 1].GetComponent<Space>().id[1];
                }
                /* 
                 * Remove comment brackets only to verify neighboring IDs work.
                Debug.Log(spaces[i, j].GetComponent<Space>().id[0] + ", " + spaces[i, j].GetComponent<Space>().id[1] + " - FRONT : " + spaces[i, j].GetComponent<Space>().neighboringIds[0, 0] + ", " + spaces[i, j].GetComponent<Space>().neighboringIds[0, 1]);
                Debug.Log(spaces[i, j].GetComponent<Space>().id[0] + ", " + spaces[i, j].GetComponent<Space>().id[1] + " - LEFT : " + spaces[i, j].GetComponent<Space>().neighboringIds[1, 0] + ", " + spaces[i, j].GetComponent<Space>().neighboringIds[1, 1]);
                Debug.Log(spaces[i, j].GetComponent<Space>().id[0] + ", " + spaces[i, j].GetComponent<Space>().id[1] + " - RIGHT : " + spaces[i, j].GetComponent<Space>().neighboringIds[2, 0] + ", " + spaces[i, j].GetComponent<Space>().neighboringIds[2, 1]);
                Debug.Log(spaces[i, j].GetComponent<Space>().id[0] + ", " + spaces[i, j].GetComponent<Space>().id[1] + " - BACK : " + spaces[i, j].GetComponent<Space>().neighboringIds[3, 0] + ", " + spaces[i, j].GetComponent<Space>().neighboringIds[3, 1]);
                */
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileManager instance;

    [SerializeField] public GameObject baseSpace;
    public GameObject[,] spaces;

    [SerializeField] float spacing = 1.025f;

    private void Awake()
    {
        instance = this;
    }
    public void InitializeBoard()
    {
        spaces = new GameObject[10, 15];
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                spaces[i, j] = Object.Instantiate(baseSpace);
                spaces[i, j].GetComponent<Space>().id[0] = i;
                spaces[i, j].GetComponent<Space>().id[1] = j;
                spaces[i, j].transform.position = new UnityEngine.Vector3((i - (float)4.5) * spacing, 0, (j - 7) * spacing);

                // Initialize all neighbors to invalid (-1)
                Space spaceComponent = spaces[i, j].GetComponent<Space>();
                for (int k = 0; k < 4; k++)
                {
                    spaceComponent.neighboringIds[k, 0] = -1;
                    spaceComponent.neighboringIds[k, 1] = -1;
                }
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
            }
        }
    }

    // Get all valid spaces the player can move to within their movement limit
    public List<Space> GetValidMovementSpaces(Space currentSpace, int movementPoints)
    {
        List<Space> validSpaces = new List<Space>();
        Queue<(Space space, int costUsed)> queue = new Queue<(Space, int)>();
        HashSet<Space> visited = new HashSet<Space>();

        queue.Enqueue((currentSpace, 0));
        visited.Add(currentSpace);

        while (queue.Count > 0)
        {
            var (space, costUsed) = queue.Dequeue();

            // Check all 4 adjacent neighbors
            for (int i = 0; i < 4; i++)
            {
                int neighborX = space.neighboringIds[i, 0];
                int neighborY = space.neighboringIds[i, 1];

                // Check for invalid neighbors)
                if (neighborX == -1 || neighborY == -1)
                    continue;

                Space neighborSpace = spaces[neighborX, neighborY].GetComponent<Space>();

                // Skip if already visited
                if (visited.Contains(neighborSpace))
                    continue;

                // Skip if blocked or occupied
                if (neighborSpace.IsBlocked() || neighborSpace.CharacterExistsInSpace())
                    continue;

                int newCost = costUsed + 1;

                // Add to valid spaces if within movement budget
                if (newCost <= movementPoints)
                {
                    validSpaces.Add(neighborSpace);
                    visited.Add(neighborSpace);
                    queue.Enqueue((neighborSpace, newCost));
                }
            }
        }

        return validSpaces;
    }



}

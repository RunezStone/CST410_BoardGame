using UnityEngine;
[System.Serializable]
public class Space : MonoBehaviour
{
    public int[] id = new int[2]; // In Unity, Element 0 is X, Element 1 is Y
    public string displayName;
    public string spaceKind;
    public int[,] neighboringIds = new int[4,2]; // Front/Up (Y + 1), Left (X - 1), Right(X + 1), Back/Down (Y - 1). Each column is a set of 2 numbers for X and Y. Row 0 = X. Row 1 = Y.

    private void Start()
    {
        Debug.Log(neighboringIds);
    }
}

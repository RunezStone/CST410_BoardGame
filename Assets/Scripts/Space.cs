using UnityEngine;
[System.Serializable]
public class Space : MonoBehaviour
{
    public Transform objectPlacement;

    public int[] id = new int[2]; // In Unity, Element 0 is X, Element 1 is Y
    public string displayName;
    public string spaceKind;
    public int[,] neighboringIds = new int[4, 2]; // Front/Up (Y + 1), Left (X - 1), Right(X + 1), Back/Down (Y - 1)

    private bool isBlocked = false;
    private GameObject currentCharacter = null;

    private void Start()
    {

    }

    public bool CharacterExistsInSpace()
    {
        return currentCharacter != null;
    }

    public void PlaceObject(GameObject character)
    {
        character.transform.position = objectPlacement.position;
        currentCharacter = character;
    }

    public void RemoveObject()
    {
        currentCharacter = null;
    }

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
    }

    public bool IsBlocked()
    {
        return isBlocked;
    }
}
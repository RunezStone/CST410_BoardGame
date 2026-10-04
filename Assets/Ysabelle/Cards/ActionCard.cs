using UnityEngine;

public abstract class ActionCard : MonoBehaviour
{
    public string cardName;
    public string cardDescription;
    public int points;

    public abstract void InitiateAction(PlayerManager playerManager);
    
}

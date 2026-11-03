using UnityEngine;

// Put on a character by Stunning Strike. At the start of their next turn they lose
// that turn, then the stun is gone. Every character takes one turn between the monk's
// turns, so this lasts exactly "until the monk's next turn".
public class StunEffect : MonoBehaviour, ITurnEffect
{
    public void OnTurnStart()
    {
        GetComponent<PlayerManager>().SkipThisTurn();
        Destroy(this);
    }

    public void OnTurnEnd() { }
}


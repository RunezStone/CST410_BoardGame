using UnityEngine;

// Anything that lasts a number of turns (Rage, Haste, Defensive Stance, Stun, Judgement...)
// implements this. PlayerManager calls these at the start and end of the owner's turn.
public interface ITurnEffect
{
    void OnTurnStart();
    void OnTurnEnd();
}

// Added to the fighter at runtime by RageCard; removes itself when it runs out.
public class RageEffect : MonoBehaviour, ITurnEffect
{
    PlayerManager player;
    int damageBonus, movementBonus, turnsLeft;

    public void Begin(PlayerManager p, int damage, int movement, int turns)
    {
        player = p;
        damageBonus = damage;
        movementBonus = movement;
        turnsLeft = turns;            // the turn Rage is played counts as turn 1

        player.playerData.damageBonus += damageBonus;
        player.playerData.movement += movementBonus;
        player.RefreshMovementSpaces();   // +1 square right away if they haven't moved yet

        Debug.Log($"{player.name} is raging for {turnsLeft} turns!");
    }

    public void OnTurnStart() { }

    public void OnTurnEnd()
    {
        turnsLeft--;
        if (turnsLeft <= 0) End();
    }

    void End()
    {
        player.playerData.damageBonus -= damageBonus;
        player.playerData.movement -= movementBonus;
        Debug.Log($"{player.name}'s rage fades.");
        Destroy(this);
    }
}
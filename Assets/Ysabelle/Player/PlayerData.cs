using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [Header("Player Base Data")]
    public int defense = 10;
    public int hitPoints = 20;
    [Tooltip("Leave at 0 to use the starting Hit Points as the max")]
    public int maxHitPoints = 0;

    [Tooltip("This character's card resource: action points (Fighter), focus (Monk), spell points (Wizard, Warlock), holy points (Cleric)")]
    public int actionPoints = 1;
    [Tooltip("Leave at 0 to use the starting points as the max")]
    public int maxActionPoints = 0;

    public int attackBonus = 2;
    public int magicBonus = 0;
    public int movement = 5;

    [Header("Temporary Modifiers")]
    [Tooltip("Extra damage added to every hit (Rage, Surge, ...). Effects add and remove this; don't set it by hand.")]
    public int damageBonus = 0;

    [Header("Player Base Checks")]
    public int brain = 0;
    public int constitution = 0;
    public int athletics = 0;

    public bool IsDead => hitPoints <= 0;

    void Awake()
    {
        if (maxHitPoints <= 0) maxHitPoints = hitPoints;
        if (maxActionPoints <= 0) maxActionPoints = actionPoints;
    }

    public void TakeDamage(int amount)
    {
        hitPoints = Mathf.Max(0, hitPoints - amount);
        if (IsDead) Debug.Log($"{name} has been defeated!");
    }

    // Heals up to max HP. Returns how much was actually healed.
    public int Heal(int amount)
    {
        if (IsDead) return 0;   // remove this line if healing should revive a downed character
        int before = hitPoints;
        hitPoints = Mathf.Min(maxHitPoints, hitPoints + amount);
        return hitPoints - before;
    }

    // Regains card points up to the max (Short Rest, or a per-turn refill). Returns how many were gained.
    public int RestorePoints(int amount)
    {
        int before = actionPoints;
        actionPoints = Mathf.Min(maxActionPoints, actionPoints + amount);
        return actionPoints - before;
    }

    void DiplayCurrPlayerDataUI()
    {

    }
}
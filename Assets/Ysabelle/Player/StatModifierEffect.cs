using UnityEngine;

// A reusable timed buff/debuff: +damage, +movement and/or +defense for a number of the
// owner's turns, removed automatically when it runs out.
// Used by Surge and Defensive Stance. Also fits Haste (+2 movement, 3 turns):
//     StatModifierEffect.Apply(target, "Haste", 3, movement: 2);
// (ITurnEffect lives in RageEffect.cs.)
public class StatModifierEffect : MonoBehaviour, ITurnEffect
{
    public string effectName;
    [SerializeField] int turnsLeft;
    [SerializeField] bool countsDownAtTurnStart;

    PlayerManager player;
    int damage, movement, defense;

    // Adds the effect, or refreshes its duration if the target already has one with this name.
    //
    // Turn counting -- pick whichever fits what the buff is FOR:
    //  countsDownAtTurnStart = false (default): ticks at the END of the owner's turn.
    //      Good for things you use on your own turn (damage, movement).
    //      If applied during the owner's turn, that turn counts as turn 1 (same as Rage).
    //  countsDownAtTurnStart = true: ticks at the START of the owner's turn.
    //      Good for defense, which matters during everyone ELSE's turns:
    //      3 = protected through 3 full rounds of other players' turns.
    public static StatModifierEffect Apply(PlayerManager target, string name, int turns,
                                           int damage = 0, int movement = 0, int defense = 0,
                                           bool countsDownAtTurnStart = false)
    {
        foreach (StatModifierEffect existing in target.GetComponents<StatModifierEffect>())
        {
            if (existing.effectName == name)
            {
                existing.turnsLeft = Mathf.Max(existing.turnsLeft, turns);
                Debug.Log($"{target.name}'s {name} refreshed ({existing.turnsLeft} turn(s)).");
                return existing;
            }
        }

        StatModifierEffect effect = target.gameObject.AddComponent<StatModifierEffect>();
        effect.countsDownAtTurnStart = countsDownAtTurnStart;
        effect.Begin(target, name, turns, damage, movement, defense);
        return effect;
    }

    public static bool Has(PlayerManager target, string name)
    {
        foreach (StatModifierEffect e in target.GetComponents<StatModifierEffect>())
            if (e.effectName == name) return true;
        return false;
    }

    void Begin(PlayerManager p, string name, int turns, int dmg, int move, int def)
    {
        player = p;
        effectName = name;
        turnsLeft = turns;
        damage = dmg;
        movement = move;
        defense = def;

        player.playerData.damageBonus += damage;
        player.playerData.movement += movement;
        player.playerData.defense += defense;
        if (movement != 0) player.RefreshMovementSpaces();   // only does anything during their turn

        Debug.Log($"{player.name} gains {effectName} for {turnsLeft} turn(s).");
    }

    public void OnTurnStart()
    {
        if (countsDownAtTurnStart) Tick();
    }

    public void OnTurnEnd()
    {
        if (!countsDownAtTurnStart) Tick();
    }

    void Tick()
    {
        turnsLeft--;
        if (turnsLeft <= 0) End();
    }

    void End()
    {
        player.playerData.damageBonus -= damage;
        player.playerData.movement -= movement;
        player.playerData.defense -= defense;
        if (movement != 0) player.RefreshMovementSpaces();
        Debug.Log($"{player.name}'s {effectName} wears off.");
        Destroy(this);
    }
}
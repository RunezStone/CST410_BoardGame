using UnityEngine;

// Put on a character by Judgement. While it's on, PlayerManager refuses their normal move,
// their card teleports (Tactical Shift, Misty Step, Shadow Veil) and Swap. Pushes still move them.
// Lasts `turnsLeft` of the target's own turns: each of their turns counts down at the start,
// and the effect comes off at the end of the last frozen turn.
public class FrozenEffect : MonoBehaviour, ITurnEffect
{
    [SerializeField] int turnsLeft;

    // Adds Frozen, or refreshes it (no stacking) if the target is already frozen.
    public static FrozenEffect Apply(PlayerManager target, int turns)
    {
        FrozenEffect effect = target.GetComponent<FrozenEffect>();
        if (effect == null) effect = target.gameObject.AddComponent<FrozenEffect>();
        effect.turnsLeft = Mathf.Max(effect.turnsLeft, turns);
        Debug.Log($"{target.name} is frozen for {effect.turnsLeft} of their turn(s).");
        return effect;
    }

    public void OnTurnStart()
    {
        turnsLeft--;   // this turn is one of the frozen ones
    }

    public void OnTurnEnd()
    {
        if (turnsLeft > 0) return;
        Debug.Log($"{name} thaws out.");
        Destroy(this);
    }
}

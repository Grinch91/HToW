/// <summary>
/// Remembers which of the player's cards is attacking and which enemy card it is
/// aimed at.
///
/// This is the fix for bug C1. Selection state previously lived on a
/// <c>BattleController</c> attached to each card GameObject, but was read via
/// <c>playerActive.GetComponent&lt;BattleController&gt;()</c> — on the *container*,
/// which had no such component. That returned null and threw on the first attack, a
/// crash visible in the recorded 2014 log. There was also no way to clear a selection
/// once made.
///
/// Selection is a property of the battle, not of a card, so it lives in one place.
/// Plain C# so it can be tested without Unity.
/// </summary>
public class BattleSelection
{
    /// <summary>The player card chosen to attack, or null.</summary>
    public CardInstance Attacker { get; private set; }

    /// <summary>The enemy card chosen as the target, or null.</summary>
    public CardInstance Target { get; private set; }

    /// <summary>True once both halves of an attack have been chosen.</summary>
    public bool IsComplete => Attacker != null && Target != null;

    /// <summary>
    /// Records a click. Ownership decides the role: your own card becomes the attacker,
    /// the opponent's becomes the target. Clicking the same card again deselects it.
    /// </summary>
    public void Select(CardInstance card)
    {
        if (card == null)
        {
            return;
        }

        if (card.Owner == Side.Player)
        {
            Attacker = ReferenceEquals(Attacker, card) ? null : card;
        }
        else
        {
            Target = ReferenceEquals(Target, card) ? null : card;
        }
    }

    /// <summary>Forgets both halves. Called after an attack resolves and at turn end.</summary>
    public void Clear()
    {
        Attacker = null;
        Target = null;
    }

    /// <summary>
    /// Drops any selection referring to a card that has left play, so a destroyed card
    /// can never remain selected.
    /// </summary>
    public void Forget(CardInstance card)
    {
        if (ReferenceEquals(Attacker, card))
        {
            Attacker = null;
        }

        if (ReferenceEquals(Target, card))
        {
            Target = null;
        }
    }
}

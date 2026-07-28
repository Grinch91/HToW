using System;

/// <summary>
/// One physical card in a match: a reference to its shared <see cref="CardData"/>
/// definition, plus the state that belongs to this copy alone.
///
/// This type exists to fix a family of bugs that all share one root cause — the old
/// <c>CardDef</c> was both definition and state, and cards were identified by name:
///
///  * Playing a card played a different one, because zones were treated as piles and
///    the card moved was whatever sat at index 0 (bug C2).
///  * Destroying a card removed the first card with a matching name, so with two
///    Raiders in play the wrong one died and the survivor inherited the dead one's
///    damage (bug M5).
///  * <c>FindGameObjectWithTag(name)</c> could destroy the attacker's own card (M4).
///
/// Identity is now reference identity. Two Raiders are two distinct
/// <see cref="CardInstance"/> objects, and moving one between zones moves that one.
/// See Docs/CardSystem.md section 8.
/// </summary>
public class CardInstance
{
    /// <summary>The shared, immutable definition. Never mutated.</summary>
    public CardData Data { get; private set; }

    /// <summary>Which side owns this card.</summary>
    public Side Owner { get; private set; }

    /// <summary>Current health. Starts at <see cref="CardData.Hp"/> and falls as it takes damage.</summary>
    public int CurrentHp { get; private set; }

    /// <summary>True once this card has attacked in the current turn. Cleared at turn start.</summary>
    public bool HasAttacked { get; set; }

    /// <summary>
    /// The GameObject currently showing this card, or null if it is not on screen
    /// (cards in a draw pile have no view). A convenience link so zones can lay out
    /// their cards; the model never reads anything back from it.
    /// </summary>
    public CardView View { get; set; }

    /// <summary>True while this card still has health remaining.</summary>
    public bool IsAlive => CurrentHp > 0;

    public CardInstance(CardData data, Side owner)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        Data = data;
        Owner = owner;
        CurrentHp = data.Hp;
        HasAttacked = false;
    }

    /// <summary>
    /// Applies damage, clamped so health never reads below zero.
    /// Returns true if this card was destroyed by it.
    /// </summary>
    public bool TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        CurrentHp -= amount;
        if (CurrentHp < 0)
        {
            CurrentHp = 0;
        }

        return !IsAlive;
    }

    /// <summary>Restores this card to full health. For future healing or between-battle repair.</summary>
    public void Heal()
    {
        CurrentHp = Data.Hp;
    }

    public override string ToString()
    {
        return Data.DisplayName + " (" + Owner + ", " + CurrentHp + "/" + Data.Hp + " hp)";
    }
}

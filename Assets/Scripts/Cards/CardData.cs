using UnityEngine;

/// <summary>
/// The immutable definition of a card, authored as an asset in the Inspector.
///
/// This replaces two earlier approaches:
///
///  * The current one, in which card stats are hardcoded inside a nine-branch if-chain
///    in <c>Deck.AddToDeck()</c>, so a balance change is a code change.
///  * The abandoned 2014 <c>CardInfo</c> ScriptableObject, whose C# class was deleted
///    but whose assets survived. That design was the right instinct; this restores it,
///    including the <c>ctype</c> and <c>ceffect</c> fields.
///
/// The critical distinction the old <c>CardDef</c> lacked is definition versus state.
/// <see cref="CardData"/> is shared and never mutated; per-card runtime values live on
/// <see cref="CardInstance"/>. Conflating them is why damage used to persist for a
/// card's whole life and why cards were removed from zones by name-matching.
/// See Docs/CardSystem.md section 8.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "HToW/Card", order = 0)]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable identifier. Used for save data and deck lists; never shown to the player.")]
    [SerializeField] private string id;

    [Tooltip("Name shown on the card.")]
    [SerializeField] private string displayName;

    [Tooltip("Face artwork. Loaded as a direct reference rather than by Resources.Load path.")]
    [SerializeField] private Sprite art;

    [Header("Stats")]
    [Tooltip("Starting health.")]
    [SerializeField] private int hp = 1;

    [Tooltip("Damage dealt when attacking.")]
    [SerializeField] private int damage;

    [Tooltip("Supply paid to play this card.")]
    [SerializeField] private int supplyCost = 1;

    [Tooltip("Morale its OWNER loses when this card is destroyed. A deck's total morale " +
             "cost is effectively its life pool, so this is the most important stat on the card.")]
    [SerializeField] private int moraleCost = 1;

    [Header("Classification")]
    [SerializeField] private Faction faction = Faction.Celtic;
    [SerializeField] private CardType cardType = CardType.Warrior;

    [Tooltip("Keyword abilities. Authored now, implemented later — see Docs/Decisions.md Q-03.")]
    [SerializeField] private CardAbility[] abilities = new CardAbility[0];

    [Header("Presentation")]
    [Tooltip("One or two lines of flavour. Always visible, never blocking.")]
    [TextArea(2, 4)]
    [SerializeField] private string flavourText;

    [Tooltip("Longer historical note, shown on demand rather than forced on the player.")]
    [TextArea(3, 8)]
    [SerializeField] private string historicalNote;

    [Tooltip("Whether this card's subject is historical, mythological, or invented. " +
             "Shown to the player so the three never blur together.")]
    [SerializeField] private Historicity historicity = Historicity.Historical;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Art => art;
    public int Hp => hp;
    public int Damage => damage;
    public int SupplyCost => supplyCost;
    public int MoraleCost => moraleCost;
    public Faction Faction => faction;
    public CardType CardType => cardType;
    public string FlavourText => flavourText;
    public string HistoricalNote => historicalNote;
    public Historicity Historicity => historicity;

    /// <summary>The card's keyword abilities. Never null.</summary>
    public CardAbility[] Abilities => abilities ?? new CardAbility[0];

    /// <summary>True if this card has the given keyword.</summary>
    public bool Has(CardAbility ability)
    {
        if (abilities == null)
        {
            return false;
        }

        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i] == ability)
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(displayName) ? base.ToString() : displayName;
    }
}

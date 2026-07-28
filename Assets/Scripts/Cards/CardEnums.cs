/// <summary>
/// Which historical tradition a card belongs to. Factions currently exist only
/// implicitly, as "which text file lists this card's id". Making them explicit is what
/// lets faction identity become a design lever — see Docs/GameDesign.md section 5.
/// </summary>
public enum Faction
{
    /// <summary>Gaelic Ireland. Numerous, cheap, cohesive.</summary>
    Celtic,

    /// <summary>Norse raiders and settlers. Fast, elite, expensive to lose.</summary>
    Norse,

    /// <summary>Hired swords, available to any side at a price.</summary>
    Mercenary
}

/// <summary>
/// The role a card plays. Recovered from the abandoned 2014 ScriptableObject schema,
/// whose <c>ctype</c> field held "Warrior" on every card — implying other types were
/// planned. See Docs/CardSystem.md section 4c.
/// </summary>
public enum CardType
{
    /// <summary>A unit that occupies the board and fights.</summary>
    Warrior
}

/// <summary>
/// Keyword abilities. All four were designed and data-authored in 2014; the code that
/// implemented them was deleted along with the CardLibrary folder, and the names
/// survived only inside unloadable .asset files. Recovered in Docs/CardSystem.md 4c.
///
/// None of these are implemented yet — they are carried on <see cref="CardData"/> from
/// the start because retrofitting an ability system is far more expensive than leaving
/// room for one. See Docs/Decisions.md Q-03.
/// </summary>
public enum CardAbility
{
    /// <summary>Placeholder for cards with no keyword.</summary>
    None = 0,

    /// <summary>
    /// Missile troops. Intended: deals damage without suffering retaliation.
    /// Historically grounded — skirmishing at range was standard Gaelic practice.
    /// </summary>
    Volley = 1,

    /// <summary>
    /// Intended: stronger for each other friendly unit sharing its type.
    /// Reflects túath levies and Norse bóndi fighting as bodies of kin and neighbours.
    /// </summary>
    United = 2,

    /// <summary>
    /// Intended: stronger while wounded, but compelled to attack.
    /// Drawn from Norse saga tradition rather than the historical record.
    /// </summary>
    Berserker = 3,

    /// <summary>
    /// Intended: may attack the turn it is played.
    /// Expresses the Norse strategic advantage of speed delivered by ships.
    /// </summary>
    Bloodrush = 4
}

/// <summary>
/// How a card's subject relates to the historical record. The project's brief requires
/// that historical fact, Irish mythology and invented gameplay content stay clearly
/// distinguishable rather than blurring together. Surfacing this on the card itself
/// costs nothing and is more interesting than hiding it.
/// See Docs/HistoricalResearch.md.
/// </summary>
public enum Historicity
{
    /// <summary>Supported by historical or archaeological evidence.</summary>
    Historical,

    /// <summary>From Irish or Norse myth and saga literature. Real culture, not real events.</summary>
    Mythological,

    /// <summary>Invented for gameplay. Must be signposted as such in game.</summary>
    Fictional
}

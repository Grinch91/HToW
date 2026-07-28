using System.Collections.Generic;

/// <summary>
/// Deck legality. Plain C# and free of Unity types so the rules can be tested directly.
///
/// The interesting rule here is the last one. Because morale is lost only when your own
/// cards die, a deck's combined morale cost is effectively its life pool — so a deck
/// totalling less than starting morale cannot be defeated. That was literally true of
/// the original Celtic deck (21 morale against 30 starting), and it is the kind of fault
/// a deckbuilder would let a player recreate constantly if nothing checked for it.
/// </summary>
public static class DeckValidator
{
    public const int MinCards = 15;
    public const int MaxCards = 40;
    public const int MaxCopiesPerCard = 4;

    /// <summary>The outcome of checking a deck.</summary>
    public struct Result
    {
        public bool IsLegal;
        public List<string> Problems;

        /// <summary>Non-blocking advice — the deck is legal but probably not what you want.</summary>
        public List<string> Warnings;
    }

    /// <summary>
    /// Checks <paramref name="deck"/>. <paramref name="startingMorale"/> is needed for
    /// the life-pool rule.
    /// </summary>
    public static Result Validate(SavedDeck deck, int startingMorale)
    {
        Result result = new Result
        {
            IsLegal = true,
            Problems = new List<string>(),
            Warnings = new List<string>()
        };

        if (deck == null)
        {
            result.IsLegal = false;
            result.Problems.Add("No deck.");
            return result;
        }

        if (string.IsNullOrEmpty(deck.deckName))
        {
            result.Problems.Add("The deck needs a name.");
            result.IsLegal = false;
        }

        int count = deck.CardCount;

        if (count < MinCards)
        {
            result.Problems.Add("Too few cards: " + count + " of at least " + MinCards + ".");
            result.IsLegal = false;
        }

        if (count > MaxCards)
        {
            result.Problems.Add("Too many cards: " + count + " of at most " + MaxCards + ".");
            result.IsLegal = false;
        }

        foreach (SavedDeck.Entry entry in deck.entries)
        {
            CardData card = CardDatabase.Find(entry.cardId);
            string label = card != null ? card.DisplayName : entry.cardId;

            if (card == null)
            {
                result.Problems.Add("Unknown card '" + entry.cardId + "'.");
                result.IsLegal = false;
                continue;
            }

            if (entry.count > MaxCopiesPerCard)
            {
                result.Problems.Add(label + ": " + entry.count + " copies, limit is "
                                    + MaxCopiesPerCard + ".");
                result.IsLegal = false;
            }

            if (entry.count <= 0)
            {
                result.Problems.Add(label + " has a count of " + entry.count + ".");
                result.IsLegal = false;
            }
        }

        // The life-pool rule.
        int morale = deck.TotalMoraleCost;
        if (morale <= startingMorale)
        {
            result.Problems.Add(
                "This deck cannot be defeated, so a match with it could never end. Its total "
                + "morale cost is " + morale + ", but you start on " + startingMorale
                + " morale and only lose morale when your own cards die. Add more cards, or "
                + "costlier ones.");
            result.IsLegal = false;
        }
        else if (morale < startingMorale * 2)
        {
            result.Warnings.Add(
                "Slim margin: " + morale + " total morale against " + startingMorale
                + " starting. Losing a handful of cards will end the match.");
        }

        return result;
    }
}

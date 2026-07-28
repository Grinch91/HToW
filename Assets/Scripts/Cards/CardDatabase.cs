using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every card in the game, indexed by id.
///
/// Needed from Milestone 6 onward because a player-built deck cannot be a
/// <see cref="DeckData"/> asset — assets are authored at edit time, and a deck the
/// player saves at runtime must be stored as data and resolved back into cards on load.
/// Ids are the stable link between the two.
/// </summary>
public static class CardDatabase
{
    private static Dictionary<string, CardData> byId;
    private static List<CardData> all;

    /// <summary>Every card, in a stable order. Loads on first use.</summary>
    public static IReadOnlyList<CardData> All
    {
        get
        {
            EnsureLoaded();
            return all;
        }
    }

    /// <summary>Looks a card up by id, or null if there is no such card.</summary>
    public static CardData Find(string id)
    {
        EnsureLoaded();

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        CardData found;
        return byId.TryGetValue(id, out found) ? found : null;
    }

    /// <summary>Replaces the contents. For tests, which must not depend on Resources.</summary>
    public static void OverrideForTesting(IEnumerable<CardData> cards)
    {
        all = new List<CardData>();
        byId = new Dictionary<string, CardData>();

        foreach (CardData card in cards)
        {
            Register(card);
        }
    }

    /// <summary>Drops the cache so the next access reloads from Resources.</summary>
    public static void Reset()
    {
        all = null;
        byId = null;
    }

    private static void EnsureLoaded()
    {
        if (all != null)
        {
            return;
        }

        all = new List<CardData>();
        byId = new Dictionary<string, CardData>();

        CardData[] loaded = Resources.LoadAll<CardData>("CardData");
        System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.Id, b.Id));

        foreach (CardData card in loaded)
        {
            Register(card);
        }

        Debug.Log("CardDatabase loaded " + all.Count + " cards");
    }

    private static void Register(CardData card)
    {
        if (card == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(card.Id))
        {
            Debug.LogWarning("Card '" + card.name + "' has no id and cannot be saved in a deck.");
            return;
        }

        if (byId.ContainsKey(card.Id))
        {
            Debug.LogWarning("Duplicate card id '" + card.Id + "'; ignoring " + card.name);
            return;
        }

        byId.Add(card.Id, card);
        all.Add(card);
    }
}

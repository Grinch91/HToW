using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A deck the player built, stored as card ids so it can be saved to disk.
///
/// <see cref="DeckData"/> is a ScriptableObject asset and therefore authored at edit
/// time; it cannot represent a deck created while the game is running. This is the
/// runtime counterpart, resolved back into cards through <see cref="CardDatabase"/>.
///
/// The 2014 project had a <c>DeckData</c> asset literally named "Custom", so a
/// deckbuilder was always intended — it simply never got written.
/// </summary>
[Serializable]
public class SavedDeck
{
    [Serializable]
    public struct Entry
    {
        public string cardId;
        public int count;
    }

    public string deckName = "New Deck";
    public List<Entry> entries = new List<Entry>();

    public SavedDeck()
    {
    }

    public SavedDeck(string name)
    {
        deckName = name;
    }

    /// <summary>Total cards, counting duplicates.</summary>
    public int CardCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                total += Mathf.Max(0, entries[i].count);
            }
            return total;
        }
    }

    /// <summary>
    /// Combined morale cost — effectively the deck's life pool, because morale is only
    /// lost when your own cards die. A deck whose total does not exceed starting morale
    /// literally cannot lose, which was true of the original Celtic deck (bug M12).
    /// </summary>
    public int TotalMoraleCost
    {
        get
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                CardData card = CardDatabase.Find(entries[i].cardId);
                if (card != null)
                {
                    total += card.MoraleCost * Mathf.Max(0, entries[i].count);
                }
            }
            return total;
        }
    }

    /// <summary>How many copies of a card this deck holds.</summary>
    public int CountOf(string cardId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].cardId == cardId)
            {
                return entries[i].count;
            }
        }
        return 0;
    }

    /// <summary>Adds one copy.</summary>
    public void Add(string cardId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].cardId == cardId)
            {
                Entry entry = entries[i];
                entry.count++;
                entries[i] = entry;
                return;
            }
        }

        entries.Add(new Entry { cardId = cardId, count = 1 });
    }

    /// <summary>Removes one copy, dropping the entry entirely when it reaches zero.</summary>
    public void Remove(string cardId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].cardId != cardId)
            {
                continue;
            }

            Entry entry = entries[i];
            entry.count--;

            if (entry.count <= 0)
            {
                entries.RemoveAt(i);
            }
            else
            {
                entries[i] = entry;
            }

            return;
        }
    }

    /// <summary>
    /// Builds the runtime cards for a match. Ids that no longer exist are skipped with a
    /// warning rather than throwing, so a save from an older build still loads.
    /// </summary>
    public List<CardInstance> BuildCards(Side owner)
    {
        List<CardInstance> cards = new List<CardInstance>(CardCount);

        for (int i = 0; i < entries.Count; i++)
        {
            CardData card = CardDatabase.Find(entries[i].cardId);
            if (card == null)
            {
                Debug.LogWarning("Deck '" + deckName + "' refers to unknown card id '"
                                 + entries[i].cardId + "'; skipping.");
                continue;
            }

            for (int copy = 0; copy < entries[i].count; copy++)
            {
                cards.Add(new CardInstance(card, owner));
            }
        }

        return cards;
    }

    /// <summary>Copies an authored <see cref="DeckData"/> asset into an editable deck.</summary>
    public static SavedDeck From(DeckData source)
    {
        SavedDeck deck = new SavedDeck(source.DeckName);

        foreach (DeckData.Entry entry in source.Entries)
        {
            if (entry.card != null)
            {
                deck.entries.Add(new Entry { cardId = entry.card.Id, count = entry.count });
            }
        }

        return deck;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A named deck list: which cards, and how many of each.
///
/// Replaces the loose text files (<c>Assets/celtic.txt</c>) that were read with
/// <c>File.ReadAllLines(Application.dataPath + ...)</c>. That approach broke on any
/// non-desktop platform, left deck data editable by the player in a shipped build, and
/// relied on a nine-branch if-chain to turn id strings into cards.
///
/// The 2014 project already had <c>DeckData</c> assets for Celtic, Viking and — tellingly
/// — Custom, implying a deckbuilder was always planned. See Docs/CardSystem.md 4c.
/// </summary>
[CreateAssetMenu(fileName = "NewDeck", menuName = "HToW/Deck", order = 1)]
public class DeckData : ScriptableObject
{
    /// <summary>A number of copies of one card.</summary>
    [Serializable]
    public struct Entry
    {
        public CardData card;

        [Min(1)]
        public int count;
    }

    [SerializeField] private string deckName;

    [Tooltip("Which historical tradition this deck represents.")]
    [SerializeField] private Faction faction = Faction.Celtic;

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public string DeckName => deckName;
    public Faction Faction => faction;
    public IReadOnlyList<Entry> Entries => entries;

    /// <summary>Total number of cards, counting duplicates.</summary>
    public int CardCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].card != null)
                {
                    total += Mathf.Max(1, entries[i].count);
                }
            }
            return total;
        }
    }

    /// <summary>
    /// The deck's combined morale cost.
    ///
    /// Because morale is lost only when your own cards die, this figure is effectively
    /// the deck's life pool — and it must exceed starting morale or the deck cannot lose.
    /// The original 5-card Celtic deck totalled 21 against 30 starting morale, making the
    /// player literally unkillable (bug M12). Exposed here so that can be checked at a
    /// glance while authoring.
    /// </summary>
    public int TotalMoraleCost
    {
        get
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].card != null)
                {
                    total += entries[i].card.MoraleCost * Mathf.Max(1, entries[i].count);
                }
            }
            return total;
        }
    }

    /// <summary>
    /// Builds the runtime cards for one match, one <see cref="CardInstance"/> per copy.
    /// The returned list is unshuffled and owned by the caller.
    /// </summary>
    public List<CardInstance> BuildCards(Side owner)
    {
        List<CardInstance> cards = new List<CardInstance>(CardCount);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.card == null)
            {
                Debug.LogWarning("Deck '" + name + "' has an empty entry at index " + i + "; skipping.");
                continue;
            }

            int copies = Mathf.Max(1, entry.count);
            for (int copy = 0; copy < copies; copy++)
            {
                cards.Add(new CardInstance(entry.card, owner));
            }
        }

        return cards;
    }
}

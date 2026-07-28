using System.Collections.Generic;
using UnityEngine;

/// <summary>What a zone is for. Determines how cards enter and leave it.</summary>
public enum ZoneKind
{
    /// <summary>Ordered pile. Cards are drawn from the top.</summary>
    DrawPile,

    /// <summary>The owner's hand. Cards leave by being played, not by position.</summary>
    Hand,

    /// <summary>Units in play. An unordered set — position carries no meaning.</summary>
    Board
}

/// <summary>
/// A place cards can be, and the anchor they lay out around.
///
/// Replaces <c>Deck</c>, which was used for the draw pile, the hand *and* the board at
/// once. A pile is a queue and a board is a set, and treating the board as a queue is
/// what caused bug C2: playing a card appended it to the end and then dealt whatever
/// sat at index 0, so every play after the first spawned a duplicate of an older card
/// and silently queued the card actually chosen.
///
/// Here, cards move by reference. <see cref="Remove"/> takes the instance you mean.
/// </summary>
public class CardZone : MonoBehaviour
{
    [Tooltip("Horizontal gap between laid-out cards, in world units.")]
    [SerializeField] private float spacing = 3.0f;

    private readonly List<CardInstance> cards = new List<CardInstance>();

    /// <summary>What this zone is for. Set by <see cref="Configure"/> at match start.</summary>
    public ZoneKind Kind { get; private set; }

    /// <summary>Which side owns this zone.</summary>
    public Side Owner { get; private set; }

    public IReadOnlyList<CardInstance> Cards => cards;
    public int Count => cards.Count;
    public bool IsEmpty => cards.Count == 0;

    /// <summary>
    /// Assigns role and ownership. Called by <c>Game</c> at start-up rather than
    /// authored per-object, because four of the six zones are instances of one shared
    /// prefab and would otherwise need per-instance overrides.
    /// </summary>
    public void Configure(ZoneKind kind, Side owner)
    {
        Kind = kind;
        Owner = owner;
    }

    public void Add(CardInstance card)
    {
        if (card == null)
        {
            return;
        }

        cards.Add(card);
    }

    public void AddRange(IEnumerable<CardInstance> range)
    {
        foreach (CardInstance card in range)
        {
            Add(card);
        }
    }

    /// <summary>Removes exactly the instance given. Returns false if it was not here.</summary>
    public bool Remove(CardInstance card)
    {
        return cards.Remove(card);
    }

    public bool Contains(CardInstance card)
    {
        return cards.Contains(card);
    }

    public void Clear()
    {
        cards.Clear();
    }

    /// <summary>
    /// Takes the top card of a pile, or null when empty.
    ///
    /// The old <c>Deck.Deal()</c> guarded with <c>if (deck.Count != null)</c> on an int,
    /// which is always true, and then indexed an empty list. See Docs/KnownBugs.md C6.
    /// </summary>
    public CardInstance DrawTop()
    {
        if (cards.Count == 0)
        {
            return null;
        }

        CardInstance top = cards[0];
        cards.RemoveAt(0);
        return top;
    }

    /// <summary>
    /// Fisher-Yates. The old implementation called itself a Knuth shuffle but drew from
    /// the full range on every iteration, which does not produce a uniform permutation
    /// (bug N2).
    /// </summary>
    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CardInstance swap = cards[i];
            cards[i] = cards[j];
            cards[j] = swap;
        }
    }

    /// <summary>A copy, safe to iterate while the zone is being modified (bug F1).</summary>
    public List<CardInstance> Snapshot()
    {
        return new List<CardInstance>(cards);
    }

    /// <summary>
    /// Positions this zone's card views in a row centred on the zone's own transform.
    ///
    /// The old code derived each card's x from the *draw pile's* remaining count, so
    /// once a deck emptied every card spawned at identical coordinates and stacked on
    /// top of each other (bug N6).
    /// </summary>
    public void LayOut()
    {
        float startX = -((cards.Count - 1) * spacing) * 0.5f;

        for (int i = 0; i < cards.Count; i++)
        {
            CardView view = cards[i].View;
            if (view == null)
            {
                continue;
            }

            view.transform.position = transform.position + new Vector3(startX + (i * spacing), 0.0f, 0.0f);
        }
    }
}

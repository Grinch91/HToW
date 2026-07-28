using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Covers the Milestone 5 opponent and the Milestone 6 deckbuilding model.
/// </summary>
public class DeckBuildingTests
{
    static CardData Card(string id, int hp, int damage, int cost, int moraleCost, params CardAbility[] abilities)
    {
        CardData data = ScriptableObject.CreateInstance<CardData>();
        SerializedLikeSetter.Set(data, "id", id);
        SerializedLikeSetter.Set(data, "displayName", id);
        SerializedLikeSetter.Set(data, "hp", hp);
        SerializedLikeSetter.Set(data, "damage", damage);
        SerializedLikeSetter.Set(data, "supplyCost", cost);
        SerializedLikeSetter.Set(data, "moraleCost", moraleCost);
        SerializedLikeSetter.Set(data, "abilities", abilities ?? new CardAbility[0]);
        return data;
    }

    CardData cheap;
    CardData strong;
    CardData chariot;

    [SetUp]
    public void SetUp()
    {
        cheap = Card("archer", hp: 4, damage: 2, cost: 1, moraleCost: 1);
        strong = Card("huscarl", hp: 12, damage: 15, cost: 5, moraleCost: 10);
        chariot = Card("chariot", hp: 4, damage: 2, cost: 1, moraleCost: 2, CardAbility.Volley);

        // Six card types, because the 4-copy limit means three types could never reach
        // the 15-card minimum and no legal deck would be constructible at all.
        CardDatabase.OverrideForTesting(new[]
        {
            cheap,
            strong,
            chariot,
            Card("ceithern", hp: 8, damage: 4, cost: 3, moraleCost: 6),
            Card("fianna", hp: 12, damage: 12, cost: 5, moraleCost: 10),
            Card("raider", hp: 4, damage: 4, cost: 2, moraleCost: 4),

            // Tuned so that huscarl-attacks-champion is a *marginal* trade: huscarl
            // kills it (10 hp vs 15 damage) but dies in return (12 hp vs 12 damage),
            // swapping 10 morale for 7. That is the band where difficulty should differ.
            Card("champion", hp: 10, damage: 12, cost: 4, moraleCost: 7)
        });
    }

    [TearDown]
    public void TearDown()
    {
        CardDatabase.Reset();
    }

    // ---- AI: playing ---------------------------------------------------------------

    [Test]
    public void BalancedAiPlaysTheStrongestAffordableCard()
    {
        AiController ai = new AiController(AiDifficulty.Balanced);
        var hand = new[] { new CardInstance(cheap, Side.AI), new CardInstance(strong, Side.AI) };

        CardInstance choice = ai.ChoosePlay(hand, availableSupply: 10);

        Assert.AreSame(strong, choice.Data);
    }

    [Test]
    public void CautiousAiPlaysTheCheapestAffordableCard()
    {
        AiController ai = new AiController(AiDifficulty.Cautious);
        var hand = new[] { new CardInstance(cheap, Side.AI), new CardInstance(strong, Side.AI) };

        CardInstance choice = ai.ChoosePlay(hand, availableSupply: 10);

        Assert.AreSame(cheap, choice.Data, "difficulty must actually change behaviour");
    }

    [Test]
    public void AiNeverPlaysWhatItCannotAfford()
    {
        AiController ai = new AiController(AiDifficulty.Balanced);
        var hand = new[] { new CardInstance(strong, Side.AI) };

        Assert.IsNull(ai.ChoosePlay(hand, availableSupply: 2));
        Assert.IsNotNull(ai.ChoosePlay(hand, availableSupply: 5));
    }

    [Test]
    public void AiReturnsNullOnAnEmptyHand()
    {
        AiController ai = new AiController();
        Assert.IsNull(ai.ChoosePlay(new CardInstance[0], availableSupply: 10));
    }

    // ---- AI: attacking -------------------------------------------------------------

    [Test]
    public void OnlyRuthlessAiAcceptsASlightlyLosingTrade()
    {
        // Huscarl kills the champion but dies doing it: 10 morale spent for 7 gained.
        CardInstance attacker = new CardInstance(strong, Side.AI);
        var enemies = new[] { new CardInstance(CardDatabase.Find("champion"), Side.Player) };

        Assert.IsNull(new AiController(AiDifficulty.Cautious).ChooseTarget(attacker, enemies),
            "a cautious opponent should not spend a card on a losing trade");
        Assert.IsNull(new AiController(AiDifficulty.Balanced).ChooseTarget(attacker, enemies),
            "a balanced opponent takes only trades that come out ahead");
        Assert.IsNotNull(new AiController(AiDifficulty.Ruthless).ChooseTarget(attacker, enemies),
            "a ruthless opponent accepts a small loss for tempo");
    }

    [Test]
    public void NoDifficultyThrowsAwayACardForNothing()
    {
        // An archer chipping 2 damage off a huscarl and dying for it is bad at any level.
        CardInstance attacker = new CardInstance(cheap, Side.AI);
        var enemies = new[] { new CardInstance(strong, Side.Player) };

        foreach (AiDifficulty difficulty in new[] { AiDifficulty.Cautious, AiDifficulty.Balanced, AiDifficulty.Ruthless })
        {
            Assert.IsNull(new AiController(difficulty).ChooseTarget(attacker, enemies),
                difficulty + " should not throw a card away for chip damage");
        }
    }

    [Test]
    public void EveryDifficultyTakesAFreeAttack()
    {
        // Volley means no retaliation, so there is no risk at any difficulty.
        CardInstance attacker = new CardInstance(chariot, Side.AI);
        var enemies = new[] { new CardInstance(strong, Side.Player) };

        foreach (AiDifficulty difficulty in new[] { AiDifficulty.Cautious, AiDifficulty.Balanced, AiDifficulty.Ruthless })
        {
            Assert.IsNotNull(new AiController(difficulty).ChooseTarget(attacker, enemies),
                difficulty + " should take a risk-free attack");
        }
    }

    // ---- SavedDeck -----------------------------------------------------------------

    [Test]
    public void AddingAndRemovingTracksCounts()
    {
        SavedDeck deck = new SavedDeck("Test");

        deck.Add("archer");
        deck.Add("archer");
        deck.Add("huscarl");

        Assert.AreEqual(2, deck.CountOf("archer"));
        Assert.AreEqual(3, deck.CardCount);

        deck.Remove("archer");
        Assert.AreEqual(1, deck.CountOf("archer"));

        deck.Remove("archer");
        Assert.AreEqual(0, deck.CountOf("archer"));
        Assert.AreEqual(1, deck.entries.Count, "an emptied entry should be dropped");
    }

    [Test]
    public void TotalMoraleCostSumsAcrossCopies()
    {
        SavedDeck deck = new SavedDeck("Test");
        deck.Add("huscarl");     // 10
        deck.Add("huscarl");     // 10
        deck.Add("archer");      // 1

        Assert.AreEqual(21, deck.TotalMoraleCost);
    }

    [Test]
    public void BuildCardsProducesOneInstancePerCopy()
    {
        SavedDeck deck = new SavedDeck("Test");
        deck.Add("archer");
        deck.Add("archer");
        deck.Add("huscarl");

        var built = deck.BuildCards(Side.Player);

        Assert.AreEqual(3, built.Count);
        Assert.AreNotSame(built[0], built[1], "copies must be distinct instances");
        foreach (CardInstance card in built)
        {
            Assert.AreEqual(Side.Player, card.Owner);
        }
    }

    [Test]
    public void UnknownCardIdsAreSkippedRatherThanThrowing()
    {
        // A save written by an older build must still load.
        SavedDeck deck = new SavedDeck("Test");
        deck.Add("archer");
        deck.Add("a-card-that-no-longer-exists");

        LogAssert.ignoreFailingMessages = true;
        var built = deck.BuildCards(Side.Player);
        LogAssert.ignoreFailingMessages = false;

        Assert.AreEqual(1, built.Count);
    }

    // ---- DeckValidator -------------------------------------------------------------

    static SavedDeck DeckOf(string cardId, int count)
    {
        SavedDeck deck = new SavedDeck("Test");
        for (int i = 0; i < count; i++)
        {
            deck.Add(cardId);
        }
        return deck;
    }

    [Test]
    public void ADeckThatCannotLoseIsRejected()
    {
        // This is bug M12 as a rule: 20 archers is 20 total morale against 30 starting,
        // so every card could die and the player would still be alive.
        SavedDeck deck = DeckOf("archer", 20);

        DeckValidator.Result result = DeckValidator.Validate(deck, startingMorale: 30);

        Assert.IsFalse(result.IsLegal);
        Assert.IsTrue(result.Problems.Exists(p => p.Contains("cannot be defeated")));
    }

    /// <summary>A 16-card deck: 4 each of four card types, 76 total morale.</summary>
    static SavedDeck LegalDeck()
    {
        SavedDeck deck = new SavedDeck("Legal");
        foreach (string id in new[] { "huscarl", "ceithern", "raider", "chariot" })
        {
            for (int i = 0; i < 4; i++)
            {
                deck.Add(id);
            }
        }
        return deck;
    }

    [Test]
    public void AWellFormedDeckIsLegal()
    {
        DeckValidator.Result result = DeckValidator.Validate(LegalDeck(), startingMorale: 30);

        Assert.IsTrue(result.IsLegal, string.Join(" | ", result.Problems.ToArray()));
        Assert.AreEqual(16, LegalDeck().CardCount);
        Assert.AreEqual(88, LegalDeck().TotalMoraleCost);
    }

    [Test]
    public void TooFewCardsIsRejected()
    {
        DeckValidator.Result result = DeckValidator.Validate(DeckOf("huscarl", 4), 30);

        Assert.IsFalse(result.IsLegal);
        Assert.IsTrue(result.Problems.Exists(p => p.Contains("Too few")));
    }

    [Test]
    public void ExceedingTheCopyLimitIsRejected()
    {
        SavedDeck deck = LegalDeck();
        deck.Add("huscarl");   // a fifth copy

        DeckValidator.Result result = DeckValidator.Validate(deck, 30);

        Assert.IsFalse(result.IsLegal);
        Assert.IsTrue(result.Problems.Exists(p => p.Contains("copies")));
    }

    [Test]
    public void ATightMoraleMarginWarnsWithoutBlocking()
    {
        // Legal, but a handful of losses would end the match — worth saying so.
        SavedDeck deck = new SavedDeck("Thin");
        for (int i = 0; i < 4; i++) deck.Add("chariot");   // 8
        for (int i = 0; i < 4; i++) deck.Add("archer");    // 4
        for (int i = 0; i < 4; i++) deck.Add("raider");    // 16
        for (int i = 0; i < 3; i++) deck.Add("ceithern");  // 18  -> 46 total, 15 cards

        DeckValidator.Result result = DeckValidator.Validate(deck, 30);

        Assert.IsTrue(result.IsLegal, string.Join(" | ", result.Problems.ToArray()));
        Assert.AreEqual(1, result.Warnings.Count, "should warn about the slim margin");
    }

    [Test]
    public void SavedDeckRoundTripsThroughJson()
    {
        // The save format must survive a write/read cycle, since this is the project's
        // first persistence of any kind.
        SavedDeck original = LegalDeck();
        original.deckName = "Round Trip";

        string json = JsonUtility.ToJson(original);
        SavedDeck restored = JsonUtility.FromJson<SavedDeck>(json);

        Assert.AreEqual("Round Trip", restored.deckName);
        Assert.AreEqual(original.CardCount, restored.CardCount);
        Assert.AreEqual(original.TotalMoraleCost, restored.TotalMoraleCost);
        Assert.AreEqual(4, restored.CountOf("huscarl"));
    }

    [Test]
    public void ProfileRoundTripsThroughJson()
    {
        PlayerProfile profile = new PlayerProfile();
        profile.decks.Add(LegalDeck());
        profile.selectedDeckName = "Legal";

        PlayerProfile restored = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));

        Assert.AreEqual(1, restored.decks.Count);
        Assert.IsNotNull(restored.SelectedDeck);
        Assert.AreEqual("Legal", restored.SelectedDeck.deckName);
    }
}

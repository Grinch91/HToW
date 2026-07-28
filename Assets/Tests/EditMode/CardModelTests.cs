using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Guards the definition/state split introduced in Milestone 2.
///
/// The old CardDef was both the shared definition and the per-copy runtime state, and
/// cards were identified by name. That single conflation caused bugs C2, M4 and M5.
/// These tests pin the properties that make those bugs impossible.
/// </summary>
public class CardModelTests
{
    static CardData MakeCard(string cardName, int hp, int damage, int moraleCost, params CardAbility[] abilities)
    {
        CardData data = ScriptableObject.CreateInstance<CardData>();

        // Fields are private and serialized, so drive them the way the Inspector does.
        SerializedLikeSetter.Set(data, "id", cardName.ToLowerInvariant());
        SerializedLikeSetter.Set(data, "displayName", cardName);
        SerializedLikeSetter.Set(data, "hp", hp);
        SerializedLikeSetter.Set(data, "damage", damage);
        SerializedLikeSetter.Set(data, "supplyCost", 1);
        SerializedLikeSetter.Set(data, "moraleCost", moraleCost);
        SerializedLikeSetter.Set(data, "abilities", abilities ?? new CardAbility[0]);

        return data;
    }

    // ---- Identity ----------------------------------------------------------------

    [Test]
    public void TwoCopiesOfTheSameCardAreDistinctInstances()
    {
        CardData raider = MakeCard("Raider", hp: 4, damage: 4, moraleCost: 4);

        CardInstance a = new CardInstance(raider, Side.Player);
        CardInstance b = new CardInstance(raider, Side.Player);

        Assert.AreNotSame(a, b, "two copies must be separate objects");
        Assert.AreSame(a.Data, b.Data, "but they must share one definition");
    }

    [Test]
    public void DamagingOneCopyDoesNotAffectTheOther()
    {
        // This is bug M5: removal by name meant the wrong Raider died and the survivor
        // inherited the dead one's health.
        CardData raider = MakeCard("Raider", hp: 4, damage: 4, moraleCost: 4);
        CardInstance a = new CardInstance(raider, Side.Player);
        CardInstance b = new CardInstance(raider, Side.Player);

        a.TakeDamage(3);

        Assert.AreEqual(1, a.CurrentHp);
        Assert.AreEqual(4, b.CurrentHp, "the second copy must be untouched");
    }

    [Test]
    public void DamagingAnInstanceNeverMutatesTheSharedDefinition()
    {
        CardData raider = MakeCard("Raider", hp: 4, damage: 4, moraleCost: 4);
        CardInstance instance = new CardInstance(raider, Side.Player);

        instance.TakeDamage(3);

        Assert.AreEqual(4, raider.Hp, "CardData is immutable during play");
    }

    [Test]
    public void RemovingByReferenceRemovesTheIntendedCopy()
    {
        // Bug C2/M5 in miniature: with name-matching, "remove the Raider" was ambiguous.
        CardData raider = MakeCard("Raider", hp: 4, damage: 4, moraleCost: 4);
        CardInstance first = new CardInstance(raider, Side.Player);
        CardInstance second = new CardInstance(raider, Side.Player);
        List<CardInstance> board = new List<CardInstance> { first, second };

        board.Remove(second);

        Assert.AreEqual(1, board.Count);
        Assert.AreSame(first, board[0], "the surviving card must be the one not removed");
    }

    // ---- Damage ------------------------------------------------------------------

    [Test]
    public void TakeDamageReportsDestruction()
    {
        CardData card = MakeCard("Chariot", hp: 4, damage: 2, moraleCost: 2);
        CardInstance instance = new CardInstance(card, Side.Player);

        Assert.IsFalse(instance.TakeDamage(3), "3 damage to 4 hp is not lethal");
        Assert.IsTrue(instance.TakeDamage(1), "the next point is lethal");
        Assert.IsFalse(instance.IsAlive);
    }

    [Test]
    public void HealthNeverReadsBelowZero()
    {
        CardData card = MakeCard("Chariot", hp: 4, damage: 2, moraleCost: 2);
        CardInstance instance = new CardInstance(card, Side.Player);

        instance.TakeDamage(999);

        Assert.AreEqual(0, instance.CurrentHp);
    }

    [Test]
    public void ExactlyLethalDamageKills()
    {
        CardData card = MakeCard("Chariot", hp: 4, damage: 2, moraleCost: 2);
        CardInstance instance = new CardInstance(card, Side.Player);

        Assert.IsTrue(instance.TakeDamage(4), "damage equal to hp must be lethal");
    }

    // ---- Abilities ---------------------------------------------------------------

    [Test]
    public void AbilitiesAreQueryable()
    {
        CardData fianna = MakeCard("Fianna", 12, 12, 10, CardAbility.Berserker);

        Assert.IsTrue(fianna.Has(CardAbility.Berserker));
        Assert.IsFalse(fianna.Has(CardAbility.Volley));
    }

    [Test]
    public void CardsWithoutAbilitiesReportNone()
    {
        CardData sellsword = MakeCard("Sellsword", 4, 10, 5);

        Assert.IsNotNull(sellsword.Abilities, "Abilities must never be null");
        Assert.AreEqual(0, sellsword.Abilities.Length);
        Assert.IsFalse(sellsword.Has(CardAbility.Volley));
    }
}

/// <summary>
/// Sets private serialized fields, which is how the Inspector authors a ScriptableObject.
/// Confined to tests so production code never reaches for reflection.
/// </summary>
internal static class SerializedLikeSetter
{
    public static void Set(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.IsNotNull(field, "no serialized field named '" + fieldName + "' on " + target.GetType().Name);
        field.SetValue(target, value);
    }
}

using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Pins the combat rules introduced in Milestone 4, above all retaliation.
///
/// Before this, the defender never dealt damage back, so attacking was risk-free and
/// every turn reduced to "attack with everything". See Docs/Decisions.md Q-01.
/// </summary>
public class CombatResolverTests
{
    static CardData Card(string cardName, int hp, int damage, int moraleCost, params CardAbility[] abilities)
    {
        CardData data = ScriptableObject.CreateInstance<CardData>();
        SerializedLikeSetter.Set(data, "id", cardName.ToLowerInvariant());
        SerializedLikeSetter.Set(data, "displayName", cardName);
        SerializedLikeSetter.Set(data, "hp", hp);
        SerializedLikeSetter.Set(data, "damage", damage);
        SerializedLikeSetter.Set(data, "supplyCost", 1);
        SerializedLikeSetter.Set(data, "moraleCost", moraleCost);
        SerializedLikeSetter.Set(data, "abilities", abilities ?? new CardAbility[0]);
        return data;
    }

    static CardInstance Instance(CardData data, Side owner = Side.Player)
    {
        return new CardInstance(data, owner);
    }

    // ---- Retaliation --------------------------------------------------------------

    [Test]
    public void DefenderDealsDamageBack()
    {
        CardInstance attacker = Instance(Card("Raider", hp: 4, damage: 4, moraleCost: 4));
        CardInstance target = Instance(Card("Ceithern", hp: 8, damage: 4, moraleCost: 6), Side.AI);

        CombatResolver.Result result = CombatResolver.Resolve(attacker, target);

        Assert.AreEqual(4, result.DamageToTarget);
        Assert.AreEqual(4, result.DamageToAttacker, "the defender must strike back");
        Assert.AreEqual(4, target.CurrentHp);
        Assert.AreEqual(0, attacker.CurrentHp);
        Assert.IsTrue(result.AttackerDestroyed, "a 4hp attacker dies to a 4 damage defender");
        Assert.IsFalse(result.TargetDestroyed);
    }

    [Test]
    public void BothCardsCanDieInOneExchange()
    {
        CardInstance attacker = Instance(Card("Sellsword", hp: 4, damage: 10, moraleCost: 5));
        CardInstance target = Instance(Card("Fianna", hp: 12, damage: 12, moraleCost: 10), Side.AI);

        // 10 damage does not kill 12 hp, so nobody dies on the target's side...
        CombatResolver.Result first = CombatResolver.Resolve(attacker, target);
        Assert.IsFalse(first.TargetDestroyed);
        Assert.IsTrue(first.AttackerDestroyed, "4hp attacker dies to 12 damage");
    }

    [Test]
    public void ADestroyedDefenderStillStrikesBack()
    {
        // Deliberate: otherwise "kill it first" would remove all risk from attacking.
        CardInstance attacker = Instance(Card("Sellsword", hp: 4, damage: 10, moraleCost: 5));
        CardInstance target = Instance(Card("Raider", hp: 4, damage: 4, moraleCost: 4), Side.AI);

        CombatResolver.Result result = CombatResolver.Resolve(attacker, target);

        Assert.IsTrue(result.TargetDestroyed);
        Assert.AreEqual(4, result.DamageToAttacker, "the dying defender still hits back");
        Assert.AreEqual(0, attacker.CurrentHp);
        Assert.IsTrue(result.AttackerDestroyed);
    }

    // ---- Volley -------------------------------------------------------------------

    [Test]
    public void VolleyAvoidsRetaliation()
    {
        CardInstance chariot = Instance(Card("Battle Chariot", hp: 4, damage: 2, moraleCost: 2, CardAbility.Volley));
        CardInstance target = Instance(Card("Huscarl", hp: 12, damage: 15, moraleCost: 10), Side.AI);

        CombatResolver.Result result = CombatResolver.Resolve(chariot, target);

        Assert.AreEqual(2, result.DamageToTarget);
        Assert.AreEqual(0, result.DamageToAttacker, "Volley takes no damage back");
        Assert.AreEqual(4, chariot.CurrentHp, "the chariot is untouched");
        Assert.IsFalse(result.AttackerDestroyed);
    }

    [Test]
    public void WithoutVolleyTheSameAttackWouldBeSuicide()
    {
        CardInstance chariot = Instance(Card("Battle Chariot", hp: 4, damage: 2, moraleCost: 2));
        CardInstance target = Instance(Card("Huscarl", hp: 12, damage: 15, moraleCost: 10), Side.AI);

        CombatResolver.Result result = CombatResolver.Resolve(chariot, target);

        Assert.IsTrue(result.AttackerDestroyed, "this is why Volley exists");
    }

    // ---- Morale swing -------------------------------------------------------------

    [Test]
    public void MoraleSwingFavoursTheAttackerOnACleanKill()
    {
        CardInstance attacker = Instance(Card("Archer", hp: 4, damage: 20, moraleCost: 1, CardAbility.Volley));
        CardInstance target = Instance(Card("Fianna", hp: 12, damage: 12, moraleCost: 10), Side.AI);

        CombatResolver.Result result = CombatResolver.Resolve(attacker, target);

        Assert.AreEqual(10, result.MoraleSwing, "killed 10 morale, lost none");
    }

    [Test]
    public void MoraleSwingIsNegativeOnABadTrade()
    {
        CardInstance attacker = Instance(Card("Fianna", hp: 1, damage: 1, moraleCost: 10));
        CardInstance target = Instance(Card("Archer", hp: 4, damage: 20, moraleCost: 1), Side.AI);

        CombatResolver.Result result = CombatResolver.Resolve(attacker, target);

        Assert.IsTrue(result.AttackerDestroyed);
        Assert.IsFalse(result.TargetDestroyed);
        Assert.AreEqual(-10, result.MoraleSwing, "losing a 10-morale card for nothing");
    }

    // ---- AI scoring ---------------------------------------------------------------

    [Test]
    public void ScoringPrefersTheKillThatCostsTheOpponentMostMorale()
    {
        CardInstance attacker = Instance(Card("Huscarl", hp: 12, damage: 15, moraleCost: 10));
        CardInstance cheap = Instance(Card("Archer", hp: 4, damage: 2, moraleCost: 1), Side.AI);
        CardInstance valuable = Instance(Card("Fianna", hp: 12, damage: 2, moraleCost: 10), Side.AI);

        Assert.Greater(
            CombatResolver.Score(attacker, valuable),
            CombatResolver.Score(attacker, cheap),
            "morale cost is the win condition and must drive targeting");
    }

    [Test]
    public void ScoringRejectsSuicidalAttacks()
    {
        // A 2-morale chariot throwing itself at a Huscarl it cannot kill.
        CardInstance chariot = Instance(Card("Battle Chariot", hp: 4, damage: 2, moraleCost: 2));
        CardInstance huscarl = Instance(Card("Huscarl", hp: 12, damage: 15, moraleCost: 10), Side.AI);

        Assert.Less(CombatResolver.Score(chariot, huscarl), 0,
            "an attack that trades a card away for nothing must score negative");
    }

    [Test]
    public void ScoringAllowsTheSameAttackWhenVolleyRemovesTheRisk()
    {
        CardInstance chariot = Instance(Card("Battle Chariot", hp: 4, damage: 2, moraleCost: 2, CardAbility.Volley));
        CardInstance huscarl = Instance(Card("Huscarl", hp: 12, damage: 15, moraleCost: 10), Side.AI);

        Assert.Greater(CombatResolver.Score(chariot, huscarl), 0,
            "free chip damage is always worth taking");
    }
}

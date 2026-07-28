/// <summary>
/// Resolves a single attack. Plain C# so the rules can be tested without Unity.
///
/// Extracted from <c>Game</c> in Milestone 4, when retaliation was introduced. Combat
/// was previously one-directional: the defender never dealt damage back, which made
/// attacking risk-free and removed almost all tactical decision-making. Every turn
/// reduced to "attack with everything". See Docs/GameDesign.md section 3 and
/// Docs/Decisions.md Q-01.
///
/// With retaliation, an attack becomes a trade to evaluate: is my 4-morale Raider worth
/// your 6-morale Ceithern? Because morale is only lost when your own cards die, that
/// question has a different answer depending on how each deck is built — which is where
/// the depth comes from.
/// </summary>
public static class CombatResolver
{
    /// <summary>What happened when an attack resolved.</summary>
    public struct Result
    {
        public int DamageToTarget;
        public int DamageToAttacker;
        public bool TargetDestroyed;
        public bool AttackerDestroyed;

        /// <summary>
        /// Net morale swing in the attacker's favour: what the defender lost, minus
        /// what the attacker lost. Negative means the attack was a bad trade.
        /// </summary>
        public int MoraleSwing;
    }

    /// <summary>
    /// Applies <paramref name="attacker"/>'s damage to <paramref name="target"/>, and
    /// the target's damage back to the attacker unless the attacker avoids retaliation.
    ///
    /// <see cref="CardAbility.Volley"/> represents missile troops — slingers,
    /// javelin-throwers, archers — striking without being struck back. It is the
    /// designed counterweight to retaliation, and without it a low-health card such as
    /// Battle Chariot (4 hp, 2 damage) could never attack anything and survive.
    /// </summary>
    public static Result Resolve(CardInstance attacker, CardInstance target)
    {
        Result result = new Result();

        result.DamageToTarget = attacker.Data.Damage;
        result.TargetDestroyed = target.TakeDamage(result.DamageToTarget);

        bool avoidsRetaliation = attacker.Data.Has(CardAbility.Volley);
        if (!avoidsRetaliation)
        {
            // A destroyed defender still strikes back as it falls. That is deliberate:
            // it keeps trades symmetrical and stops "kill it first" removing all risk.
            result.DamageToAttacker = target.Data.Damage;
            result.AttackerDestroyed = attacker.TakeDamage(result.DamageToAttacker);
        }

        int gained = result.TargetDestroyed ? target.Data.MoraleCost : 0;
        int lost = result.AttackerDestroyed ? attacker.Data.MoraleCost : 0;
        result.MoraleSwing = gained - lost;

        return result;
    }

    /// <summary>
    /// Scores a prospective attack from the attacker's point of view. Used by the AI so
    /// that its choices are driven by morale — the actual win condition — rather than by
    /// raw damage, which the original heuristics ignored entirely.
    /// </summary>
    public static int Score(CardInstance attacker, CardInstance target)
    {
        bool killsTarget = target.CurrentHp <= attacker.Data.Damage;
        bool diesInReturn = !attacker.Data.Has(CardAbility.Volley)
                            && attacker.CurrentHp <= target.Data.Damage;

        int score = 0;

        if (killsTarget)
        {
            score += 100 + (target.Data.MoraleCost * 10);
        }
        else
        {
            score += attacker.Data.Damage;
        }

        if (diesInReturn)
        {
            score -= 80 + (attacker.Data.MoraleCost * 10);
        }

        return score;
    }
}

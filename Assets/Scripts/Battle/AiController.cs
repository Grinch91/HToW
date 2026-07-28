using System.Collections.Generic;

/// <summary>How hard the opponent plays.</summary>
public enum AiDifficulty
{
    /// <summary>Plays cheaply and only attacks when the trade is clearly favourable.</summary>
    Cautious,

    /// <summary>Plays the strongest card it can afford and takes any positive trade.</summary>
    Balanced,

    /// <summary>Accepts slightly unfavourable trades to keep pressure on.</summary>
    Ruthless
}

/// <summary>
/// Chooses the AI's plays and attacks.
///
/// Extracted from <c>Game</c> in Milestone 5. Plain C# so its decisions can be tested
/// directly — which matters, because an opponent's judgement is the hardest thing to
/// verify by looking at it.
///
/// The original AI was not playing the same game as the player. It scanned its *draw
/// pile* rather than its hand, never removed what it played, and so could replay one
/// card forever; its hand was filled every turn and read by nothing. That was fixed in
/// Milestone 2. What remains here is making its judgement good rather than merely legal.
///
/// Difficulty is expressed as thresholds on a shared scoring function rather than as
/// separate code paths, so every tier plays by identical rules and only its appetite
/// for risk changes.
/// </summary>
public class AiController
{
    /// <summary>Tuning for one difficulty tier.</summary>
    private struct Profile
    {
        /// <summary>
        /// The score an attack must reach before the AI will risk losing the attacker.
        /// Applies *only* when the attacker would die; a risk-free attack is always
        /// taken, at every difficulty, because declining free damage is never correct.
        /// </summary>
        public int MinScoreWhenRisking;

        /// <summary>Whether to reach for the strongest affordable card or the cheapest.</summary>
        public bool PlaysStrongest;
    }

    public AiDifficulty Difficulty { get; set; }

    public AiController(AiDifficulty difficulty = AiDifficulty.Balanced)
    {
        Difficulty = difficulty;
    }

    private Profile CurrentProfile
    {
        get
        {
            switch (Difficulty)
            {
                case AiDifficulty.Cautious:
                    // Will only lose a card for a clearly excellent trade.
                    return new Profile { MinScoreWhenRisking = 60, PlaysStrongest = false };

                case AiDifficulty.Ruthless:
                    // Will spend a card on a slightly losing trade to keep the pressure on.
                    return new Profile { MinScoreWhenRisking = -30, PlaysStrongest = true };

                default:
                    // Takes any trade that comes out ahead.
                    return new Profile { MinScoreWhenRisking = 1, PlaysStrongest = true };
            }
        }
    }

    /// <summary>
    /// Picks one card to play from hand, or null if nothing is affordable.
    /// </summary>
    public CardInstance ChoosePlay(IReadOnlyList<CardInstance> hand, int availableSupply)
    {
        Profile profile = CurrentProfile;
        CardInstance best = null;

        foreach (CardInstance card in hand)
        {
            if (card.Data.SupplyCost > availableSupply)
            {
                continue;
            }

            if (best == null)
            {
                best = card;
                continue;
            }

            bool better = profile.PlaysStrongest
                ? card.Data.Damage > best.Data.Damage
                : card.Data.SupplyCost < best.Data.SupplyCost;

            if (better)
            {
                best = card;
            }
        }

        return best;
    }

    /// <summary>
    /// Picks the best target for <paramref name="attacker"/>, or null to hold back.
    ///
    /// Scoring lives in <see cref="CombatResolver.Score"/>, so the AI judges a trade by
    /// exactly the rules the player is subject to, retaliation included. The original
    /// applied two ungraded heuristics, meaning the *last* matching enemy won rather
    /// than the best one — and it never considered MoraleCost, which is the actual win
    /// condition.
    /// </summary>
    public CardInstance ChooseTarget(CardInstance attacker, IReadOnlyList<CardInstance> enemies)
    {
        CardInstance best = null;
        int bestScore = int.MinValue;

        foreach (CardInstance candidate in enemies)
        {
            int score = CombatResolver.Score(attacker, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best == null)
        {
            return null;
        }

        // Difficulty governs appetite for risk, not for value. An attack the attacker
        // survives costs nothing, so declining it is never correct at any difficulty —
        // an earlier version applied a flat score threshold and had Cautious refusing
        // free chip damage, which read as broken rather than careful.
        bool attackerSurvives = attacker.Data.Has(CardAbility.Volley)
                                || attacker.CurrentHp > best.Data.Damage;

        if (attackerSurvives)
        {
            return bestScore > 0 ? best : null;
        }

        return bestScore >= CurrentProfile.MinScoreWhenRisking ? best : null;
    }
}

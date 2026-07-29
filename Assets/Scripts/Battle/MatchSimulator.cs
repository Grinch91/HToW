using System;
using System.Collections.Generic;

/// <summary>
/// Plays complete matches with no Unity involvement, so balance can be measured instead
/// of guessed at.
///
/// Card games are almost impossible to balance by inspection — the interactions between
/// a supply curve, a morale pool and retaliation are not something you can hold in your
/// head. Running ten thousand matches takes a second and answers the question directly.
///
/// It reuses <see cref="CombatResolver"/> and <see cref="AiController"/>, so combat maths
/// and decision-making are the real ones. The surrounding bookkeeping — drawing, paying
/// supply, morale on death — mirrors <c>Game</c> rather than sharing code with it, which
/// is a divergence risk worth being honest about: if the two ever disagree, the
/// simulator's numbers stop meaning anything. Unifying them behind one rules object is
/// the natural follow-up.
/// </summary>
public class MatchSimulator
{
    // Read from BaseCharacter rather than restated here. They were duplicated as consts
    // originally and immediately drifted: starting morale was changed in BaseCharacter
    // and the simulator carried on reporting figures for the old value, which made a
    // whole balance run meaningless. Derive, never restate.
    public static readonly int StartingMorale;
    public static readonly int StartingSupply;
    public static readonly int MaxSupply;

    public const int OpeningHand = 5;

    static MatchSimulator()
    {
        Player reference = new Player();
        StartingMorale = reference.playerMorale;
        StartingSupply = reference.playerSupply;
        MaxSupply = reference.MaxSupply;
    }

    /// <summary>The outcome of one match.</summary>
    public struct Result
    {
        public Side Winner;
        public bool Stalemate;
        public int Turns;
        public int WinnerMoraleRemaining;
    }

    /// <summary>Aggregate figures across many matches.</summary>
    public class Report
    {
        public int Matches;
        public int PlayerWins;
        public int AiWins;
        public int Stalemates;
        public long TotalTurns;

        public readonly Dictionary<string, int> Played = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Kills = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Deaths = new Dictionary<string, int>();

        public float PlayerWinRate => Matches == 0 ? 0f : (float)PlayerWins / Matches;
        public float AverageTurns => Matches == 0 ? 0f : (float)TotalTurns / Matches;

        public void Count(Dictionary<string, int> table, string key)
        {
            int current;
            table.TryGetValue(key, out current);
            table[key] = current + 1;
        }
    }

    private sealed class SideState
    {
        public Side Side;
        public List<CardInstance> Draw = new List<CardInstance>();
        public List<CardInstance> Hand = new List<CardInstance>();
        public List<CardInstance> Board = new List<CardInstance>();
        public List<CardInstance> Discard = new List<CardInstance>();
        public int Morale = StartingMorale;
        public int Supply = StartingSupply;
        public int SupplyCap = StartingSupply - 1;
        public AiController Brain;
    }

    private readonly Random random;
    private readonly Report report;

    public MatchSimulator(int seed, Report sharedReport = null)
    {
        random = new Random(seed);
        report = sharedReport ?? new Report();
    }

    public Report CurrentReport => report;

    /// <summary>
    /// Plays one match to a conclusion. Both sides are driven by the AI, which is what
    /// makes the comparison fair: neither deck benefits from a better pilot.
    /// </summary>
    public Result Play(
        List<CardInstance> playerCards,
        List<CardInstance> aiCards,
        AiDifficulty playerSkill = AiDifficulty.Balanced,
        AiDifficulty aiSkill = AiDifficulty.Balanced,
        int maxTurns = 200)
    {
        SideState player = new SideState
        {
            Side = Side.Player,
            Draw = Shuffled(playerCards),
            Brain = new AiController(playerSkill)
        };

        SideState enemy = new SideState
        {
            Side = Side.AI,
            Draw = Shuffled(aiCards),
            Brain = new AiController(aiSkill)
        };

        for (int i = 0; i < OpeningHand; i++)
        {
            DrawCard(player);
            DrawCard(enemy);
        }

        SideState active = random.Next(0, 2) == 0 ? player : enemy;
        SideState waiting = active == player ? enemy : player;

        int turns = 0;
        while (turns < maxTurns)
        {
            turns++;

            // Turn start.
            DrawCard(active);

            // Supply refills to a ceiling that grows by one per turn — see Game.GainSupply.
            active.SupplyCap = Math.Min(active.SupplyCap + 1, MaxSupply);
            active.Supply = active.SupplyCap;

            foreach (CardInstance card in active.Board)
            {
                card.HasAttacked = false;
            }

            // Main phase: play while anything is affordable.
            while (true)
            {
                CardInstance choice = active.Brain.ChoosePlay(active.Hand, active.Supply);
                if (choice == null)
                {
                    break;
                }

                active.Supply -= choice.Data.SupplyCost;
                active.Hand.Remove(choice);
                active.Board.Add(choice);
                report.Count(report.Played, choice.Data.DisplayName);
            }

            // Attack phase.
            foreach (CardInstance attacker in new List<CardInstance>(active.Board))
            {
                if (attacker.HasAttacked || !attacker.IsAlive)
                {
                    continue;
                }

                // With no defenders left, units strike the commander directly. This is
                // the game's second win condition and it has to be modelled here, or the
                // balance figures describe a game nobody is playing.
                if (waiting.Board.Count == 0)
                {
                    waiting.Morale -= attacker.Data.Damage;
                    attacker.HasAttacked = true;

                    if (waiting.Morale <= 0)
                    {
                        break;
                    }

                    continue;
                }

                CardInstance target = active.Brain.ChooseTarget(attacker, waiting.Board);
                if (target == null)
                {
                    continue;
                }

                CombatResolver.Result outcome = CombatResolver.Resolve(attacker, target);
                attacker.HasAttacked = true;

                if (outcome.TargetDestroyed)
                {
                    Kill(waiting, target);
                    report.Count(report.Kills, attacker.Data.DisplayName);
                }

                if (outcome.AttackerDestroyed)
                {
                    Kill(active, attacker);
                    report.Count(report.Kills, target.Data.DisplayName);
                }

                if (player.Morale <= 0 || enemy.Morale <= 0)
                {
                    break;
                }
            }

            if (player.Morale <= 0 || enemy.Morale <= 0)
            {
                break;
            }

            SideState swap = active;
            active = waiting;
            waiting = swap;
        }

        report.Matches++;
        report.TotalTurns += turns;

        Result result = new Result { Turns = turns };

        if (player.Morale <= 0 && enemy.Morale <= 0)
        {
            // Retaliation regularly destroys both cards in an exchange, so both sides can
            // cross zero on the same attack. Whoever is less far past zero has held out
            // longer and takes it; a genuine tie is the only draw.
            if (player.Morale > enemy.Morale)
            {
                result.Winner = Side.Player;
                report.PlayerWins++;
            }
            else if (enemy.Morale > player.Morale)
            {
                result.Winner = Side.AI;
                report.AiWins++;
            }
            else
            {
                result.Stalemate = true;
                report.Stalemates++;
            }
        }
        else if (player.Morale <= 0)
        {
            result.Winner = Side.AI;
            result.WinnerMoraleRemaining = enemy.Morale;
            report.AiWins++;
        }
        else if (enemy.Morale <= 0)
        {
            result.Winner = Side.Player;
            result.WinnerMoraleRemaining = player.Morale;
            report.PlayerWins++;
        }
        else
        {
            result.Stalemate = true;
            report.Stalemates++;
        }

        return result;
    }

    private void Kill(SideState owner, CardInstance card)
    {
        owner.Board.Remove(card);
        owner.Morale -= card.Data.MoraleCost;
        card.Heal();
        owner.Discard.Add(card);
        report.Count(report.Deaths, card.Data.DisplayName);
    }

    private void DrawCard(SideState side)
    {
        if (side.Draw.Count == 0 && side.Discard.Count > 0)
        {
            side.Draw.AddRange(side.Discard);
            side.Discard.Clear();
            ShuffleInPlace(side.Draw);
        }

        if (side.Draw.Count == 0)
        {
            return;
        }

        CardInstance top = side.Draw[0];
        side.Draw.RemoveAt(0);
        side.Hand.Add(top);
    }

    private List<CardInstance> Shuffled(List<CardInstance> source)
    {
        List<CardInstance> copy = new List<CardInstance>(source);
        ShuffleInPlace(copy);
        return copy;
    }

    private void ShuffleInPlace(List<CardInstance> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            CardInstance swap = cards[i];
            cards[i] = cards[j];
            cards[j] = swap;
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// TurnController is the fix for the project's defining defect: Update() used to run a
/// full player turn and a full AI turn every frame, producing 716 turn cycles in about
/// twelve seconds of real play. See Docs/GameplayLoop.md section 3.
/// </summary>
public class TurnControllerTests
{
    static TurnController StartedMatch(Side first)
    {
        TurnController turns = new TurnController();
        turns.BeginMatch(first);
        Drain(turns);
        return turns;
    }

    /// <summary>Exactly what Game.Update() does each frame.</summary>
    static void Drain(TurnController turns)
    {
        while (turns.Advance())
        {
        }
    }

    // ---- The regression that defines Milestone 1 --------------------------------

    [Test]
    public void TenThousandFramesWithoutInputDoNotAdvanceTheTurn()
    {
        TurnController turns = StartedMatch(Side.Player);
        int startTurn = turns.TurnNumber;
        Side startSide = turns.ActiveSide;

        for (int frame = 0; frame < 10000; frame++)
        {
            Drain(turns);
        }

        Assert.AreEqual(startTurn, turns.TurnNumber, "turn advanced without input");
        Assert.AreEqual(startSide, turns.ActiveSide, "side changed without input");
        Assert.AreEqual(TurnPhase.Main, turns.Phase, "should park in Main awaiting input");
    }

    // ---- Normal progression -----------------------------------------------------

    [Test]
    public void RequestingEndOfTurnPassesControlToTheOpponent()
    {
        TurnController turns = StartedMatch(Side.Player);

        turns.RequestEndTurn();
        Drain(turns);

        Assert.AreEqual(Side.AI, turns.ActiveSide);
        Assert.AreEqual(2, turns.TurnNumber);
        Assert.AreEqual(TurnPhase.Main, turns.Phase);
    }

    [Test]
    public void SidesAlternateStrictly()
    {
        TurnController turns = StartedMatch(Side.AI);
        List<Side> order = new List<Side>();

        for (int i = 0; i < 6; i++)
        {
            order.Add(turns.ActiveSide);
            turns.RequestEndTurn();
            Drain(turns);
        }

        Assert.AreEqual(Side.AI, order[0], "the coin flip decides who acts first");
        for (int i = 1; i < order.Count; i++)
        {
            Assert.AreNotEqual(order[i - 1], order[i], "sides must alternate");
        }
    }

    [Test]
    public void RepeatedEndTurnRequestsStillEndOnlyOneTurn()
    {
        TurnController turns = StartedMatch(Side.Player);

        turns.RequestEndTurn();
        turns.RequestEndTurn();
        turns.RequestEndTurn();
        Drain(turns);

        Assert.AreEqual(2, turns.TurnNumber, "an impatient double-click must not skip a turn");
        Assert.AreEqual(Side.AI, turns.ActiveSide);
    }

    // ---- Events ------------------------------------------------------------------

    [Test]
    public void OneFullTurnRaisesStartEndStartInOrder()
    {
        TurnController turns = new TurnController();
        List<string> log = new List<string>();
        turns.TurnStarted += s => log.Add("start:" + s);
        turns.TurnEnded += s => log.Add("end:" + s);

        turns.BeginMatch(Side.Player);
        Drain(turns);
        turns.RequestEndTurn();
        Drain(turns);

        Assert.AreEqual(
            new[] { "start:Player", "end:Player", "start:AI" },
            log.ToArray());
    }

    // ---- Match end ---------------------------------------------------------------

    [Test]
    public void GameOverIsTerminal()
    {
        TurnController turns = StartedMatch(Side.Player);

        turns.EndMatch();
        for (int frame = 0; frame < 1000; frame++)
        {
            Drain(turns);
        }

        Assert.AreEqual(TurnPhase.GameOver, turns.Phase);
        Assert.AreEqual(1, turns.TurnNumber, "no turns may pass after the match ends");
        Assert.IsFalse(turns.MatchInProgress);
    }

    [Test]
    public void AWinDetectedDuringTurnEndStopsTheHandover()
    {
        TurnController turns = new TurnController();
        turns.TurnEnded += s => turns.EndMatch();   // stands in for a lethal attack

        turns.BeginMatch(Side.Player);
        Drain(turns);
        turns.RequestEndTurn();
        Drain(turns);

        Assert.AreEqual(TurnPhase.GameOver, turns.Phase);
        Assert.AreEqual(Side.Player, turns.ActiveSide, "the loser must not get another turn");
    }

    [Test]
    public void OpponentFlipsBothWays()
    {
        Assert.AreEqual(Side.AI, TurnController.Opponent(Side.Player));
        Assert.AreEqual(Side.Player, TurnController.Opponent(Side.AI));
    }
}

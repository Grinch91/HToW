using System;

/// <summary>
/// Which side of the board is acting.
/// </summary>
public enum Side
{
    Player,
    AI
}

/// <summary>
/// The phases a single turn moves through.
/// </summary>
public enum TurnPhase
{
    /// <summary>Before the match has begun. Nothing is drawn or played.</summary>
    Setup,

    /// <summary>Draw, gain supply, refresh units. Automatic; resolves immediately.</summary>
    TurnStart,

    /// <summary>The active side acts. This phase does NOT self-advance — it waits
    /// for <see cref="RequestEndTurn"/>. That wait is the entire point of this class.</summary>
    Main,

    /// <summary>Attacks resolve, then control passes to the other side.</summary>
    TurnEnd,

    /// <summary>The match is over. No further transitions occur.</summary>
    GameOver
}

/// <summary>
/// Owns turn order and turn phase for a battle.
///
/// Deliberately a plain C# class rather than a MonoBehaviour: it has no per-frame
/// behaviour of its own, needs no scene wiring, and can be unit tested without Unity.
///
/// This replaces the original design, in which <c>Game.Update()</c> ran a full player
/// turn and a full AI turn on every frame because <c>EndTurn()</c> reset the state back
/// to <c>Begin</c>. A recorded 2014 session executed 716 turn cycles in about twelve
/// seconds. See Docs/GameplayLoop.md section 3.
///
/// The fix is structural, not a patch: <see cref="TurnPhase.Main"/> is a state that only
/// an explicit request can leave, so turns cannot advance on their own no matter how
/// often <see cref="Advance"/> is called.
/// </summary>
public class TurnController
{
    /// <summary>The side currently taking its turn.</summary>
    public Side ActiveSide { get; private set; }

    /// <summary>The phase the active side's turn is in.</summary>
    public TurnPhase Phase { get; private set; }

    /// <summary>Increments once per turn taken, starting at 1. Both sides share the count.</summary>
    public int TurnNumber { get; private set; }

    /// <summary>True while the match is running and neither side has won.</summary>
    public bool MatchInProgress => Phase != TurnPhase.Setup && Phase != TurnPhase.GameOver;

    /// <summary>Raised at the start of a turn, before the active side may act.</summary>
    public event Action<Side> TurnStarted;

    /// <summary>Raised when a turn ends, before control passes to the other side.</summary>
    public event Action<Side> TurnEnded;

    private bool _endTurnRequested;

    public TurnController()
    {
        Phase = TurnPhase.Setup;
    }

    /// <summary>
    /// Begins the match with <paramref name="first"/> taking the opening turn.
    /// </summary>
    public void BeginMatch(Side first)
    {
        ActiveSide = first;
        TurnNumber = 1;
        Phase = TurnPhase.TurnStart;
        _endTurnRequested = false;
    }

    /// <summary>
    /// Asks to end the current turn. Called by the End Turn button for the player, and
    /// by the AI once it has finished acting.
    ///
    /// Ignored outside <see cref="TurnPhase.Main"/>, so a double-click or an AI that
    /// requests twice cannot skip a turn.
    /// </summary>
    public void RequestEndTurn()
    {
        if (Phase == TurnPhase.Main)
        {
            _endTurnRequested = true;
        }
    }

    /// <summary>
    /// Ends the match. No further phase transitions occur after this.
    /// </summary>
    public void EndMatch()
    {
        Phase = TurnPhase.GameOver;
    }

    /// <summary>
    /// Performs at most one phase transition and returns whether one occurred.
    ///
    /// Callers should drain this with <c>while (Advance()) { }</c>. That loop always
    /// terminates: <see cref="TurnPhase.Main"/> returns false unless an end-turn has
    /// been requested, and a request is consumed as it is read.
    /// </summary>
    public bool Advance()
    {
        switch (Phase)
        {
            case TurnPhase.TurnStart:
                Phase = TurnPhase.Main;
                TurnStarted?.Invoke(ActiveSide);
                return true;

            case TurnPhase.Main:
                if (!_endTurnRequested)
                {
                    return false;
                }
                _endTurnRequested = false;
                Phase = TurnPhase.TurnEnd;
                return true;

            case TurnPhase.TurnEnd:
                TurnEnded?.Invoke(ActiveSide);

                // GameOver may have been raised by a handler above (a unit died and
                // dropped a side's morale to zero). Do not hand the turn over if so.
                if (Phase == TurnPhase.GameOver)
                {
                    return false;
                }

                ActiveSide = Opponent(ActiveSide);
                TurnNumber++;
                Phase = TurnPhase.TurnStart;
                return true;

            default:
                return false;
        }
    }

    /// <summary>The side opposing <paramref name="side"/>.</summary>
    public static Side Opponent(Side side)
    {
        return side == Side.Player ? Side.AI : Side.Player;
    }
}

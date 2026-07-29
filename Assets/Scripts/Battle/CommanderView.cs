using UnityEngine;

/// <summary>
/// Makes a side's general clickable so units can strike at the commander directly when
/// no defenders remain.
///
/// The General objects have been sitting in Battle.unity since 2014 with artwork and no
/// code at all. They now carry the game's second win condition: if your opponent has
/// nothing on the board, your units hit their commander and take morale off directly.
///
/// Without this an empty board was a dead end — neither side could make progress, and a
/// match could only end if someone chose to trade into a defended board.
/// </summary>
public class CommanderView : MonoBehaviour
{
    /// <summary>Which side this commander belongs to.</summary>
    public Side Owner { get; private set; }

    private Game game;
    private SpriteRenderer spriteRenderer;
    private Color restingColour;

    public void Bind(Side owner, Game owningGame)
    {
        Owner = owner;
        game = owningGame;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            restingColour = spriteRenderer.color;
        }

        // Sized from the artwork so the whole portrait is clickable.
        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            collider.size = spriteRenderer.sprite.bounds.size;
        }
    }

    /// <summary>Marks the commander as a legal target for the selected attacker.</summary>
    public void SetTargetable(bool targetable)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = targetable
            ? new Color(1.0f, 0.75f, 0.75f)
            : restingColour;
    }

    private void OnMouseDown()
    {
        if (game != null)
        {
            game.OnCommanderClicked(this);
        }
    }
}

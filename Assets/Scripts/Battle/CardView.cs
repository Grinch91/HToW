using UnityEngine;

/// <summary>
/// The GameObject side of a card: sprite, collider, and click handling.
///
/// Replaces three scripts at once — <c>CardAttributes</c> (held the data),
/// <c>MouseController</c> (clicks in hand) and <c>BattleController</c> (clicks in
/// play). Splitting behaviour across two controllers by zone meant selection state
/// lived on individual cards but was read from their container, which is exactly why
/// combat threw a NullReferenceException on the first attack (bug C1).
///
/// A view now simply reports the click. Deciding what it means is the game's job.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class CardView : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Game game;

    /// <summary>The card this view shows. Null until <see cref="Bind"/> is called.</summary>
    public CardInstance Instance { get; private set; }

    /// <summary>
    /// Attaches this view to a card. <paramref name="faceDown"/> hides the face for the
    /// opponent's hand.
    /// </summary>
    public void Bind(CardInstance instance, Game owner, Sprite face, bool faceDown)
    {
        Instance = instance;
        game = owner;
        instance.View = this;

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = face;

        name = faceDown ? "Card (hidden)" : instance.Data.DisplayName;

        // Face-down cards are the opponent's hand and must not be clickable.
        GetComponent<BoxCollider2D>().enabled = !faceDown;
    }

    public void SetFace(Sprite face)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = face;
        }
    }

    /// <summary>Tints the card to show it is the current attacker or target.</summary>
    public void SetHighlight(Color colour)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = colour;
        }
    }

    private void OnMouseDown()
    {
        if (game != null && Instance != null)
        {
            game.OnCardClicked(this);
        }
    }
}

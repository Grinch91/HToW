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
    private TextMesh costLabel;
    private TextMesh damageLabel;
    private TextMesh healthLabel;

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

        if (!faceDown)
        {
            BuildStatLabels();
            RefreshStats();
        }
    }

    // Card art is a flat image with no stats drawn on it, so until Milestone 2 a card's
    // cost, damage and health were invisible to the player everywhere in the game. There
    // was no way to make an informed decision about anything. These labels are a
    // deliberately plain stand-in; the proper card frame (Resources/cardfront.png exists
    // and is unused) belongs with the UI pass.
    private void BuildStatLabels()
    {
        if (costLabel != null)
        {
            return;
        }

        costLabel = CreateLabel("Cost", new Vector3(-0.55f, 0.85f, -0.1f), TextAnchor.UpperLeft);
        damageLabel = CreateLabel("Damage", new Vector3(-0.55f, -0.85f, -0.1f), TextAnchor.LowerLeft);
        healthLabel = CreateLabel("Health", new Vector3(0.55f, -0.85f, -0.1f), TextAnchor.LowerRight);
    }

    private TextMesh CreateLabel(string labelName, Vector3 localPosition, TextAnchor anchor)
    {
        GameObject obj = new GameObject(labelName);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = Vector3.one * 0.35f;

        TextMesh text = obj.AddComponent<TextMesh>();
        text.anchor = anchor;
        text.alignment = TextAlignment.Center;
        text.fontSize = 48;
        text.characterSize = 0.2f;
        text.color = Color.white;

        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.sortingLayerID = spriteRenderer.sortingLayerID;
        renderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        return text;
    }

    /// <summary>Refreshes the printed stats. Called whenever the card takes damage.</summary>
    public void RefreshStats()
    {
        if (Instance == null || costLabel == null)
        {
            return;
        }

        costLabel.text = Instance.Data.SupplyCost.ToString();
        damageLabel.text = Instance.Data.Damage.ToString();
        healthLabel.text = Instance.CurrentHp.ToString();

        // Wounded cards read amber so damage is visible at a glance.
        healthLabel.color = Instance.CurrentHp < Instance.Data.Hp
            ? new Color(1.0f, 0.6f, 0.2f)
            : Color.white;
    }

    /// <summary>
    /// Turns a face-down card face up — used when the AI plays a card out of its hidden
    /// hand onto the board, where it becomes public information.
    /// </summary>
    public void Reveal()
    {
        if (Instance == null)
        {
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = Instance.Data.Art;
        }

        name = Instance.Data.DisplayName;
        GetComponent<BoxCollider2D>().enabled = true;

        BuildStatLabels();
        RefreshStats();
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

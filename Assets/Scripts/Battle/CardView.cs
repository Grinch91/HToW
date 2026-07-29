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

        BoxCollider2D collider = GetComponent<BoxCollider2D>();

        // Size the click target from the art rather than a magic number. It was a fixed
        // 2.0 x 3.0 against card art 5.12 x 7.44 units wide, so only the middle of a
        // card responded to clicks and the edges silently did nothing.
        if (face != null)
        {
            collider.size = face.bounds.size;
        }

        // Face-down cards are the opponent's hand and must not be clickable.
        collider.enabled = !faceDown;

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

    private Vector3 restingScale;
    private bool scaleCaptured;

    /// <summary>
    /// Marks the card as the current attacker or target. Colour alone proved hard to
    /// read against varied card art, so a selected card also lifts slightly and draws
    /// in front of its neighbours.
    /// </summary>
    public void SetHighlight(Color colour)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = colour;
        }

        if (!scaleCaptured)
        {
            restingScale = transform.localScale;
            scaleCaptured = true;
        }

        bool selected = colour != Color.white;
        transform.localScale = selected ? restingScale * 1.12f : restingScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = selected ? 100 : 0;
        }
    }

    private void OnMouseDown()
    {
        if (game != null && Instance != null)
        {
            game.OnCardClicked(this);
        }
    }

    //Combat feedback
    #region

    // Cards used to appear and vanish with no acknowledgement at all: an attack was a
    // silent number change and a death was a card blinking out of existence. Nothing
    // told the player that anything had happened, let alone what.

    /// <summary>Lunges toward a point and back. Purely cosmetic.</summary>
    public void PlayAttack(Vector3 worldTarget)
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(AttackRoutine(worldTarget));
        }
    }

    private System.Collections.IEnumerator AttackRoutine(Vector3 worldTarget)
    {
        Vector3 start = transform.position;
        Vector3 forward = Vector3.MoveTowards(start, worldTarget, 1.6f);
        forward.z = start.z;

        const float outDuration = 0.12f;
        const float backDuration = 0.16f;

        for (float t = 0f; t < outDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, forward, t / outDuration);
            yield return null;
        }

        for (float t = 0f; t < backDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(forward, start, t / backDuration);
            yield return null;
        }

        transform.position = start;
    }

    /// <summary>Flashes red and floats the damage figure upward.</summary>
    public void PlayHit(int damage)
    {
        RefreshStats();

        if (isActiveAndEnabled)
        {
            StartCoroutine(HitRoutine());
            SpawnFloatingNumber("-" + damage, new Color(1f, 0.35f, 0.30f));
        }
    }

    private System.Collections.IEnumerator HitRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        Color original = spriteRenderer.color;

        for (int flash = 0; flash < 2; flash++)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.4f);
            yield return new WaitForSeconds(0.06f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// Fades and shrinks the card away, then destroys it. The caller has already removed
    /// the model from its zone, so this object is purely a leftover visual.
    /// </summary>
    public void PlayDeathThenDestroy()
    {
        // Stop responding to clicks the moment it is dying.
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        if (isActiveAndEnabled)
        {
            StartCoroutine(DeathRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator DeathRoutine()
    {
        Vector3 startScale = transform.localScale;
        Color startColour = spriteRenderer != null ? spriteRenderer.color : Color.white;

        const float duration = 0.45f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;

            transform.localScale = Vector3.Lerp(startScale, startScale * 0.55f, k);
            transform.Rotate(0f, 0f, 220f * Time.deltaTime);

            if (spriteRenderer != null)
            {
                Color fading = Color.Lerp(startColour, new Color(0.4f, 0.1f, 0.1f, 0f), k);
                spriteRenderer.color = fading;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Dims a card that has already attacked this turn, so it is obvious at a glance
    /// which units still have an action available.
    /// </summary>
    public void SetSpent(bool spent)
    {
        if (spriteRenderer == null || Instance == null)
        {
            return;
        }

        spriteRenderer.color = spent ? new Color(0.55f, 0.55f, 0.60f) : Color.white;
    }

    private void SpawnFloatingNumber(string text, Color colour)
    {
        GameObject obj = new GameObject("Damage");
        obj.transform.position = transform.position + new Vector3(0f, 1.0f, -1f);

        TextMesh mesh = obj.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.color = colour;
        mesh.fontSize = 90;
        mesh.characterSize = 0.14f;
        mesh.anchor = TextAnchor.MiddleCenter;

        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null && spriteRenderer != null)
        {
            renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            renderer.sortingOrder = 200;
        }

        FloatingNumber floater = obj.AddComponent<FloatingNumber>();
        floater.Begin(mesh);
    }
    #endregion
}

/// <summary>Drifts a damage figure upward, fades it, then removes itself.</summary>
public class FloatingNumber : MonoBehaviour
{
    private TextMesh mesh;

    public void Begin(TextMesh target)
    {
        mesh = target;
        StartCoroutine(Run());
    }

    private System.Collections.IEnumerator Run()
    {
        const float duration = 0.9f;
        Color start = mesh.color;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.position += new Vector3(0f, 1.6f * Time.deltaTime, 0f);
            mesh.color = new Color(start.r, start.g, start.b, 1f - (t / duration));
            yield return null;
        }

        Destroy(gameObject);
    }
}

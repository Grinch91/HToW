using UnityEngine;

/// <summary>
/// The GameObject side of a card.
///
/// A card is composed rather than drawn: the illustration sits at the back, a generated
/// faction frame overlays it, and the stats sit on top in fixed positions. Nothing here
/// is loaded from an art file — the frames, badges and ring all come from
/// <see cref="ProceduralArt"/>, which is what makes the visual identity free to produce
/// and instant to recolour.
///
/// Replaces three 2014 scripts at once: CardAttributes held the data, MouseController
/// handled clicks in hand and BattleController handled clicks in play. Splitting
/// behaviour by zone is what put selection state on cards but read it from their
/// container, which threw on the first attack.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class CardView : MonoBehaviour
{
    // Layout positions in card-local units. The card is 5.12 x 7.44, centred on origin,
    // so these track the regions cut into the frame texture.
    private static readonly Vector3 CostAt = new Vector3(-2.02f, 3.02f, -0.10f);
    private static readonly Vector3 MoraleAt = new Vector3(2.02f, 2.92f, -0.10f);
    private static readonly Vector3 StrikeAt = new Vector3(-1.72f, -2.86f, -0.10f);
    private static readonly Vector3 StandAt = new Vector3(1.72f, -2.86f, -0.10f);
    private const float NameY = -1.93f;
    private const float KeywordY = -2.32f;

    private SpriteRenderer portrait;
    private SpriteRenderer frame;
    private SpriteRenderer readyRing;
    private Game game;

    private TextMesh costText, moraleText, strikeText, standText, nameText, keywordText;
    private Vector3 restingScale;
    private bool scaleCaptured;
    private bool built;

    /// <summary>The card this view shows. Null until <see cref="Bind"/> is called.</summary>
    public CardInstance Instance { get; private set; }

    /// <summary>
    /// Attaches this view to a card.
    ///
    /// The frame is the root renderer and the illustration is a child sitting inside its
    /// window. The first version had that the other way round, which meant the source
    /// artwork's own border, name banner and printed stats showed through — a card
    /// drawn inside a card, with every label duplicated.
    /// </summary>
    public void Bind(CardInstance instance, Game owner, bool faceDown)
    {
        Instance = instance;
        game = owner;
        instance.View = this;

        Faction faction = instance.Data.Faction;

        frame = GetComponent<SpriteRenderer>();
        frame.sprite = faceDown
            ? ProceduralArt.CardBack(faction)
            : ProceduralArt.CardFrame(faction);
        frame.sortingOrder = 1;

        name = faceDown ? "Card (hidden)" : instance.Data.DisplayName;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();

        // Size the click target from the frame rather than a magic number. It was a fixed
        // 2.0 x 3.0 against a card 5.12 x 7.44 units wide, so only the middle of a card
        // responded to clicks and the edges silently did nothing.
        collider.size = frame.sprite.bounds.size;
        collider.enabled = !faceDown;

        if (!faceDown)
        {
            BuildFace();
            RefreshStats();
        }
    }

    /// <summary>Turns a face-down card face up, used when the AI plays from its hidden hand.</summary>
    public void Reveal()
    {
        if (Instance == null)
        {
            return;
        }

        if (frame != null)
        {
            frame.sprite = ProceduralArt.CardFrame(Instance.Data.Faction);
        }

        name = Instance.Data.DisplayName;
        GetComponent<BoxCollider2D>().enabled = true;

        BuildFace();
        RefreshStats();
    }

    //Composition
    #region

    private void BuildFace()
    {
        if (built)
        {
            return;
        }

        built = true;

        Faction faction = Instance.Data.Faction;
        const int baseOrder = 0;

        readyRing = AddLayer("ReadyRing", ProceduralArt.ReadyRing(), new Vector3(0f, 0f, 0.2f), baseOrder - 2);
        readyRing.enabled = false;

        // Sits in the frame's window: the transparent region runs y 216..704 of a
        // 744-tall texture, whose centre is 0.88 units above the card's own centre.
        portrait = AddLayer("Portrait", ProceduralArt.Portrait(Instance.Data.Art),
            new Vector3(0f, 0.88f, 0.1f), baseOrder);

        AddLayer("Cost", ProceduralArt.CostBadge(faction), CostAt, baseOrder + 2);
        AddLayer("Morale", ProceduralArt.MoraleBanner(), MoraleAt, baseOrder + 2);
        AddLayer("Strike", ProceduralArt.StatPlate(faction), StrikeAt, baseOrder + 2);
        AddLayer("Stand", ProceduralArt.StatPlate(faction), StandAt, baseOrder + 2);

        costText = AddText("CostText", CostAt + (Vector3.back * 0.05f), 0.085f, Palette.BogOak, baseOrder + 3);
        moraleText = AddText("MoraleText", MoraleAt + new Vector3(0f, 0.12f, -0.05f), 0.075f, Palette.Vellum, baseOrder + 3);
        strikeText = AddText("StrikeText", StrikeAt + (Vector3.back * 0.05f), 0.075f, Palette.Ink, baseOrder + 3);
        standText = AddText("StandText", StandAt + (Vector3.back * 0.05f), 0.075f, Palette.Ink, baseOrder + 3);

        nameText = AddText("NameText", new Vector3(0f, NameY, -0.15f), 0.055f, Palette.Vellum, baseOrder + 3);
        nameText.text = Instance.Data.DisplayName;

        keywordText = AddText("KeywordText", new Vector3(0f, KeywordY, -0.15f), 0.032f, Palette.InkSoft, baseOrder + 3);
        keywordText.text = KeywordLine(Instance.Data);
    }

    private SpriteRenderer AddLayer(string layerName, Sprite sprite, Vector3 localPosition, int order)
    {
        GameObject obj = new GameObject(layerName);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = localPosition;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = frame != null ? frame.sortingLayerID : 0;
        renderer.sortingOrder = order;
        return renderer;
    }

    private TextMesh AddText(string textName, Vector3 localPosition, float size, Color colour, int order)
    {
        GameObject obj = new GameObject(textName);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = localPosition;

        TextMesh mesh = obj.AddComponent<TextMesh>();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 72;
        mesh.characterSize = size;
        mesh.color = colour;

        Font display = Resources.Load<Font>("carolingia");
        if (display != null)
        {
            mesh.font = display;
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = display.material;
        }

        MeshRenderer r = obj.GetComponent<MeshRenderer>();
        r.sortingLayerID = frame != null ? frame.sortingLayerID : 0;
        r.sortingOrder = order;
        return mesh;
    }

    // Keyword plus the honesty mark. Distinguishing recorded history from saga and from
    // invention is a stated goal of the project, and putting it on the card costs nothing.
    private static string KeywordLine(CardData data)
    {
        string keyword = "";
        foreach (CardAbility ability in data.Abilities)
        {
            if (ability != CardAbility.None)
            {
                keyword += ability.ToString().ToUpperInvariant() + " ";
            }
        }

        string mark;
        switch (data.Historicity)
        {
            case Historicity.Mythological: mark = "MYTH"; break;
            case Historicity.Fictional: mark = "INVENTED"; break;
            default: mark = "FACT"; break;
        }

        return keyword.Length > 0 ? keyword.Trim() + "  ·  " + mark : mark;
    }
    #endregion

    /// <summary>Refreshes the printed stats. Called whenever the card takes damage.</summary>
    public void RefreshStats()
    {
        if (Instance == null || costText == null)
        {
            return;
        }

        costText.text = Instance.Data.SupplyCost.ToString();
        moraleText.text = Instance.Data.MoraleCost.ToString();
        strikeText.text = Instance.Data.Damage.ToString();
        standText.text = Instance.CurrentHp.ToString();

        // A wounded unit reads in madder — the colour reserved for loss.
        standText.color = Instance.CurrentHp < Instance.Data.Hp ? Palette.Madder : Palette.Ink;
    }

    /// <summary>
    /// Marks the card as the current attacker or target. Colour alone proved hard to read
    /// against varied card art, so a selected card also lifts and draws in front.
    /// </summary>
    public void SetHighlight(Color colour)
    {
        if (!scaleCaptured)
        {
            restingScale = transform.localScale;
            scaleCaptured = true;
        }

        bool selected = colour != Color.white;

        if (portrait != null) portrait.color = colour;
        if (frame != null)
        {
            frame.color = colour;
            // Lift the whole card above its neighbours, not just the illustration.
            frame.sortingOrder = selected ? 101 : 1;
        }

        transform.localScale = selected ? restingScale * 1.12f : restingScale;
    }

    /// <summary>
    /// Shows the gold ring when this unit still has an attack available.
    ///
    /// Dimming a spent card only tells the player what they have already done. The ring
    /// tells them what they can still do, which is the question they actually have.
    /// </summary>
    public void SetReady(bool ready)
    {
        if (readyRing != null)
        {
            readyRing.enabled = ready;
        }
    }

    /// <summary>Dims a card that has already attacked this turn.</summary>
    public void SetSpent(bool spent)
    {
        Color tint = spent ? new Color(0.55f, 0.55f, 0.60f) : Color.white;

        if (portrait != null) portrait.color = tint;
        if (frame != null) frame.color = tint;
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
    // silent number change and a death was a card blinking out of existence.

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

    /// <summary>Flashes and floats the damage figure upward.</summary>
    public void PlayHit(int damage)
    {
        RefreshStats();

        if (isActiveAndEnabled)
        {
            StartCoroutine(HitRoutine());
            SpawnFloatingNumber("-" + damage, Palette.Madder);
        }
    }

    private System.Collections.IEnumerator HitRoutine()
    {
        if (portrait == null)
        {
            yield break;
        }

        Color original = portrait.color;

        for (int flash = 0; flash < 2; flash++)
        {
            portrait.color = Palette.Madder;
            yield return new WaitForSeconds(0.06f);
            portrait.color = original;
            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// Fades and shrinks the card away, then destroys it. The caller has already removed
    /// the model from its zone, so this object is purely a leftover visual.
    /// </summary>
    public void PlayDeathThenDestroy()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        if (readyRing != null)
        {
            readyRing.enabled = false;
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
        const float duration = 0.45f;

        SpriteRenderer[] layers = GetComponentsInChildren<SpriteRenderer>();
        TextMesh[] texts = GetComponentsInChildren<TextMesh>();

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;

            transform.localScale = Vector3.Lerp(startScale, startScale * 0.55f, k);
            transform.Rotate(0f, 0f, 220f * Time.deltaTime);

            foreach (SpriteRenderer layer in layers)
            {
                Color c = layer.color;
                c.a = 1f - k;
                layer.color = c;
            }

            foreach (TextMesh text in texts)
            {
                Color c = text.color;
                c.a = 1f - k;
                text.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
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
        if (renderer != null && portrait != null)
        {
            renderer.sortingLayerID = portrait.sortingLayerID;
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

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The front end, built in uGUI at runtime.
///
/// Replaces the immediate-mode OnGUI main menu, which had been the front end since 2014.
/// OnGUI allocated every frame, laid out against hardcoded pixel coordinates, and did
/// not scale — at 720p two of its three buttons were off the bottom of the screen.
///
/// The hierarchy is built in code rather than authored in the scene so there is no scene
/// surgery to review and no prefab to keep in sync: a CanvasScaler set to a 1920x1080
/// reference resolution does the scaling that the old menu could not.
///
/// Styling comes from <see cref="Palette"/> and <see cref="ProceduralArt"/>, so the front
/// end and the board share one visual language rather than looking like two projects.
/// </summary>
[RequireComponent(typeof(UI))]
public class MenuUI : MonoBehaviour
{
    [Tooltip("Full-screen backdrop. Falls back to a flat ground colour if unset.")]
    public Texture2D backdrop;

    [Tooltip("Title artwork shown above the buttons.")]
    public Texture2D banner;

    private UI deckBuilder;
    private Canvas canvas;
    private GameObject mainPanel;
    private GameObject battlePanel;
    private Text deckSummary;

    private Font display;

    private void Awake()
    {
        deckBuilder = GetComponent<UI>();
        display = Resources.Load<Font>("carolingia");

        Build();
        ShowMain();
    }

    //Construction
    #region

    private void Build()
    {
        GameObject canvasObject = new GameObject("MenuCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // Halfway between matching width and height, so the layout survives both
        // ultrawide and 4:3 without cropping the banner or crowding the buttons.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        BuildBackdrop(canvasObject.transform);
        mainPanel = BuildMainPanel(canvasObject.transform);
        battlePanel = BuildBattlePanel(canvasObject.transform);
    }

    // uGUI silently does nothing without one of these, and the failure looks exactly
    // like a broken button.
    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        GameObject events = new GameObject("EventSystem");
        events.AddComponent<UnityEngine.EventSystems.EventSystem>();
        events.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private void BuildBackdrop(Transform parent)
    {
        Image art = NewImage("Backdrop", parent, Palette.BogOak);
        Stretch(art.rectTransform);

        if (backdrop != null)
        {
            art.sprite = Sprite.Create(backdrop,
                new Rect(0, 0, backdrop.width, backdrop.height), new Vector2(0.5f, 0.5f));
            art.color = Color.white;
            art.preserveAspect = false;
        }

        // A wash so the artwork never competes with the type.
        Image wash = NewImage("Wash", parent, new Color(0f, 0f, 0f, 0.42f));
        Stretch(wash.rectTransform);
    }

    private GameObject BuildMainPanel(Transform parent)
    {
        GameObject panel = new GameObject("MainMenu", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Stretch((RectTransform)panel.transform);

        BuildBanner(panel.transform);

        MakeButton(panel.transform, "Battle Mode", 0, Palette.Verdigris, () => ShowBattle());
        MakeButton(panel.transform, "Cards", 1, Palette.Orpiment, () =>
        {
            Hide();
            deckBuilder.OpenDeckBuilder();
        });
        MakeButton(panel.transform, "Exit", 2, Palette.Iron, Application.Quit);

        return panel;
    }

    private GameObject BuildBattlePanel(Transform parent)
    {
        GameObject panel = new GameObject("BattleMenu", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        Stretch((RectTransform)panel.transform);

        BuildBanner(panel.transform);

        deckSummary = NewText("DeckSummary", panel.transform, "", 30, Palette.Vellum);
        RectTransform summaryRect = deckSummary.rectTransform;
        summaryRect.anchorMin = new Vector2(0.5f, 0f);
        summaryRect.anchorMax = new Vector2(0.5f, 0f);
        summaryRect.pivot = new Vector2(0.5f, 0f);
        summaryRect.anchoredPosition = new Vector2(0f, 430f);
        summaryRect.sizeDelta = new Vector2(900f, 44f);

        MakeButton(panel.transform, "Load Battle", 0, Palette.Madder, () => SceneManager.LoadScene("Battle"));
        MakeButton(panel.transform, "Main Menu", 1, Palette.Iron, () => ShowMain());

        return panel;
    }

    private void BuildBanner(Transform parent)
    {
        if (banner == null)
        {
            Text fallback = NewText("Title", parent, "Hibernia", 96, Palette.Orpiment);
            RectTransform t = fallback.rectTransform;
            t.anchorMin = new Vector2(0.5f, 1f);
            t.anchorMax = new Vector2(0.5f, 1f);
            t.pivot = new Vector2(0.5f, 1f);
            t.anchoredPosition = new Vector2(0f, -110f);
            t.sizeDelta = new Vector2(1100f, 140f);
            return;
        }

        Image art = NewImage("Banner", parent, Color.white);
        art.sprite = Sprite.Create(banner,
            new Rect(0, 0, banner.width, banner.height), new Vector2(0.5f, 0.5f));
        art.preserveAspect = true;

        RectTransform rect = art.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -60f);
        rect.sizeDelta = new Vector2(900f, 460f);
    }

    /// <summary>
    /// One menu button. Buttons stack upward from the bottom, so the column always fits
    /// however short the window is — the failure the old menu had.
    /// </summary>
    private void MakeButton(Transform parent, string label, int index, Color accent, UnityEngine.Events.UnityAction onClick)
    {
        const float height = 78f;
        const float gap = 22f;

        Image plate = NewImage("Button " + label, parent, new Color(0.10f, 0.08f, 0.05f, 0.94f));
        RectTransform rect = plate.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(420f, height);
        rect.anchoredPosition = new Vector2(0f, 110f + (index * (height + gap)));

        // A pigment rule down the leading edge carries the accent without the button
        // itself becoming a block of colour.
        Image rule = NewImage("Rule", plate.transform, accent);
        RectTransform ruleRect = rule.rectTransform;
        ruleRect.anchorMin = new Vector2(0f, 0f);
        ruleRect.anchorMax = new Vector2(0f, 1f);
        ruleRect.pivot = new Vector2(0f, 0.5f);
        ruleRect.sizeDelta = new Vector2(7f, 0f);
        ruleRect.anchoredPosition = Vector2.zero;

        Text text = NewText("Label", plate.transform, label, 34, Palette.Vellum);
        Stretch(text.rectTransform);
        text.alignment = TextAnchor.MiddleCenter;

        Button button = plate.gameObject.AddComponent<Button>();
        button.targetGraphic = plate;

        ColorBlock colours = button.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1.35f, 1.3f, 1.2f);
        colours.pressedColor = new Color(0.8f, 0.75f, 0.68f);
        colours.selectedColor = Color.white;
        colours.fadeDuration = 0.08f;
        button.colors = colours;

        button.onClick.AddListener(onClick);
    }
    #endregion

    //Screens
    #region

    private void ShowMain()
    {
        mainPanel.SetActive(true);
        battlePanel.SetActive(false);
        canvas.enabled = true;
    }

    private void ShowBattle()
    {
        SavedDeck selected = SaveSystem.Profile.SelectedDeck;
        deckSummary.text = selected != null
            ? "Taking " + selected.deckName + " into battle  ·  " + selected.CardCount
              + " cards  ·  " + selected.TotalMoraleCost + " morale"
            : "No deck selected — the default Celtic deck will be used.";

        mainPanel.SetActive(false);
        battlePanel.SetActive(true);
        canvas.enabled = true;
    }

    private void Hide()
    {
        canvas.enabled = false;
    }

    /// <summary>Called by the deckbuilder when it closes.</summary>
    public void ReturnToMenu()
    {
        ShowMain();
    }
    #endregion

    //Helpers
    #region

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image NewImage(string imageName, Transform parent, Color colour)
    {
        GameObject obj = new GameObject(imageName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = colour;
        return image;
    }

    private Text NewText(string textName, Transform parent, string value, int size, Color colour)
    {
        GameObject obj = new GameObject(textName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.fontSize = size;
        text.color = colour;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        if (display != null)
        {
            text.font = display;
        }

        return text;
    }
    #endregion
}

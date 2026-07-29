using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// The main menu, and the deckbuilder reached from it.
///
/// Still immediate-mode OnGUI with hardcoded coordinates, which is how this menu has
/// always worked. It is the wrong long-term technology — it allocates every frame, does
/// not scale across resolutions, and is effectively deprecated — but rebuilding the
/// front end in uGUI is a self-contained job for the UI pass, and doing it here would
/// have meant shipping the deckbuilder later for no gameplay gain.
/// See Docs/TechnicalDebt.md section 8.
/// </summary>
public class UI : MonoBehaviour
{
    private delegate void GUIMethod();
    private GUIMethod currentGUIMethod;

    public GUIStyle mystyle;
    public Texture btn;
    public Texture banner;

    [Tooltip("Full-screen menu backdrop. The 2014 scene showed this through a GUITexture " +
             "component, which Unity removed in 2018 — the upgrade stripped it silently, " +
             "leaving the menus on the camera's flat blue clear colour.")]
    public Texture background;

    private Vector2 collectionScroll;
    private Vector2 deckScroll;
    private SavedDeck editingDeck;
    private DeckValidator.Result validation;
    private string statusMessage = "";

    void Start()
    {
        this.currentGUIMethod = MainMenu;
    }

    void OnGUI()
    {
        this.currentGUIMethod();
    }

    //Layout helpers
    #region

    // The menu used to place its buttons at hardcoded y = 610 / 690 / 770, authored
    // against a window roughly 900px tall. Below that, "Cards" was clipped and "Exit"
    // was off-screen entirely, so at 720p the game looked like it had failed to start.
    // Predicted in Docs/UnityUpgrade.md section 3.1 and confirmed on the first build.
    //
    // Everything is now derived from Screen.width/height, so the menu fits any window.
    private const float ButtonWidth = 260f;
    private const float ButtonHeight = 58f;
    private const float ButtonGap = 22f;

    private static Rect ButtonSlot(int index, int count)
    {
        float stackHeight = (count * ButtonHeight) + ((count - 1) * ButtonGap);
        float bottomMargin = Mathf.Max(40f, Screen.height * 0.07f);
        float top = Screen.height - stackHeight - bottomMargin;

        return new Rect(
            (Screen.width - ButtonWidth) * 0.5f,
            top + (index * (ButtonHeight + ButtonGap)),
            ButtonWidth,
            ButtonHeight);
    }

    private GUIStyle centredButtonStyle;

    private bool MenuButton(int index, int count, string label)
    {
        Rect slot = ButtonSlot(index, count);

        if (btn != null)
        {
            GUI.DrawTexture(slot, btn, ScaleMode.StretchToFill);
        }

        // The authored style left-aligns its text, which reads as misaligned now that
        // the button plate is centred and full width. Copy it once and centre the label.
        if (centredButtonStyle == null)
        {
            centredButtonStyle = new GUIStyle(mystyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        return GUI.Button(
            new Rect(slot.x + 16, slot.y + 8, slot.width - 32, slot.height - 16),
            label,
            centredButtonStyle);
    }

    // Drawn before anything else, cropped to fill rather than stretched, so the artwork
    // keeps its proportions at any window shape.
    private void DrawBackground()
    {
        if (background == null)
        {
            return;
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), background, ScaleMode.ScaleAndCrop);
    }

    // Scaled to fit rather than drawn at a fixed offset, so it never overlaps the
    // buttons or spills off a narrow window.
    private void DrawBanner()
    {
        if (banner == null)
        {
            return;
        }

        float maxWidth = Screen.width * 0.72f;
        float maxHeight = Screen.height * 0.48f;

        float aspect = (float)banner.width / banner.height;
        float width = maxWidth;
        float height = width / aspect;

        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        GUI.DrawTexture(
            new Rect((Screen.width - width) * 0.5f, Screen.height * 0.05f, width, height),
            banner,
            ScaleMode.ScaleToFit);
    }

    private static void CentredLabel(float normalisedY, string text)
    {
        GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height * normalisedY, 600f, 30f),
            text, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
    }
    #endregion

    //Main menu
    #region
    public void MainMenu()
    {
        DrawBackground();
        DrawBanner();

        if (MenuButton(0, 3, "Battle Mode"))
        {
            this.currentGUIMethod = BattleMenu;
        }

        if (MenuButton(1, 3, "Cards"))
        {
            OpenDeckBuilder();
        }

        if (MenuButton(2, 3, "Exit"))
        {
            Application.Quit();
        }
    }

    public void BattleMenu()
    {
        DrawBackground();
        DrawBanner();

        SavedDeck selected = SaveSystem.Profile.SelectedDeck;
        CentredLabel(0.60f, selected != null
            ? "Taking " + selected.deckName + " into battle (" + selected.CardCount
              + " cards, " + selected.TotalMoraleCost + " morale)"
            : "No deck selected — the default Celtic deck will be used.");

        if (MenuButton(0, 2, "Load Battle"))
        {
            SceneManager.LoadScene("Battle");
        }

        if (MenuButton(1, 2, "Main Menu"))
        {
            this.currentGUIMethod = MainMenu;
        }
    }
    #endregion

    //Deckbuilder
    #region

    // The "Cards" button has shown "Sorry N/A" since 2014. The 2014 project also had a
    // DeckData asset named "Custom", so this screen was always intended.
    private void OpenDeckBuilder()
    {
        PlayerProfile profile = SaveSystem.Profile;
        editingDeck = profile.SelectedDeck;

        if (editingDeck == null)
        {
            editingDeck = new SavedDeck("New Deck");
            profile.decks.Add(editingDeck);
            profile.selectedDeckName = editingDeck.deckName;
        }

        Revalidate();
        statusMessage = "";
        this.currentGUIMethod = DeckBuilder;
    }

    private void Revalidate()
    {
        validation = DeckValidator.Validate(editingDeck, new Player().playerMorale);
    }

    // ---- Styling -----------------------------------------------------------------
    //
    // The deckbuilder was drawn with Unity's default GUI skin, which looked like a debug
    // window bolted onto a game that has an illuminated-manuscript main menu. These
    // styles are built once, in code, from flat colour swatches plus the project's own
    // insular display font (Resources/carolingia.ttf) — no new art required.

    private sealed class Palette
    {
        public static readonly Color Parchment = new Color(0.94f, 0.89f, 0.78f);
        public static readonly Color Muted = new Color(0.72f, 0.66f, 0.55f);
        public static readonly Color Gold = new Color(0.88f, 0.72f, 0.34f);
        public static readonly Color Good = new Color(0.60f, 0.83f, 0.52f);
        public static readonly Color Bad = new Color(0.92f, 0.47f, 0.40f);

        public static readonly Color Panel = new Color(0.09f, 0.06f, 0.04f, 0.90f);
        public static readonly Color Header = new Color(0.24f, 0.16f, 0.08f, 0.95f);
        public static readonly Color Row = new Color(1f, 1f, 1f, 0.045f);
        public static readonly Color Button = new Color(0.30f, 0.20f, 0.10f, 0.95f);
        public static readonly Color ButtonHover = new Color(0.44f, 0.30f, 0.15f, 0.98f);
    }

    private bool stylesBuilt;
    private Texture2D dimWash;
    private GUIStyle panelStyle, headerStyle, titleStyle, rowStyle;
    private GUIStyle cardNameStyle, cardStatStyle, tagStyle, footerStyle, fieldStyle;
    private GUIStyle actionStyle, tabStyle, tabActiveStyle;

    private static Texture2D Swatch(Color colour)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, colour);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private void BuildStyles()
    {
        if (stylesBuilt)
        {
            return;
        }

        stylesBuilt = true;

        Font display = Resources.Load<Font>("carolingia");

        Texture2D panelTex = Swatch(Palette.Panel);
        Texture2D headerTex = Swatch(Palette.Header);
        Texture2D rowTex = Swatch(Palette.Row);
        Texture2D buttonTex = Swatch(Palette.Button);
        Texture2D buttonHoverTex = Swatch(Palette.ButtonHover);
        dimWash = Swatch(new Color(0f, 0f, 0f, 0.45f));

        panelStyle = new GUIStyle();
        panelStyle.normal.background = panelTex;

        headerStyle = new GUIStyle();
        headerStyle.normal.background = headerTex;
        headerStyle.normal.textColor = Palette.Gold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontSize = 20;
        if (display != null) headerStyle.font = display;

        titleStyle = new GUIStyle();
        titleStyle.normal.textColor = Palette.Gold;
        titleStyle.fontSize = 34;
        titleStyle.alignment = TextAnchor.MiddleLeft;
        if (display != null) titleStyle.font = display;

        rowStyle = new GUIStyle();
        rowStyle.normal.background = rowTex;

        cardNameStyle = new GUIStyle();
        cardNameStyle.normal.textColor = Palette.Parchment;
        cardNameStyle.fontSize = 17;
        cardNameStyle.alignment = TextAnchor.MiddleLeft;
        if (display != null) cardNameStyle.font = display;

        cardStatStyle = new GUIStyle();
        cardStatStyle.normal.textColor = Palette.Muted;
        cardStatStyle.fontSize = 13;
        cardStatStyle.alignment = TextAnchor.MiddleLeft;

        tagStyle = new GUIStyle();
        tagStyle.fontSize = 12;
        tagStyle.alignment = TextAnchor.MiddleRight;

        footerStyle = new GUIStyle();
        footerStyle.fontSize = 14;
        footerStyle.wordWrap = true;
        footerStyle.alignment = TextAnchor.UpperLeft;
        footerStyle.normal.textColor = Palette.Parchment;

        actionStyle = new GUIStyle();
        actionStyle.normal.background = buttonTex;
        actionStyle.hover.background = buttonHoverTex;
        actionStyle.active.background = buttonHoverTex;
        actionStyle.normal.textColor = Palette.Parchment;
        actionStyle.hover.textColor = Color.white;
        actionStyle.alignment = TextAnchor.MiddleCenter;
        actionStyle.fontSize = 14;

        tabStyle = new GUIStyle(actionStyle);
        tabStyle.fontSize = 16;
        if (display != null) tabStyle.font = display;

        tabActiveStyle = new GUIStyle(tabStyle);
        tabActiveStyle.normal.background = buttonHoverTex;
        tabActiveStyle.normal.textColor = Palette.Gold;

        fieldStyle = new GUIStyle(GUI.skin.textField);
        fieldStyle.normal.textColor = Palette.Parchment;
        fieldStyle.fontSize = 15;
    }

    private static Color FactionColour(Faction faction)
    {
        switch (faction)
        {
            case Faction.Celtic: return new Color(0.58f, 0.83f, 0.52f);
            case Faction.Norse: return new Color(0.56f, 0.74f, 0.92f);
            default: return new Color(0.90f, 0.77f, 0.45f);
        }
    }

    public void DeckBuilder()
    {
        BuildStyles();

        const int margin = 28;
        int half = (Screen.width - (margin * 3)) / 2;
        int listTop = 168;
        int listHeight = Screen.height - listTop - 150;

        DrawBackground();

        // A dim wash over the artwork, so the forest does not fight the text. Built once
        // in BuildStyles — allocating a texture per OnGUI call would leak steadily.
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dimWash);

        GUI.Label(new Rect(margin, 22, Screen.width, 44), "Deck Builder", titleStyle);

        PlayerProfile profile = SaveSystem.Profile;

        int x = margin;
        for (int i = 0; i < profile.decks.Count; i++)
        {
            SavedDeck deck = profile.decks[i];
            bool isCurrent = ReferenceEquals(deck, editingDeck);
            if (GUI.Button(new Rect(x, 76, 150, 30), deck.deckName, isCurrent ? tabActiveStyle : tabStyle))
            {
                editingDeck = deck;
                profile.selectedDeckName = deck.deckName;
                Revalidate();
            }
            x += 156;
        }

        if (GUI.Button(new Rect(x, 76, 130, 30), "+ New deck", tabStyle))
        {
            SavedDeck created = new SavedDeck("Deck " + (profile.decks.Count + 1));
            profile.decks.Add(created);
            editingDeck = created;
            profile.selectedDeckName = created.deckName;
            Revalidate();
        }

        GUI.Label(new Rect(margin, 120, 70, 26), "Name", cardStatStyle);
        string renamed = GUI.TextField(new Rect(margin + 70, 118, 260, 28), editingDeck.deckName, 32, fieldStyle);
        if (renamed != editingDeck.deckName)
        {
            editingDeck.deckName = renamed;
            profile.selectedDeckName = renamed;
            Revalidate();
        }

        DrawCollection(new Rect(margin, listTop, half, listHeight));
        DrawDeck(new Rect((margin * 2) + half, listTop, half, listHeight));
        DrawFooter(margin, listTop + listHeight + 14);
    }

    private void Panel(Rect area, string heading)
    {
        GUI.Box(area, GUIContent.none, panelStyle);
        GUI.Box(new Rect(area.x, area.y, area.width, 32), heading, headerStyle);
    }

    private void DrawCollection(Rect area)
    {
        Panel(area, "Collection");

        const float rowHeight = 54f;
        Rect inner = new Rect(area.x + 8, area.y + 38, area.width - 16, area.height - 46);
        Rect content = new Rect(0, 0, inner.width - 22, CardDatabase.All.Count * rowHeight);

        collectionScroll = GUI.BeginScrollView(inner, collectionScroll, content);

        for (int i = 0; i < CardDatabase.All.Count; i++)
        {
            CardData card = CardDatabase.All[i];
            int inDeck = editingDeck.CountOf(card.Id);
            float y = i * rowHeight;

            if (i % 2 == 0)
            {
                GUI.Box(new Rect(0, y, content.width, rowHeight - 4), GUIContent.none, rowStyle);
            }

            GUI.Label(new Rect(10, y + 4, content.width - 190, 22), card.DisplayName, cardNameStyle);

            GUI.Label(new Rect(10, y + 27, content.width - 190, 20),
                card.Damage + " dmg   " + card.Hp + " hp   " + card.SupplyCost + " supply   "
                + card.MoraleCost + " morale", cardStatStyle);

            // Faction and keywords carry the colour, so a deck's character is visible at
            // a glance rather than having to be read.
            tagStyle.normal.textColor = FactionColour(card.Faction);
            GUI.Label(new Rect(content.width - 250, y + 4, 160, 20), card.Faction.ToString(), tagStyle);

            string keywords = Keywords(card);
            if (keywords.Length > 0)
            {
                tagStyle.normal.textColor = Palette.Gold;
                GUI.Label(new Rect(content.width - 250, y + 27, 160, 20), keywords, tagStyle);
            }

            bool atLimit = inDeck >= DeckValidator.MaxCopiesPerCard;
            GUI.enabled = !atLimit;
            string label = atLimit ? "Max" : (inDeck > 0 ? "Add  " + inDeck : "Add");
            if (GUI.Button(new Rect(content.width - 82, y + 10, 74, 30), label, actionStyle))
            {
                editingDeck.Add(card.Id);
                Revalidate();
            }
            GUI.enabled = true;
        }

        GUI.EndScrollView();
    }

    private static string Keywords(CardData card)
    {
        if (card.Abilities.Length == 0)
        {
            return "";
        }

        string text = "";
        foreach (CardAbility ability in card.Abilities)
        {
            text += ability + " ";
        }
        return text.Trim();
    }

    private void DrawDeck(Rect area)
    {
        Panel(area, editingDeck.deckName + "  —  " + editingDeck.CardCount + " cards, "
                    + editingDeck.TotalMoraleCost + " morale");

        const float rowHeight = 34f;
        Rect inner = new Rect(area.x + 8, area.y + 38, area.width - 16, area.height - 46);
        Rect content = new Rect(0, 0, inner.width - 22, editingDeck.entries.Count * rowHeight);

        deckScroll = GUI.BeginScrollView(inner, deckScroll, content);

        for (int i = 0; i < editingDeck.entries.Count; i++)
        {
            SavedDeck.Entry entry = editingDeck.entries[i];
            CardData card = CardDatabase.Find(entry.cardId);
            float y = i * rowHeight;

            if (i % 2 == 0)
            {
                GUI.Box(new Rect(0, y, content.width, rowHeight - 4), GUIContent.none, rowStyle);
            }

            GUI.Label(new Rect(10, y + 4, 42, 22), entry.count + "x", cardNameStyle);
            GUI.Label(new Rect(52, y + 4, content.width - 200, 22),
                card != null ? card.DisplayName : entry.cardId, cardNameStyle);

            if (card != null)
            {
                tagStyle.normal.textColor = FactionColour(card.Faction);
                GUI.Label(new Rect(content.width - 230, y + 6, 130, 20),
                    card.SupplyCost + " supply", tagStyle);
            }

            if (GUI.Button(new Rect(content.width - 92, y + 2, 84, 26), "Remove", actionStyle))
            {
                editingDeck.Remove(entry.cardId);
                Revalidate();
                break;   // the list just changed; redraw next frame
            }
        }

        GUI.EndScrollView();

        if (editingDeck.entries.Count == 0)
        {
            GUI.Label(new Rect(area.x + 20, area.y + 60, area.width - 40, 40),
                "Empty. Add cards from the collection on the left.", cardStatStyle);
        }
    }

    private void DrawFooter(int margin, int y)
    {
        // Legality is reported continuously rather than only on save, so the player can
        // see why a deck is not playable while they are building it.
        string report;
        if (validation.IsLegal)
        {
            footerStyle.normal.textColor = validation.Warnings.Count > 0 ? Palette.Gold : Palette.Good;
            report = validation.Warnings.Count > 0
                ? "Legal deck.  " + string.Join("  ", validation.Warnings.ToArray())
                : "Legal deck — ready for battle.";
        }
        else
        {
            footerStyle.normal.textColor = Palette.Bad;
            report = string.Join("   ", validation.Problems.ToArray());
        }

        GUI.Label(new Rect(margin, y, Screen.width - (margin * 2), 58), report, footerStyle);

        if (GUI.Button(new Rect(margin, y + 64, 140, 34), "Save", actionStyle))
        {
            SaveSystem.Save();
            statusMessage = validation.IsLegal
                ? "Saved."
                : "Saved, but this deck is not legal and cannot be taken into battle.";
        }

        if (GUI.Button(new Rect(margin + 152, y + 64, 140, 34), "Main Menu", actionStyle))
        {
            SaveSystem.Save();
            this.currentGUIMethod = MainMenu;
        }

        footerStyle.normal.textColor = Palette.Muted;
        GUI.Label(new Rect(margin + 310, y + 70, Screen.width - margin - 320, 30),
            statusMessage, footerStyle);
    }
    #endregion
}

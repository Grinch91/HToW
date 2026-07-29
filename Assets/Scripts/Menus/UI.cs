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

    private bool MenuButton(int index, int count, string label)
    {
        Rect slot = ButtonSlot(index, count);

        if (btn != null)
        {
            GUI.DrawTexture(slot, btn, ScaleMode.StretchToFill);
        }

        return GUI.Button(
            new Rect(slot.x + 16, slot.y + 8, slot.width - 32, slot.height - 16),
            label,
            mystyle);
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

    public void DeckBuilder()
    {
        const int margin = 20;
        int half = (Screen.width - (margin * 3)) / 2;
        int listTop = 150;
        int listHeight = Screen.height - listTop - 140;

        GUI.Label(new Rect(margin, 20, Screen.width, 40), "Deck Builder");

        // Deck selection.
        PlayerProfile profile = SaveSystem.Profile;
        int x = margin;
        for (int i = 0; i < profile.decks.Count; i++)
        {
            SavedDeck deck = profile.decks[i];
            bool isCurrent = ReferenceEquals(deck, editingDeck);
            if (GUI.Button(new Rect(x, 60, 130, 26), (isCurrent ? "> " : "") + deck.deckName))
            {
                editingDeck = deck;
                profile.selectedDeckName = deck.deckName;
                Revalidate();
            }
            x += 136;
        }

        if (GUI.Button(new Rect(x, 60, 110, 26), "+ New deck"))
        {
            SavedDeck created = new SavedDeck("Deck " + (profile.decks.Count + 1));
            profile.decks.Add(created);
            editingDeck = created;
            profile.selectedDeckName = created.deckName;
            Revalidate();
        }

        // Rename.
        GUI.Label(new Rect(margin, 96, 60, 24), "Name");
        string renamed = GUI.TextField(new Rect(margin + 60, 96, 220, 24), editingDeck.deckName, 32);
        if (renamed != editingDeck.deckName)
        {
            editingDeck.deckName = renamed;
            profile.selectedDeckName = renamed;
            Revalidate();
        }

        DrawCollection(new Rect(margin, listTop, half, listHeight));
        DrawDeck(new Rect(margin * 2 + half, listTop, half, listHeight));
        DrawFooter(margin, listTop + listHeight + 10);
    }

    private void DrawCollection(Rect area)
    {
        GUI.Box(area, "Collection");

        Rect inner = new Rect(area.x + 6, area.y + 24, area.width - 12, area.height - 30);
        int rows = CardDatabase.All.Count;
        Rect content = new Rect(0, 0, inner.width - 20, rows * 46);

        collectionScroll = GUI.BeginScrollView(inner, collectionScroll, content);

        for (int i = 0; i < rows; i++)
        {
            CardData card = CardDatabase.All[i];
            int inDeck = editingDeck.CountOf(card.Id);
            float y = i * 46;

            GUI.Label(new Rect(0, y, content.width - 60, 22),
                card.DisplayName + "   " + card.SupplyCost + " supply");
            GUI.Label(new Rect(0, y + 20, content.width - 60, 22),
                card.Damage + " dmg / " + card.Hp + " hp / " + card.MoraleCost + " morale   "
                + card.Faction + "   " + Keywords(card));

            GUI.enabled = inDeck < DeckValidator.MaxCopiesPerCard;
            if (GUI.Button(new Rect(content.width - 56, y + 6, 50, 28),
                    inDeck > 0 ? "Add " + inDeck : "Add"))
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
        GUI.Box(area, editingDeck.deckName + " — " + editingDeck.CardCount + " cards, "
                      + editingDeck.TotalMoraleCost + " morale");

        Rect inner = new Rect(area.x + 6, area.y + 24, area.width - 12, area.height - 30);
        Rect content = new Rect(0, 0, inner.width - 20, editingDeck.entries.Count * 30);

        deckScroll = GUI.BeginScrollView(inner, deckScroll, content);

        for (int i = 0; i < editingDeck.entries.Count; i++)
        {
            SavedDeck.Entry entry = editingDeck.entries[i];
            CardData card = CardDatabase.Find(entry.cardId);
            float y = i * 30;

            GUI.Label(new Rect(0, y, content.width - 60, 24),
                entry.count + "x  " + (card != null ? card.DisplayName : entry.cardId));

            if (GUI.Button(new Rect(content.width - 56, y, 50, 24), "Remove"))
            {
                editingDeck.Remove(entry.cardId);
                Revalidate();
                break;   // the list just changed; redraw next frame
            }
        }

        GUI.EndScrollView();
    }

    private void DrawFooter(int margin, int y)
    {
        // Legality is reported continuously rather than only on save, so the player can
        // see why a deck is not playable while they are building it.
        string report;
        if (validation.IsLegal)
        {
            report = "Legal deck." + (validation.Warnings.Count > 0
                ? "  " + string.Join("  ", validation.Warnings.ToArray())
                : "");
        }
        else
        {
            report = string.Join("   ", validation.Problems.ToArray());
        }

        GUI.Label(new Rect(margin, y, Screen.width - (margin * 2), 60), report);

        if (GUI.Button(new Rect(margin, y + 62, 120, 30), "Save"))
        {
            SaveSystem.Save();
            statusMessage = validation.IsLegal
                ? "Saved."
                : "Saved, but this deck is not legal and cannot be taken into battle.";
        }

        if (GUI.Button(new Rect(margin + 130, y + 62, 120, 30), "Main Menu"))
        {
            SaveSystem.Save();
            this.currentGUIMethod = MainMenu;
        }

        GUI.Label(new Rect(margin + 260, y + 62, Screen.width - margin - 270, 30), statusMessage);
    }
    #endregion
}

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

    //Main menu
    #region
    public void MainMenu()
    {
        GUI.Label(new Rect((Screen.width / 4), -60, Screen.width, Screen.height), banner, mystyle);

        GUI.Label(new Rect((Screen.width / 2 - 130), 600, 250, 58), btn, mystyle);
        if (GUI.Button(new Rect((Screen.width / 2 - 100), 610, 100, 40), "Battle Mode", mystyle))
        {
            this.currentGUIMethod = BattleMenu;
        }

        GUI.Label(new Rect((Screen.width / 2 - 130), 680, 250, 58), btn, mystyle);
        if (GUI.Button(new Rect((Screen.width / 2 - 100), 690, 100, 40), "Cards", mystyle))
        {
            OpenDeckBuilder();
        }

        GUI.Label(new Rect((Screen.width / 2 - 130), 760, 250, 58), btn, mystyle);
        if (GUI.Button(new Rect((Screen.width / 2 - 100), 770, 100, 40), "Exit", mystyle))
        {
            Application.Quit();
        }
    }

    public void BattleMenu()
    {
        GUI.Label(new Rect((Screen.width / 4), -60, Screen.width, Screen.height), banner, mystyle);

        SavedDeck selected = SaveSystem.Profile.SelectedDeck;
        GUI.Label(new Rect((Screen.width / 2 - 200), 550, 400, 30),
            selected != null
                ? "Taking " + selected.deckName + " into battle (" + selected.CardCount + " cards)"
                : "No deck selected — the default Celtic deck will be used.");

        GUI.Label(new Rect((Screen.width / 2 - 130), 600, 250, 58), btn, mystyle);
        if (GUI.Button(new Rect((Screen.width / 2 - 100), 610, 100, 40), "Load Battle", mystyle))
        {
            SceneManager.LoadScene("Battle");
        }

        GUI.Label(new Rect((Screen.width / 2 - 130), 680, 250, 58), btn, mystyle);
        if (GUI.Button(new Rect((Screen.width / 2 - 100), 690, 100, 40), "Main Menu", mystyle))
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

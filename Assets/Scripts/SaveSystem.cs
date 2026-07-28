using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Everything persisted between sessions.</summary>
[Serializable]
public class PlayerProfile
{
    public List<SavedDeck> decks = new List<SavedDeck>();

    /// <summary>Name of the deck taken into battle.</summary>
    public string selectedDeckName = "";

    public SavedDeck Find(string deckName)
    {
        for (int i = 0; i < decks.Count; i++)
        {
            if (decks[i].deckName == deckName)
            {
                return decks[i];
            }
        }
        return null;
    }

    public SavedDeck SelectedDeck
    {
        get
        {
            SavedDeck selected = Find(selectedDeckName);
            if (selected != null)
            {
                return selected;
            }

            return decks.Count > 0 ? decks[0] : null;
        }
    }
}

/// <summary>
/// Reads and writes the player profile.
///
/// The project's first save system. The 2014 main menu had a "Continue" button whose
/// body was commented out; nothing has ever been persisted until now.
///
/// Uses <c>Application.persistentDataPath</c> rather than the pattern the old deck
/// loader used — <c>File.ReadAllLines(Application.dataPath + ...)</c> — which wrote
/// beside the executable, broke on any non-desktop platform, and left game data sitting
/// in the install directory (bug F6).
/// </summary>
public static class SaveSystem
{
    private const string FileName = "profile.json";

    private static PlayerProfile cached;

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>The current profile, loading it or seeding a new one on first use.</summary>
    public static PlayerProfile Profile
    {
        get
        {
            if (cached == null)
            {
                cached = Load();
            }
            return cached;
        }
    }

    public static PlayerProfile Load()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                PlayerProfile loaded = JsonUtility.FromJson<PlayerProfile>(json);
                if (loaded != null)
                {
                    Debug.Log("Loaded profile with " + loaded.decks.Count + " deck(s) from " + SavePath);
                    return loaded;
                }
            }
        }
        catch (Exception e)
        {
            // A corrupt save must never stop the game from starting.
            Debug.LogWarning("Could not read save at " + SavePath + ": " + e.Message
                             + ". Starting a fresh profile.");
        }

        return CreateDefaultProfile();
    }

    public static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Profile, true);
            File.WriteAllText(SavePath, json);
            Debug.Log("Saved profile to " + SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Could not write save to " + SavePath + ": " + e.Message);
        }
    }

    /// <summary>Forgets the cached profile. Used by tests.</summary>
    public static void Reset()
    {
        cached = null;
    }

    /// <summary>
    /// Seeds a profile from the authored decks so a new player has something playable
    /// and something to edit, rather than an empty deckbuilder.
    /// </summary>
    private static PlayerProfile CreateDefaultProfile()
    {
        PlayerProfile profile = new PlayerProfile();

        foreach (string path in new[] { "DeckData/Celtic", "DeckData/Viking" })
        {
            DeckData authored = Resources.Load<DeckData>(path);
            if (authored != null)
            {
                profile.decks.Add(SavedDeck.From(authored));
            }
        }

        if (profile.decks.Count > 0)
        {
            profile.selectedDeckName = profile.decks[0].deckName;
        }

        return profile;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Drives one battle: sets up the zones, sequences turns, plays cards, runs the AI and
/// resolves combat.
///
/// Still doing more than one job — combat resolution and AI belong in their own types
/// (Milestones 3 and 5). But it no longer owns card data, card construction, layout
/// maths or selection state, and it is roughly half the size it was.
/// </summary>
public class Game : MonoBehaviour
{
    //Decks
    #region
    [Header("Decks (falls back to Resources/DeckData if unset)")]
    public DeckData playerDeckData;
    public DeckData aiDeckData;
    #endregion


    //Zones — field names preserved so the existing scene wiring still resolves
    #region
    public CardZone playerDeck;
    public CardZone aiDeck;
    public CardZone playerHand;
    public CardZone aiHand;
    public CardZone playerActive;
    public CardZone aiActive;
    #endregion


    //Characters
    #region
    public Player playerInstance = new Player();
    public Enemy enemyInstance = new Enemy();
    #endregion


    //Tuning
    #region
    [Header("Pacing")]
    [Tooltip("Seconds the AI pauses before acting, so the player can follow what it does.")]
    public float aiThinkSeconds = 1.5f;

    [Tooltip("How hard the opponent plays. All tiers obey identical rules; only their appetite for risk differs.")]
    public AiDifficulty aiDifficulty = AiDifficulty.Balanced;

    [Header("Card visuals")]
    [Tooltip("Card art is 5.12 x 7.44 world units unscaled; 0.55 makes a card about " +
             "2.8 x 4.1, which fits four rows inside the camera's 30-unit height.")]
    public Vector3 cardScale = new Vector3(0.55f, 0.55f, 1.0f);
    public Color attackerHighlight = new Color(0.7f, 1.0f, 0.7f);
    public Color targetHighlight = new Color(1.0f, 0.7f, 0.7f);
    #endregion


    //Runtime state
    #region
    readonly TurnController turns = new TurnController();
    readonly BattleSelection selection = new BattleSelection();
    AiController ai;

    GameObject PlayerWins;
    GameObject PlayerTurn;
    GameObject AIWins;
    GameObject AITurn;

    // Discard piles. Created at runtime because they are not displayed and therefore
    // need no scene anchor; destroyed cards and exhausted draw piles cycle through here.
    CardZone playerDiscard;
    CardZone aiDiscard;

    // The HUD objects have existed in Battle.unity since 2014 and nothing ever wrote to
    // them, so the player could not see their own morale or supply (bug M8). Found by
    // name because they are scene roots, not children of Game; they become TextMeshPro
    // with serialized references in the UI pass.
    TextMesh playerHealthText;
    TextMesh enemyHealthText;
    TextMesh playerSupplyText;
    TextMesh enemySupplyText;

    Sprite cardBack;
    bool matchStarted;
    float aiThinkTimer;
    bool awaitingDismissal;
    #endregion


    //Start
    #region
    void Start()
    {
        PlayerWins = transform.Find("MessagePlayerWin").gameObject;
        PlayerTurn = transform.Find("MessagePlayerTurn").gameObject;
        AIWins = transform.Find("MessageAIWin").gameObject;
        AITurn = transform.Find("MessageAITurn").gameObject;
        HideAllMessages();

        cardBack = Resources.Load<Sprite>("back");

        playerDeck.Configure(ZoneKind.DrawPile, Side.Player);
        aiDeck.Configure(ZoneKind.DrawPile, Side.AI);
        playerHand.Configure(ZoneKind.Hand, Side.Player);
        aiHand.Configure(ZoneKind.Hand, Side.AI);
        playerActive.Configure(ZoneKind.Board, Side.Player);
        aiActive.Configure(ZoneKind.Board, Side.AI);

        ai = new AiController(aiDifficulty);

        playerDiscard = CreateDiscardZone("Discard-Player", Side.Player);
        aiDiscard = CreateDiscardZone("Discard-Enemy", Side.AI);

        BindHud();

        turns.TurnStarted += OnTurnStarted;
        turns.TurnEnded += OnTurnEnded;

        BuildPlayerDeck();
        BuildDeck(aiDeck, ResolveDeck(aiDeckData, "DeckData/Viking"), Side.AI);

        UpdateHud();
    }

    CardZone CreateDiscardZone(string zoneName, Side owner)
    {
        GameObject obj = new GameObject(zoneName);
        obj.transform.SetParent(transform, false);
        CardZone zone = obj.AddComponent<CardZone>();
        zone.Configure(ZoneKind.DrawPile, owner);
        return zone;
    }

    void BindHud()
    {
        playerHealthText = FindHudLabel("playerHealth");
        enemyHealthText = FindHudLabel("enemyHealth");
        playerSupplyText = FindHudLabel("playerSupply");
        enemySupplyText = FindHudLabel("enemySupply");
    }

    // The 2014 scene placed HUD *icons* (hp.png, supplyicon.png) as SpriteRenderers but
    // never added the numbers beside them — the HUD was half-built, not merely unwired.
    //
    // Labels are created at the *root* of the scene rather than parented to the icons.
    // The icons hang off the General objects, which carry non-uniform scale
    // (1.56, 1.32, 1); inheriting that scale, on top of a characterSize tuned for a
    // small camera, produced text several world units tall that covered the board.
    // Root-level objects have no inherited scale, so the size on screen is predictable.
    static TextMesh FindHudLabel(string objectName)
    {
        GameObject anchor = GameObject.Find(objectName);
        if (anchor == null)
        {
            Debug.LogWarning("HUD anchor '" + objectName + "' not found in scene.");
            return null;
        }

        // The 2014 scene left an empty TextMesh child under some icons. Hide them so
        // they cannot render stale placeholder text beside the real label.
        foreach (TextMesh legacy in anchor.GetComponentsInChildren<TextMesh>(true))
        {
            legacy.gameObject.SetActive(false);
        }

        Vector3 iconPosition = anchor.transform.position;

        // Icons sit near the left and right edges of the board, so the label goes on
        // whichever side faces the middle.
        bool onRight = iconPosition.x > 0.0f;
        float offsetX = onRight ? -2.0f : 2.0f;

        GameObject labelObject = new GameObject(objectName + "Label");
        labelObject.transform.position = new Vector3(
            iconPosition.x + offsetX, iconPosition.y, iconPosition.z - 0.5f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.anchor = onRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        label.alignment = onRight ? TextAlignment.Right : TextAlignment.Left;
        label.fontSize = 64;

        // Camera is orthographic size 15, so the view is 30 world units tall. This gives
        // roughly 0.8 units of cap height — legible without dominating the board.
        label.characterSize = 0.12f;
        label.color = Color.white;

        SpriteRenderer icon = anchor.GetComponent<SpriteRenderer>();
        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (icon != null && renderer != null)
        {
            renderer.sortingLayerID = icon.sortingLayerID;
            renderer.sortingOrder = icon.sortingOrder + 10;
        }

        return label;
    }

    void UpdateHud()
    {
        SetLabel(playerHealthText, "Morale " + playerInstance.playerMorale);
        SetLabel(enemyHealthText, "Morale " + enemyInstance.enemyMorale);
        SetLabel(playerSupplyText, "Supply " + playerInstance.playerSupply + "/" + playerInstance.MaxSupply);
        SetLabel(enemySupplyText, "Supply " + enemyInstance.enemySupply + "/" + enemyInstance.MaxSupply);
    }

    static void SetLabel(TextMesh label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    void OnDestroy()
    {
        turns.TurnStarted -= OnTurnStarted;
        turns.TurnEnded -= OnTurnEnded;
    }

    static DeckData ResolveDeck(DeckData assigned, string resourcePath)
    {
        if (assigned != null)
        {
            return assigned;
        }

        DeckData loaded = Resources.Load<DeckData>(resourcePath);
        if (loaded == null)
        {
            Debug.LogError("No DeckData assigned and none found at Resources/" + resourcePath);
        }

        return loaded;
    }

    // The player brings whichever deck they selected in the deckbuilder. Falls back to
    // the authored Celtic list when there is no profile — a fresh install, or a save
    // that failed to read.
    void BuildPlayerDeck()
    {
        SavedDeck chosen = SaveSystem.Profile.SelectedDeck;

        if (chosen != null && chosen.CardCount > 0)
        {
            playerDeck.Clear();
            playerDeck.AddRange(chosen.BuildCards(Side.Player));
            playerDeck.Shuffle();
            Debug.Log("Built player deck '" + chosen.deckName + "': " + playerDeck.Count
                      + " cards, " + chosen.TotalMoraleCost + " total morale cost");
            return;
        }

        BuildDeck(playerDeck, ResolveDeck(playerDeckData, "DeckData/Celtic"), Side.Player);
    }

    // Card stats used to live in a nine-branch if-chain inside Deck.AddToDeck(), so a
    // balance change was a code change. They are now authored as CardData assets.
    void BuildDeck(CardZone pile, DeckData data, Side owner)
    {
        pile.Clear();
        if (data == null)
        {
            return;
        }

        pile.AddRange(data.BuildCards(owner));
        pile.Shuffle();
        Debug.Log("Built " + owner + " deck: " + pile.Count + " cards, "
                  + data.TotalMoraleCost + " total morale cost");
    }
    #endregion


    //Messages
    #region
    void HideAllMessages()
    {
        PlayerWins.SetActive(false);
        PlayerTurn.SetActive(false);
        AIWins.SetActive(false);
        AITurn.SetActive(false);
    }

    void ShowOnly(GameObject message)
    {
        HideAllMessages();
        message.SetActive(true);
    }

    void ShowTurnMessage(Side side)
    {
        ShowOnly(side == Side.Player ? PlayerTurn : AITurn);
    }

    public void OnButton(string message)
    {
        switch (message)
        {
            case "EndTurn":
                if (turns.ActiveSide == Side.Player && turns.Phase == TurnPhase.Main)
                {
                    Debug.Log("Player ended their turn");
                    turns.RequestEndTurn();
                }
                break;
        }
    }
    #endregion


    //Card views — one factory replacing four near-identical construction methods
    #region
    CardView CreateView(CardInstance card, CardZone zone, bool faceDown)
    {
        GameObject obj = new GameObject("Card");
        obj.transform.SetParent(zone.transform, false);
        obj.transform.localScale = cardScale;

        // The collider is sized from the sprite inside CardView.Bind.
        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<SpriteRenderer>();

        CardView view = obj.AddComponent<CardView>();
        Sprite face = faceDown ? cardBack : card.Data.Art;
        view.Bind(card, this, face, faceDown);

        // Card names are no longer written into Unity tags. Tags were used as identity
        // for FindGameObjectWithTag-based destruction, which could delete the attacker's
        // own card (bug M4) and required every card name to be pre-registered by hand.
        return view;
    }

    void DestroyView(CardInstance card)
    {
        if (card.View != null)
        {
            Destroy(card.View.gameObject);
            card.View = null;
        }
    }

    void RefreshHighlights()
    {
        // Explicit null checks rather than ?. — the null-conditional operator bypasses
        // Unity's overloaded == and would happily call into a destroyed object.
        foreach (CardInstance card in playerActive.Cards)
        {
            if (card.View != null)
            {
                card.View.SetHighlight(
                    ReferenceEquals(card, selection.Attacker) ? attackerHighlight : Color.white);
            }
        }

        foreach (CardInstance card in aiActive.Cards)
        {
            if (card.View != null)
            {
                card.View.SetHighlight(
                    ReferenceEquals(card, selection.Target) ? targetHighlight : Color.white);
            }
        }
    }
    #endregion


    //Drawing and playing
    #region
    void Draw(Side side)
    {
        CardZone pile = side == Side.Player ? playerDeck : aiDeck;
        CardZone hand = side == Side.Player ? playerHand : aiHand;
        CardZone discard = side == Side.Player ? playerDiscard : aiDiscard;

        // Recycle the discard pile rather than letting a side run dry permanently. The
        // original 5-card Celtic deck was exhausted by the opening draw and every
        // subsequent turn logged "Deck is empty" (bug M11).
        if (pile.IsEmpty && !discard.IsEmpty)
        {
            Debug.Log(side + " reshuffles " + discard.Count + " cards from the discard pile");
            pile.AddRange(discard.Cards);
            discard.Clear();
            pile.Shuffle();
        }

        CardInstance card = pile.DrawTop();
        if (card == null)
        {
            Debug.Log(side + " has no cards left to draw");
            return;
        }

        hand.Add(card);
        CreateView(card, hand, faceDown: side == Side.AI);
        hand.LayOut();
    }

    /// <summary>
    /// Moves a specific card from its owner's hand into play, paying its supply cost.
    ///
    /// The old path appended the chosen card to the board and then dealt whatever sat
    /// at index 0, so every play after the first spawned a duplicate of an older card
    /// (bug C2). Moving one instance by reference makes that impossible.
    /// </summary>
    public bool PlayCard(CardInstance card)
    {
        Side side = card.Owner;
        CardZone hand = side == Side.Player ? playerHand : aiHand;
        CardZone board = side == Side.Player ? playerActive : aiActive;

        if (!hand.Contains(card))
        {
            return false;
        }

        if (AvailableSupply(side) < card.Data.SupplyCost)
        {
            Debug.Log("Not enough supply for " + card.Data.DisplayName
                      + " (costs " + card.Data.SupplyCost + ", have " + AvailableSupply(side) + ")");
            return false;
        }

        SpendSupply(side, card.Data.SupplyCost);

        hand.Remove(card);
        board.Add(card);

        // The card keeps its view; it just changes parent and re-lays out.
        if (card.View != null)
        {
            card.View.transform.SetParent(board.transform, false);
            card.View.Reveal();
        }
        else
        {
            CreateView(card, board, faceDown: false);
        }

        hand.LayOut();
        board.LayOut();
        UpdateHud();

        Debug.Log(side + " played " + card.Data.DisplayName
                  + " (" + AvailableSupply(side) + " supply left)");
        return true;
    }

    public void OnCardClicked(CardView view)
    {
        CardInstance card = view.Instance;

        if (turns.ActiveSide != Side.Player || turns.Phase != TurnPhase.Main)
        {
            return;
        }

        if (playerHand.Contains(card))
        {
            PlayCard(card);
            return;
        }

        if (playerActive.Contains(card) || aiActive.Contains(card))
        {
            selection.Select(card);
            RefreshHighlights();

            if (selection.IsComplete)
            {
                ResolveSelectedAttack();
            }
        }
    }
    #endregion


    //Supply
    #region
    int AvailableSupply(Side side)
    {
        return side == Side.Player ? playerInstance.playerSupply : enemyInstance.enemySupply;
    }

    void SpendSupply(Side side, int amount)
    {
        if (side == Side.Player)
        {
            playerInstance.playerSupply -= amount;
        }
        else
        {
            enemyInstance.enemySupply -= amount;
        }
    }

    // Supply refills to a ceiling that grows by one per turn, rather than accumulating
    // whatever was left over.
    //
    // Accumulating was measurably broken. Simulation over 4,000 matches showed supply
    // never rising above about three, because there is always a 1-cost card worth
    // playing and spending it leaves nothing banked. Every 5-cost card — Fianna,
    // Huscarl, Captain — went unplayed in every single match, so a third of the roster
    // was dead and card cost barely mattered.
    //
    // Refilling makes the turn number the real constraint: on turn five you command five
    // supply whatever you did on turn four, so expensive cards arrive on schedule and
    // cost becomes a genuine decision. See Docs/Decisions.md D-12.
    void GainSupply(Side side)
    {
        if (side == Side.Player)
        {
            playerInstance.playerSupplyCap =
                Mathf.Min(playerInstance.playerSupplyCap + 1, playerInstance.MaxSupply);
            playerInstance.playerSupply = playerInstance.playerSupplyCap;
        }
        else
        {
            enemyInstance.enemySupplyCap =
                Mathf.Min(enemyInstance.enemySupplyCap + 1, enemyInstance.MaxSupply);
            enemyInstance.enemySupply = enemyInstance.enemySupplyCap;
        }
    }
    #endregion


    //Turn phases
    #region
    void OnTurnStarted(Side side)
    {
        Debug.Log("Turn " + turns.TurnNumber + " - " + side + " to act");
        ShowTurnMessage(side);
        Draw(side);
        GainSupply(side);

        foreach (CardInstance card in (side == Side.Player ? playerActive : aiActive).Cards)
        {
            card.HasAttacked = false;
        }

        selection.Clear();
        RefreshHighlights();
        UpdateHud();
        aiThinkTimer = 0.0f;
    }

    void OnTurnEnded(Side side)
    {
        // The player attacks by choosing cards during their turn; only the AI needs its
        // attacks resolved automatically here.
        if (side == Side.AI)
        {
            ResolveAiAttacks();
        }

        selection.Clear();
        RefreshHighlights();
        CheckForWinner();
    }
    #endregion


    //Combat
    #region
    void ResolveSelectedAttack()
    {
        CardInstance attacker = selection.Attacker;
        CardInstance target = selection.Target;

        if (attacker.HasAttacked)
        {
            Debug.Log(attacker.Data.DisplayName + " has already attacked this turn");
            selection.Clear();
            RefreshHighlights();
            return;
        }

        Attack(attacker, target);
        attacker.HasAttacked = true;
        selection.Clear();
        RefreshHighlights();
    }

    void Attack(CardInstance attacker, CardInstance target)
    {
        CombatResolver.Result result = CombatResolver.Resolve(attacker, target);

        Debug.Log(attacker.Data.DisplayName + " hits " + target.Data.DisplayName
                  + " for " + result.DamageToTarget
                  + (result.DamageToAttacker > 0
                      ? " and takes " + result.DamageToAttacker + " back"
                      : " without retaliation (Volley)"));

        // Refresh both cards' printed health before anything is destroyed.
        if (attacker.View != null) attacker.View.RefreshStats();
        if (target.View != null) target.View.RefreshStats();

        if (result.TargetDestroyed)
        {
            Destroy(target);
        }

        if (result.AttackerDestroyed)
        {
            Destroy(attacker);
        }

        UpdateHud();
    }

    void Destroy(CardInstance card)
    {
        CardZone board = card.Owner == Side.Player ? playerActive : aiActive;
        CardZone discard = card.Owner == Side.Player ? playerDiscard : aiDiscard;

        board.Remove(card);

        // Morale is lost by the card's OWNER, in proportion to how valuable it was.
        // This is the game's most distinctive rule: you are not damaged by being
        // attacked, you are damaged by losing your own people.
        if (card.Owner == Side.Player)
        {
            playerInstance.playerMorale -= card.Data.MoraleCost;
        }
        else
        {
            enemyInstance.enemyMorale -= card.Data.MoraleCost;
        }

        Debug.Log(card.Data.DisplayName + " destroyed; " + card.Owner
                  + " loses " + card.Data.MoraleCost + " morale");

        card.Heal();          // returns to the discard pile at full strength
        discard.Add(card);

        selection.Forget(card);
        DestroyView(card);
        board.LayOut();
        UpdateHud();

        CheckForWinner();
    }

    public void CheckForWinner()
    {
        if (turns.Phase == TurnPhase.GameOver)
        {
            return;
        }

        bool playerBroken = playerInstance.playerMorale <= 0;
        bool enemyBroken = enemyInstance.enemyMorale <= 0;

        if (!playerBroken && !enemyBroken)
        {
            return;
        }

        turns.EndMatch();
        awaitingDismissal = true;

        // Retaliation can push both sides past zero on the same exchange. Whoever is
        // less far past it has held out longer and takes the win; the player is given
        // an exact tie. Matches the rule the balance simulator uses.
        if (playerBroken && enemyBroken)
        {
            ShowOnly(playerInstance.playerMorale >= enemyInstance.enemyMorale ? PlayerWins : AIWins);
        }
        else
        {
            ShowOnly(playerBroken ? AIWins : PlayerWins);
        }
    }
    #endregion


    //AI
    #region
    // The AI now plays from its HAND and pays supply, like the player. It previously
    // scanned its draw pile, never removed what it played, and ignored its hand
    // entirely (bug M1). Scoring and difficulty are Milestone 5.
    void RunAiTurn()
    {
        // Keep playing while anything is affordable, rather than exactly one card per
        // turn. The old `do { ... } while (n < 1)` loop ran precisely once — a stand-in
        // for a rule that was never written.
        while (true)
        {
            CardInstance choice = ai.ChoosePlay(aiHand.Cards, AvailableSupply(Side.AI));
            if (choice == null || !PlayCard(choice))
            {
                break;
            }
        }
    }

    void ResolveAiAttacks()
    {
        foreach (CardInstance attacker in aiActive.Snapshot())
        {
            if (attacker.HasAttacked || playerActive.Count == 0)
            {
                continue;
            }

            CardInstance target = ChooseAiTarget(attacker);
            if (target == null)
            {
                continue;
            }

            Attack(attacker, target);
            attacker.HasAttacked = true;
        }
    }

    CardInstance ChooseAiTarget(CardInstance attacker)
    {
        CardInstance target = ai.ChooseTarget(attacker, playerActive.Cards);
        if (target == null)
        {
            Debug.Log(attacker.Data.DisplayName + " holds back — no attack worth making");
        }

        return target;
    }
    #endregion


    //Setup and update
    #region
    void InitialSetUp()
    {
        for (int card = 0; card < 5; card++)
        {
            Draw(Side.Player);
            Draw(Side.AI);
        }

        turns.BeginMatch(FlipCoin());
    }

    static Side FlipCoin()
    {
        Side first = Random.Range(0, 2) == 0 ? Side.AI : Side.Player;
        Debug.Log(first + " goes first");
        return first;
    }

    void Update()
    {
        if (!matchStarted)
        {
            InitialSetUp();
            matchStarted = true;
        }

        if (turns.Phase == TurnPhase.GameOver)
        {
            // The result used to be yanked away after five seconds. Let the player sit
            // with it and dismiss it themselves. See Docs/GameDesign.md section 8.
            if (awaitingDismissal && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            {
                SceneManager.LoadScene("MainMenu");
            }
            return;
        }

        if (turns.ActiveSide == Side.AI && turns.Phase == TurnPhase.Main)
        {
            aiThinkTimer += Time.deltaTime;
            if (aiThinkTimer >= aiThinkSeconds)
            {
                RunAiTurn();
                turns.RequestEndTurn();
            }
        }

        // Terminates: Main does not advance unless an end-turn has been requested, so a
        // player turn parks here and waits for input.
        while (turns.Advance())
        {
        }
    }
    #endregion
}

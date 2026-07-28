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

    [Header("Card visuals")]
    public Vector2 cardSize = new Vector2(2.0f, 3.0f);
    public Vector3 cardScale = new Vector3(0.75f, 0.75f, 1.0f);
    public Color attackerHighlight = new Color(0.7f, 1.0f, 0.7f);
    public Color targetHighlight = new Color(1.0f, 0.7f, 0.7f);
    #endregion


    //Runtime state
    #region
    readonly TurnController turns = new TurnController();
    readonly BattleSelection selection = new BattleSelection();

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

        playerDiscard = CreateDiscardZone("Discard-Player", Side.Player);
        aiDiscard = CreateDiscardZone("Discard-Enemy", Side.AI);

        BindHud();

        turns.TurnStarted += OnTurnStarted;
        turns.TurnEnded += OnTurnEnded;

        BuildDeck(playerDeck, ResolveDeck(playerDeckData, "DeckData/Celtic"), Side.Player);
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
    // Attaching each label as a child of its icon reuses the placement that was already
    // authored, and avoids editing the scene for something the UI pass will replace with
    // TextMeshPro anyway.
    static TextMesh FindHudLabel(string objectName)
    {
        GameObject anchor = GameObject.Find(objectName);
        if (anchor == null)
        {
            Debug.LogWarning("HUD anchor '" + objectName + "' not found in scene.");
            return null;
        }

        TextMesh existing = anchor.GetComponentInChildren<TextMesh>();
        if (existing != null)
        {
            return existing;
        }

        GameObject labelObject = new GameObject(objectName + "Label");
        labelObject.transform.SetParent(anchor.transform, false);
        labelObject.transform.localPosition = new Vector3(1.4f, 0.0f, -0.1f);
        labelObject.transform.localScale = Vector3.one;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleLeft;
        label.alignment = TextAlignment.Left;
        label.fontSize = 64;
        label.characterSize = 0.5f;
        label.color = Color.white;

        SpriteRenderer icon = anchor.GetComponent<SpriteRenderer>();
        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (icon != null && renderer != null)
        {
            renderer.sortingLayerID = icon.sortingLayerID;
            renderer.sortingOrder = icon.sortingOrder + 1;
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

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = cardSize;

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

    void GainSupply(Side side)
    {
        if (side == Side.Player)
        {
            if (playerInstance.playerSupply < playerInstance.MaxSupply)
            {
                playerInstance.playerSupply++;
            }
        }
        else
        {
            if (enemyInstance.enemySupply < enemyInstance.MaxSupply)
            {
                enemyInstance.enemySupply++;
            }
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

        if (playerInstance.playerMorale <= 0)
        {
            turns.EndMatch();
            ShowOnly(AIWins);
            awaitingDismissal = true;
        }
        else if (enemyInstance.enemyMorale <= 0)
        {
            turns.EndMatch();
            ShowOnly(PlayerWins);
            awaitingDismissal = true;
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
        CardInstance best = null;
        foreach (CardInstance card in aiHand.Cards)
        {
            if (card.Data.SupplyCost > AvailableSupply(Side.AI))
            {
                continue;
            }

            if (best == null || card.Data.Damage > best.Data.Damage)
            {
                best = card;
            }
        }

        if (best != null)
        {
            PlayCard(best);
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
        CardInstance best = null;
        int bestScore = 0;   // never take an attack scored as a net loss

        foreach (CardInstance candidate in playerActive.Cards)
        {
            // Scoring lives in CombatResolver so the AI is judged by the same rules the
            // player plays under, retaliation included. The old version applied two
            // ungraded heuristics, so the LAST matching card won rather than the best
            // one, and it ignored MoraleCost entirely — the actual win condition.
            int score = CombatResolver.Score(attacker, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best == null)
        {
            Debug.Log(attacker.Data.DisplayName + " holds back — no attack worth making");
        }

        return best;
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

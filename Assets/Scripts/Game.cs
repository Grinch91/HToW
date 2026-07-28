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

    [Tooltip("Seconds a win/lose message stays on screen before returning to the main menu.")]
    public float endGameMessageSeconds = 5.0f;

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

    Sprite cardBack;
    bool matchStarted;
    float aiThinkTimer;
    float endGameTimer;
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

        turns.TurnStarted += OnTurnStarted;
        turns.TurnEnded += OnTurnEnded;

        BuildDeck(playerDeck, ResolveDeck(playerDeckData, "DeckData/Celtic"), Side.Player);
        BuildDeck(aiDeck, ResolveDeck(aiDeckData, "DeckData/Viking"), Side.AI);
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

        CardInstance card = pile.DrawTop();
        if (card == null)
        {
            Debug.Log(side + " deck is empty");
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
            card.View.SetFace(card.Data.Art);
        }
        else
        {
            CreateView(card, board, faceDown: false);
        }

        hand.LayOut();
        board.LayOut();

        Debug.Log(side + " played " + card.Data.DisplayName);
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
        Debug.Log(attacker.Data.DisplayName + " attacks " + target.Data.DisplayName);

        // Combat is still one-directional: the defender deals no damage back. That is a
        // design question, not an oversight — see Docs/Decisions.md Q-01.
        if (target.TakeDamage(attacker.Data.Damage))
        {
            Destroy(target);
        }
    }

    void Destroy(CardInstance card)
    {
        CardZone board = card.Owner == Side.Player ? playerActive : aiActive;
        board.Remove(card);

        // Morale is lost by the card's OWNER, in proportion to how valuable it was.
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

        selection.Forget(card);
        DestroyView(card);
        board.LayOut();

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
            endGameTimer = 0.0f;
        }
        else if (enemyInstance.enemyMorale <= 0)
        {
            turns.EndMatch();
            ShowOnly(PlayerWins);
            endGameTimer = 0.0f;
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
        int bestScore = int.MinValue;

        foreach (CardInstance candidate in playerActive.Cards)
        {
            // Prefer a kill, weighted by the morale it costs the player; otherwise
            // prefer removing the biggest threat. The old version applied two rules
            // with no scoring, so the LAST matching card won rather than the best one,
            // and it ignored morale entirely — the actual win condition.
            int score = candidate.CurrentHp <= attacker.Data.Damage
                ? 100 + (candidate.Data.MoraleCost * 10)
                : candidate.Data.Damage;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
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
            endGameTimer += Time.deltaTime;
            if (endGameTimer >= endGameMessageSeconds)
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

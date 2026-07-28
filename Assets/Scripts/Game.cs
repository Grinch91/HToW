using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

//Main class
public class Game : MonoBehaviour {
	//Instances
	#region
	public Deck playerDeck =  new Deck();
	public Deck aiDeck = new Deck();
	public Deck playerActive = new Deck();
	public Deck aiActive = new Deck();
	public Deck playerHand = new Deck();
	public Deck aiHand = new Deck();

	public Player playerInstance = new Player();
	public Enemy enemyInstance = new Enemy();
	#endregion


	//Variables
	#region
	public Texture btn;
	bool aiDeckEmpty;
	bool playerDeckEmpty;

	[Tooltip("Seconds the AI pauses before acting, so the player can follow what it does.")]
	public float aiThinkSeconds = 1.5f;

	[Tooltip("Seconds a win/lose message stays on screen before returning to the main menu.")]
	public float endGameMessageSeconds = 5.0f;

	public float tempX = 2.0f;
	public float tempY = 3.0f;
	public int i = 0;
	public List<CardDef> userlist = new List<CardDef>();
	public List<CardDef> enemylist = new List<CardDef>();

	GameObject PlayerWins;
	GameObject PlayerTurn;
	GameObject AIWins;
	GameObject AITurn;

	string playerFile;
	string enemyFile;
	#endregion


	//Turn state
	#region

	// Owns turn order and phase. See Assets/Scripts/Battle/TurnController.cs.
	readonly TurnController turns = new TurnController();

	bool matchStarted;
	float aiThinkTimer;
	float endGameTimer;
	#endregion


	//Start function
	#region

	// Use this for initialization
	void Start () 
	{
		/*Right now this will do nothing as player/enemyDeck need to be set in a menu
		playerFile = playerInstance.playerDeck;
		enemyFile = enemyInstance.enemyDeck;
		*/
		playerFile = "celtic.txt";
		enemyFile = "viking.txt";
		//deckInstance.Initialize();
		playerDeck.Initialize();
		aiDeck.Initialize();
		// Morale was previously forced to 1 here "for testing purposes", which meant a
		// single card death ended the match. Left alone for now: the starting values come
		// from BaseCharacter and are corrected in Milestone 3 along with the HUD.
		PlayerWins = this.transform.Find("MessagePlayerWin").gameObject;
		PlayerTurn = this.transform.Find("MessagePlayerTurn").gameObject;
		AIWins = this.transform.Find("MessageAIWin").gameObject;
		AITurn = this.transform.Find("MessageAITurn").gameObject;
		HideAllMessages();

		turns.TurnStarted += OnTurnStarted;
		turns.TurnEnded += OnTurnEnded;

		Debug.Log ("Building Player deck");
		BuildDeck(playerDeck, playerFile);
		Debug.Log ("Building Enemy deck");
		BuildDeck(aiDeck, enemyFile);
	}

	void OnDestroy()
	{
		turns.TurnStarted -= OnTurnStarted;
		turns.TurnEnded -= OnTurnEnded;
	}
	#endregion


	//BuildDeck Function
	#region
	// Previously took only a file name and used m_state to decide which deck to fill,
	// using the state machine as an argument-passing channel. Now the target is explicit.
	void BuildDeck(Deck deck, string file)
	{
		deck.ClearDeck();
		deck.Load(file);
		deck.Shuffle();
	}
	#endregion


	//Button Related functions
	#region

	//Displays message objects depending on certain requirments.
	// Exactly one message object is visible at a time, so every path goes through here.
	// The old ShowMessage() took a magic string, mixed display with scene loading, and
	// incremented messageTimer while resetting a different field (`timer`) — so its
	// timeout never actually elapsed. Display and timing are now separate concerns.
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

	// The turn banners existed in the scene from the start but were only ever hidden,
	// never shown: ShowMessage's only caller passed win/lose strings. The player got no
	// turn feedback at all. See Docs/KnownBugs.md M6.
	void ShowTurnMessage(Side side)
	{
		ShowOnly(side == Side.Player ? PlayerTurn : AITurn);
	}

	public void OnButton(string message)
	{
		switch(message)
		{
		case "EndTurn":
			// The button previously set a `buttonPushed` field that nothing ever read,
			// so End Turn did nothing at all. See Docs/KnownBugs.md C3.
			if(turns.ActiveSide == Side.Player && turns.Phase == TurnPhase.Main)
			{
				Debug.Log("Player ended their turn");
				turns.RequestEndTurn();
			}
			break;

		}

	}
	#endregion


	//OnCardHandClick function OnCardHandClick()
	#region

	public void OnCardHandClick(string cardName)
	{
		Debug.Log ("You have clicked a card");
		string[] temp = new string[1];
		//search for cardname
		#region
		if(cardName == "Battle Chariot")
		{
			temp[0] = "1";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "Ceithern")
		{
			temp[0] = "2";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "Fianna")
		{
			temp[0] = "3";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "Raider")
		{
			temp[0] = "4";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "Bondi")
		{
			temp[0] = "5";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "Huscarl")
		{
			temp[0] = "6";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "MercArcher")
		{
			temp[0] = "7";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "Sellsword")
		{
			temp[0] = "8";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		if(cardName == "Captain")
		{
			temp[0] = "9";
			playerActive.AddToDeck(temp);
			AddToActivePlayer();
		}
		#endregion
		
		//obj.transform.parent = playerActive.transform;
	}
	#endregion

	//AI related actions here
	//SelectCardAI()
	#region
	void SelectCardAI()
	{
		//temp card variable used for storage
		CardDef temp = new CardDef("null",0,0,0,0,"null");
		bool runOnce = false;
		string[] selectedCardTemp = new string[1];
		if(aiDeck.deck.Count > 0 && runOnce == false)
		{
			//loops through entire deck selecting the best card it can
			for(int count = 0; count < aiDeck.deck.Count; count++)
			{
				
				print("Count is: "+count);
				if(aiDeck.deck[count].Dmg > temp.Dmg && aiDeck.deck[count].Cost <= enemyInstance.enemySupply)
				{
					temp = aiDeck.deck[count];
					Debug.Log("Temp.dmg is now");
					Debug.Log(temp.Dmg);
					Debug.Log(temp.Name);
					//setting information to be passed to AddToDeck()
					#region
					if(temp.Name == "Battle Chariot")
					{	
						selectedCardTemp[0] = "1";
					}
					if(temp.Name == "Ceithern")
					{
						selectedCardTemp[0] = "2";
					}
					if(temp.Name == "Fianna")
					{
						selectedCardTemp[0] = "3";
					}
					if(temp.Name == "Raider")
					{
						selectedCardTemp[0] = "4";
					}
					if(temp.Name == "Bondi")
					{
						selectedCardTemp[0] = "5";
					}
					if(temp.Name == "Huscarl")
					{
						selectedCardTemp[0] = "6";
					}
					if(temp.Name == "MercArcher")
					{
						selectedCardTemp[0] = "7";
					}
					if(temp.Name == "Sellsword")
					{
						selectedCardTemp[0] = "8";
					}
					if(temp.Name == "Captain")
					{
						selectedCardTemp[0] = "9";
					}
					#endregion
				}
			}
			runOnce = true;
			Debug.Log("What is in cardTemp");
			Debug.Log (selectedCardTemp[0]);
			Debug.Log("Add To Deck");
			aiActive.AddToDeck(selectedCardTemp);
			
			Debug.Log("Add To Active AI");
			AddToActiveAi();

		}
	}

	#endregion


	//Battle() and Attack()
	#region
	void Battle(Side attackingSide)
	{
		//temp variables used to store attacker/target
		CardDef attacker = new CardDef("null",0,0,0,0,"null");
		CardDef target = new CardDef("null",0,0,0,0,"null");

		//find attacker/target
		attacker = AttackingCard(attacker, attackingSide);
		target = TargetCard(target, attacker, attackingSide);
		if(attacker.Name != "null" && target.Name != "null")
		{
			Debug.Log ("Entering attack: " +attacker.Name +" is attacking "+target.Name);
			Attack(attacker,target,attackingSide);
		}
	}

	//Method for selecting the attackingCard
	public CardDef AttackingCard(CardDef selectAttacker, Side attackingSide)
	{
		//If AI scans through every card in the active area selecting the best choice
		if(attackingSide == Side.AI)
		{
			foreach(CardDef card in aiActive.deck)
			{
				if(card.Dmg > selectAttacker.Dmg)
				{
					selectAttacker = card;
				}
			}
		}
		//if player select attacker is set to be which ever card selected
		if(attackingSide == Side.Player)
		{
			BattleController selection = PlayerSelection();
			if(selection != null && selection.attacker != null)
			{
				selectAttacker = selection.attacker;
			}
		}

		return selectAttacker;
	}

	//Method for selecting the TargetCard
	public CardDef TargetCard(CardDef selectTarget, CardDef attacker, Side attackingSide)
	{
		//If AI scans through every card in the active area selecting the best choice
		if(attackingSide == Side.AI)
		{
			foreach(CardDef card in playerActive.deck)
			{
				if(card.Hp < attacker.Dmg)
				{
					selectTarget = card;
				}
				if(card.Hp > attacker.Dmg && card.Dmg > attacker.Dmg)
				{
					selectTarget = card;
				}
			}
		}
		//if player select attacker is set to be which ever card selected
		if(attackingSide == Side.Player)
		{
			BattleController selection = PlayerSelection();
			if(selection != null && selection.target != null)
			{
				selectTarget = selection.target;
			}
		}
		return selectTarget;
	}

	// STOPGAP for Milestone 1 only. The original code called
	// playerActive.GetComponent<BattleController>() and dereferenced the result
	// directly. No BattleController is attached to the Active-Player object — the
	// component only ever exists on individual card objects, which are its *children* —
	// so this threw a NullReferenceException on the very first attack. That crash is
	// visible in the recorded 2014 log. See Docs/KnownBugs.md C1.
	//
	// Returning null instead of throwing lets the turn system be observed and verified.
	// It does NOT fix selection: the player still cannot choose an attacker or target,
	// so player attacks simply do not resolve. The real fix is a proper selection
	// service in Milestone 2, which is also where card identity stops being a string.
	BattleController PlayerSelection()
	{
		return playerActive.GetComponent<BattleController>();
	}

	//Attack method, takes the attacker and target variables
	void Attack(CardDef attacker,CardDef target,Side attackingSide)
	{
		//Reduces target health by the damage of the attacker, then checks if health is less than or is 0
		target.Hp = target.Hp - attacker.Dmg;
		if(target.Hp <=0)
		{
			//If AI destroys player card, removing it and reduces their morale.
			if(attackingSide == Side.AI)
			{
				playerInstance.playerMorale = playerInstance.playerMorale - target.MoraleCost;
				foreach(CardDef card in playerActive.deck)
				{
					if(target.Name == card.Name)
					{
						playerActive.deck.Remove(card);
						CheckForWinner();
						break;
					}
				}
			}
			//If player destroys AI card, removing it and reduces their morale.
			if(attackingSide == Side.Player)
			{
				enemyInstance.enemyMorale = enemyInstance.enemyMorale - target.MoraleCost;
				foreach(CardDef card in aiActive.deck)
				{
					if(target.Name == card.Name)
					{
						aiActive.deck.Remove(card);
						Destroy(GameObject.FindGameObjectWithTag(target.Name));
						CheckForWinner();
						break;
					}
				}

			}
		}
	}
	#endregion


	//Adding cards to the active area from hand AddToActivePlayer/AddToActiveAi
	//Creates an object with various components such as colliders, controllers, position, etc.
	#region
	void AddToActivePlayer()
	{
		CardDef c1 = playerActive.Deal();
		//Checks to ensure
		if(c1 != null)
		{
			GameObject newObj = new GameObject();
			newObj.name = c1.Name;
			newObj.tag = c1.Name;
			CardAttributes newCard = newObj.AddComponent<CardAttributes>();
			BoxCollider bc = newObj.AddComponent<BoxCollider>();
			bc.size = new Vector3(tempX,tempY,1);
			BattleController battle = newObj.AddComponent<BattleController>();
			SpriteRenderer ren = newObj.AddComponent<SpriteRenderer>();
			newCard.Data = c1;
			newObj.transform.parent = playerActive.transform;
			string store =  newCard.Data.Front;
			Sprite mySprite = Resources.Load<Sprite>(store)as Sprite;
			float x = -10+(playerDeck.deck.Count)*2.0f;
			float y = -3.0f;
			ren.sprite = mySprite;
			Vector2 pos = new Vector2(x,y);			
			newObj.transform.position = pos;
			Vector3 scale = new Vector3(0.75f,0.75f,1.0f);
			newObj.transform.localScale = scale;
			userlist.Add(c1);			
			playerActive.deck.Add(c1);
			i++;
		}
		
	}

	void AddToActiveAi()
	{
		CardDef c1 = aiActive.Deal();
		if(c1 != null)
		{
			GameObject newObj = new GameObject();
			newObj.name = c1.Name;
			newObj.tag = c1.Name;
			CardAttributes newCard = newObj.AddComponent<CardAttributes>();
			BoxCollider bc = newObj.AddComponent<BoxCollider>();
			bc.size = new Vector3(tempX,tempY,1);
			BattleController mc = newObj.AddComponent<BattleController>();
			SpriteRenderer ren = newObj.AddComponent<SpriteRenderer>();
			newCard.Data = c1;
			newCard.Data.IsEnemy = true;
			newObj.transform.parent = aiActive.transform;
			string store = newCard.Data.Front;
			Sprite mySprite = Resources.Load<Sprite>(store)as Sprite;
			float x = -10+(aiDeck.deck.Count)*2.0f;
			float y = 3.0f;
			ren.sprite = mySprite;
			Vector2 pos = new Vector2(x,y);			
			newObj.transform.position = pos;
			Vector3 scale = new Vector3(0.75f,0.75f,1.0f);
			newObj.transform.localScale = scale;
			enemylist.Add(c1);			
			aiActive.deck.Add(c1);
			i++;
		}
	}

	#endregion


	//Adding cards to hands
	#region

	//Creates an object with various components such as colliders, controllers, position, etc.
	void AddToPlayerHand()
	{
		CardDef c1 = playerDeck.Deal();
		//Checks to ensure playerDeck is not empty by seeing if c1 is null
		if(c1 != null)
		{
			GameObject newObj = new GameObject();
			newObj.name = c1.Name;
			CardAttributes newCard = newObj.AddComponent<CardAttributes>();
			BoxCollider bc = newObj.AddComponent<BoxCollider>();
			bc.size = new Vector3(tempX,tempY,1);
			MouseController mc = newObj.AddComponent<MouseController>();
			SpriteRenderer ren = newObj.AddComponent<SpriteRenderer>();
			newCard.Data = c1;
			newObj.transform.parent = playerHand.transform;
			string store = newCard.Data.Front;
			Sprite mySprite = Resources.Load<Sprite>(store)as Sprite;
			float x = -8+(playerDeck.deck.Count)*3.0f;
			float y = -14;
			ren.sprite = mySprite;
			Vector2 pos = new Vector2(x,y);
			Vector3 scale = new Vector3(0.75f,0.75f,1.0f);
			newObj.transform.localScale = scale;
			newObj.transform.position = pos;
			userlist.Add(c1);			
			playerHand.deck.Add(c1);
			i++;
		}

	}

	void AddToEnemyHand()
	{
		CardDef c1 = aiDeck.Deal();
		if(c1 != null)
		{
			GameObject newObj = new GameObject();
			newObj.name = c1.Name;
			CardAttributes newCard = newObj.AddComponent<CardAttributes>();
			BoxCollider bc = newObj.AddComponent<BoxCollider>();
			bc.size = new Vector3(tempX,tempY,1);
			SpriteRenderer ren = newObj.AddComponent<SpriteRenderer>();
			newCard.Data = c1;
			newCard.Data.IsEnemy = true;
			newObj.transform.parent = aiHand.transform;
			string store = newCard.Data.Back;
			Sprite mySprite = Resources.Load<Sprite>(store)as Sprite;
			float x = -11.0f+(playerDeck.deck.Count)*3.0f;
			float y = 14;
			ren.sprite = mySprite;
			Vector2 pos = new Vector2(x,y);
			Vector3 scale = new Vector3(0.75f,0.75f,1.0f);
			newObj.transform.localScale = scale;
			/*This is how to access the CardAttributes from the object SO YOU KNOW NIALL
			Debug.Log(newObj.GetComponent<CardAttributes>().Data.Hp);*/
			newObj.transform.position = pos;
			newObj.transform.RotateAround(pos,transform.right,180f);
			enemylist.Add(c1);			
			aiHand.deck.Add(c1);
			i++;
		}

	}
	#endregion


	//Turn phase handlers
	#region

	// Start of turn: draw, gain supply, and refresh units so they may attack again.
	void OnTurnStarted(Side side)
	{
		Debug.Log("Turn " + turns.TurnNumber + " - " + side + " to act");
		ShowTurnMessage(side);
		DrawForTurn(side);
		GainSupply(side);
		RefreshUnits(side);
		aiThinkTimer = 0.0f;
	}

	// End of turn: the active side's units attack, then control passes over.
	void OnTurnEnded(Side side)
	{
		ResolveAttacks(side);
		CheckForWinner();
	}

	void DrawForTurn(Side side)
	{
		CheckIsDeckEmpty(side);
		if(side == Side.Player)
		{
			if(playerDeckEmpty == false) AddToPlayerHand();
		}
		else
		{
			if(aiDeckEmpty == false) AddToEnemyHand();
		}
	}

	void GainSupply(Side side)
	{
		// Supply was previously granted inside EndTurn() to whichever side was *about to*
		// act, which made it hard to reason about. It is now granted to the side whose
		// turn is beginning. Note nothing spends supply yet — see Docs/KnownBugs.md C4,
		// fixed in Milestone 3.
		if(side == Side.Player)
		{
			if(playerInstance.playerSupply < playerInstance.MaxSupply)
			{
				playerInstance.playerSupply++;
			}
		}
		else
		{
			if(enemyInstance.enemySupply < enemyInstance.MaxSupply)
			{
				enemyInstance.enemySupply++;
			}
		}
	}

	// HasAttacked was set to true and never cleared, so once turns actually worked every
	// unit would have become a one-shot. See Docs/KnownBugs.md F2 — a latent bug that
	// this milestone would otherwise have activated.
	void RefreshUnits(Side side)
	{
		List<CardDef> units = side == Side.Player ? playerActive.deck : aiActive.deck;
		foreach(CardDef card in units)
		{
			card.HasAttacked = false;
		}
	}

	void ResolveAttacks(Side side)
	{
		List<CardDef> attackers = side == Side.Player ? playerActive.deck : aiActive.deck;
		List<CardDef> defenders = side == Side.Player ? aiActive.deck : playerActive.deck;

		// Iterate a snapshot: Attack() removes destroyed cards from the defending list,
		// and future combat features (retaliation, area effects) will mutate this one
		// too. See Docs/KnownBugs.md F1.
		foreach(CardDef card in new List<CardDef>(attackers))
		{
			if(card.HasAttacked == false && defenders.Count > 0)
			{
				Battle(side);
				card.HasAttacked = true;
			}
		}
	}
	#endregion


	//Check deck is not empty CheckIsDeckEmpty()
	#region
	public void CheckIsDeckEmpty(Side side)
	{
		if(side == Side.AI)
		{
			aiDeckEmpty = aiDeck.deck.Count == 0;
		}
		else
		{
			playerDeckEmpty = playerDeck.deck.Count == 0;
		}
	}
	#endregion

	//Match setup
	#region

	//Initial Setup function adds a number of cards to either the enemy or player hand at start up
	void InitialSetUp()
	{
		for(int card = 0; card < 5; card++)
		{
			// The original guards were `if (deck.Count != null)` on an int, which is
			// always true. Deal() would then index an empty list. See Docs/KnownBugs.md C6.
			if(aiDeck.deck.Count > 0)
			{
				AddToEnemyHand();
			}
			if(playerDeck.deck.Count > 0)
			{
				AddToPlayerHand();
			}
		}
		turns.BeginMatch(FlipCoin());
	}

	Side FlipCoin()
	{
		Side first = Random.Range(0, 2) == 0 ? Side.AI : Side.Player;
		Debug.Log(first + " goes first");
		return first;
	}
	#endregion

	//Check to see if AI or Player Wins CheckForWinner()
	#region
	public void CheckForWinner()
	{
		if(turns.Phase == TurnPhase.GameOver) return;

		if(playerInstance.playerMorale <= 0)
		{
			turns.EndMatch();
			ShowOnly(AIWins);
			endGameTimer = 0.0f;
		}
		else if(enemyInstance.enemyMorale <= 0)
		{
			turns.EndMatch();
			ShowOnly(PlayerWins);
			endGameTimer = 0.0f;
		}
	}
	#endregion

	//Update()
	#region
	void Update ()
	{
		if(matchStarted == false)
		{
			InitialSetUp();
			matchStarted = true;
			Debug.Log ("Initial Set up done.");
		}

		if(turns.Phase == TurnPhase.GameOver)
		{
			endGameTimer += Time.deltaTime;
			if(endGameTimer >= endGameMessageSeconds)
			{
				SceneManager.LoadScene("MainMenu");
			}
			return;
		}

		// The AI is the only side that acts without player input, so it is the only side
		// that needs a timer. It requests its own end of turn when finished.
		if(turns.ActiveSide == Side.AI && turns.Phase == TurnPhase.Main)
		{
			aiThinkTimer += Time.deltaTime;
			if(aiThinkTimer >= aiThinkSeconds)
			{
				SelectCardAI();
				turns.RequestEndTurn();
			}
		}

		// Drain every transition that is ready. This terminates: TurnPhase.Main does not
		// advance unless an end-turn has been requested, so a player turn parks here and
		// waits for the End Turn button. That wait is what this milestone adds.
		while(turns.Advance())
		{
		}
	}
	#endregion
}

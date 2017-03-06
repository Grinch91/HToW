using UnityEngine;
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
	bool justChanged;
	bool playerTurnOver;
	bool buttonPushed = false;

	public float timer = 0.0f;
	public float timerMax = 5.0f;
	public float messageTimer = 0.0f;

	public float tempX = 2.0f;
	public float tempY = 3.0f;
	public int n;
	public int i = 0;
	public int seperatingCardsAI = -7;
	public int seperatingCardsPlayer = -7;
	public List<CardDef> userlist = new List<CardDef>();
	public List<CardDef> enemylist = new List<CardDef>();

	GameObject PlayerWins;
	GameObject PlayerTurn;
	GameObject AIWins;
	GameObject AITurn;

	string playerFile;
	string enemyFile;

	const float FlyTime = 0.5f;

	bool playerFirst = true;
	#endregion


	//GameState
	#region

	enum GameState
	{
		Initial,
		Begin,
		Pending,
		Resolved,
		PlayerTurn,
		AITurn,
		Pause,
		PlayerWin,
		AIWin
	};

	GameState m_state;

	GameObject [] Buttons;
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
		//For testing purposes
		playerInstance.playerMorale = 1;
		enemyInstance.enemyMorale = 1;
		PlayerWins = this.transform.Find("MessagePlayerWin").gameObject;
		PlayerTurn = this.transform.Find("MessagePlayerTurn").gameObject;
		AIWins = this.transform.Find("MessageAIWin").gameObject;
		AITurn = this.transform.Find("MessageAITurn").gameObject;
		PlayerWins.SetActive(false);
		PlayerTurn.SetActive(false);
		AIWins.SetActive(false);
		AITurn.SetActive(false);
		Buttons =  new GameObject[1];
		Buttons[0] = this.transform.Find("Button1").gameObject;
		m_state = GameState.PlayerTurn;
		Debug.Log ("Building Player deck");
		BuildDeck(playerFile);
		m_state = GameState.AITurn;
		Debug.Log ("Building Enemy deck");
		BuildDeck(enemyFile);
		m_state = GameState.Initial;
		Debug.Log("Player Supply is");
		Debug.Log (playerInstance.playerSupply);

	}
	#endregion


	//BuildDeck Function
	#region
	void BuildDeck(string file)
	{
		if(m_state == GameState.PlayerTurn)
		{
			playerDeck.ClearDeck();
			playerDeck.Load(file);
			playerDeck.Shuffle();
		}
		else if(m_state == GameState.AITurn)
		{
			aiDeck.ClearDeck();
			aiDeck.Load(file);
			aiDeck.Shuffle();
		}
	}
	#endregion


	//Button Related functions
	#region

	//Displays message objects depending on certain requirments.
	//Used to trigger messages for user
	void ShowMessage(string msg)
	{
		messageTimer += Time.deltaTime;
		if (msg == "AI wins")
		{
			PlayerWins.SetActive(false);
			PlayerTurn.SetActive(false);
			AIWins.SetActive(true);
			AITurn.SetActive(false);
			if(messageTimer >= timerMax)
			{
				Application.LoadLevel("MainMenu");
			}
			timer=0.0f;
		}		
		else if (msg == "AI Turn")
		{
			PlayerWins.SetActive(false);
			PlayerTurn.SetActive(false);
			AIWins.SetActive(false);
			AITurn.SetActive(true);
		}
		else if (msg == "Player wins")
		{
			PlayerWins.SetActive(true);
			PlayerTurn.SetActive(false);
			AIWins.SetActive(false);
			AITurn.SetActive(false);
			if(messageTimer >= timerMax)
			{
				Application.LoadLevel("MainMenu");
			}
			timer=0.0f;
		}
		else if (msg == "Player Turn")
		{
			PlayerWins.SetActive(false);
			PlayerTurn.SetActive(true);
			AIWins.SetActive(false);
			AITurn.SetActive(false);
		}
	}

	public void OnButton(string message)
	{
		Debug.Log("Test:" + message);
		switch(message)
		{
		case "EndTurn":
			Debug.Log ("You have pushed the button");
			buttonPushed = true;
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
	void Battle()
	{
		//temp variables used to store attacker/target
		CardDef attacker = new CardDef("null",0,0,0,0,"null");
		CardDef target = new CardDef("null",0,0,0,0,"null");
		
		//find attacker/target
		attacker = AttackingCard(attacker);
		target = TargetCard(target, attacker);
		Debug.Log ("display target/attacker names");
		Debug.Log (target.Name);
		Debug.Log (attacker.Name);
		if(attacker.Name != "null" && target.Name != "null")
		{
			Debug.Log ("Entering attack: " +attacker.Name +"is attacking "+target.Name);
			Attack(attacker,target);
		}
	}

	//Method for selecting the attackingCard
	public CardDef AttackingCard(CardDef selectAttacker)
	{
		//If AI scans through every card in the active area selecting the best choice
		if(m_state == GameState.AITurn)
		{
			foreach(CardDef card in aiActive.deck)
			{
				if(card.Dmg > selectAttacker.Dmg)
				{
					selectAttacker = card;
					Debug.Log ("SelectedCard Name for enemy attack is "+selectAttacker.Name);
				}
			}
		}
		//if player select attacker is set to be which ever card selected
		if(m_state == GameState.PlayerTurn && playerActive.GetComponent<BattleController>().attacker != null)
		{
			selectAttacker = playerActive.GetComponent<BattleController>().attacker;
		}
		
		return selectAttacker;
	}
	
	//Method for selecting the TargetCard
	public CardDef TargetCard(CardDef selectTarget, CardDef attacker)
	{
		//If AI scans through every card in the active area selecting the best choice
		if(m_state == GameState.AITurn)
		{
			foreach(CardDef card in playerActive.deck)
			{
				if(card.Hp < attacker.Dmg)
				{
					selectTarget = card;
					Debug.Log("Enemy's target card is "+selectTarget.Name);
				}
				if(card.Hp > attacker.Dmg && card.Dmg > attacker.Dmg)
				{
					selectTarget = card;
					Debug.Log("Enemy's target card is "+selectTarget.Name);
				}
			}
		}
		//if player select attacker is set to be which ever card selected
		if(m_state == GameState.PlayerTurn && playerActive.GetComponent<BattleController>().target != null)
		{
			selectTarget = playerActive.GetComponent<BattleController>().target;
		}
		return selectTarget;
	}

	//Attack method, takes the attacker and target variables
	void Attack(CardDef attacker,CardDef target)
	{
		//Reduces target health by the damage of the attacker, then checks if health is less than or is 0
		target.Hp = target.Hp - attacker.Dmg;
		if(target.Hp <=0)
		{
			//If AI destroys player card, removing it and reduces their morale.
			if(m_state == GameState.AITurn)
			{
				Debug.Log(target.Hp);
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
			if(m_state == GameState.PlayerTurn)
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
			seperatingCardsPlayer = seperatingCardsPlayer+3;
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
			seperatingCardsAI = seperatingCardsAI+3;
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


	//Turn related functions EnemyTurn(), UserTurn() Pending and Resolved
	#region
	//Extra precaution
	void EnemyTurn()
	{
		Debug.Log ("What is playerTurnOver currently at when entering enemy turn");
		Debug.Log (playerTurnOver);
		
		n = 0;
		Debug.Log("In enemy turn");
		//sets a timer so the turn is not completed instantly and the player can see what happens
		CheckIsDeckEmpty();
		timer += Time.deltaTime;
		if(timer >= timerMax)
		{
			do
			{
				if(aiDeckEmpty == false)
				{
					AddToEnemyHand();
					Debug.Log ("Added to enemy hand done.");
				}
				n++;
				Debug.Log("Into AddToEnemyActive");
				SelectCardAI();
			}while(n<1);
			timer = 0.0f;
		}
		
		//Checks through deck of the active area for each card so they can all attack.
		foreach(CardDef card in aiActive.deck)
		{
			if(card.HasAttacked != true && playerActive.deck.Count > 0)
			{
				Battle();
				card.HasAttacked = true;
			}
		}
		m_state = GameState.Resolved;
		Debug.Log(m_state);
		if(m_state == GameState.Resolved)
		{
			Debug.Log ("I have reached endTurn in playerTurn");
			EndTurn();
		}
		
	}

	void UserTurn()
	{
		Debug.Log ("What is playerTurnOver currently at when entering player turn");
		Debug.Log (playerTurnOver);
		if(playerTurnOver == false)
		{
			n = 0;
			Debug.Log("In User Turn");
			CheckIsDeckEmpty();
			do
			{
				if(playerDeckEmpty == false)
				{
					AddToPlayerHand();
					Debug.Log ("Added to player hand done.");
				}
				n++;
			}while(n<1);

			foreach(CardDef card in playerActive.deck)
			{
				if(card.HasAttacked != true && aiActive.deck.Count > 0)
				{
					Battle();
					card.HasAttacked = true;
				}
			}
			m_state = GameState.Resolved;
		
			//System.Threading.Thread.Sleep(5000);
			if(m_state == GameState.Resolved)
			{
				EndTurn();
			}
		}
	}
	#endregion

	//Ending turns EndTurn()
	#region
	void EndTurn()
	{
		justChanged = false;
		Debug.Log("Changing turn");
		m_state  = GameState.Begin;
		if(playerFirst == true && justChanged == false)
		{
			playerFirst = false;
			justChanged = true;
			playerTurnOver = true;
			if(playerInstance.playerSupply <10)
			{
				playerInstance.playerSupply = playerInstance.playerSupply + 1;
			}
		}
		if(playerFirst == false && justChanged == false)
		{
			playerFirst = true;
			justChanged = true;
			playerTurnOver = false;
			if(enemyInstance.enemySupply<10)
			{

				enemyInstance.enemySupply = enemyInstance.enemySupply + 1;
			}
		}

	}
	#endregion


	//Check deck is not empty CheckIsDeckEmpty()
	#region
	public void CheckIsDeckEmpty()
	{
		if(m_state == GameState.AITurn)
		{
			if(aiDeck.deck.Count != 0)
			{
				aiDeckEmpty = false;
			}else
			{
				aiDeckEmpty = true;
				Debug.Log("Deck is empty");
			}
		}
		if(m_state == GameState.PlayerTurn)
		{
			if(playerDeck.deck.Count != 0)
			{
				playerDeckEmpty = false;
			}else
			{
				playerDeckEmpty = true;
				Debug.Log("Deck is empty");
			}
		}
	}


	#endregion
	
	//Methods within the update statement, placed in call order.
	#region
	//Initial Setup function adds a number of cards to either the enemy or player hand at start up
	#region

	void InitialSetUp()
	{
		for(int i = 0; i<5;i++)
		{
			if(aiDeck.deck.Count != null)
			{
				AddToEnemyHand();
			}
			if(playerDeck.deck.Count != null)
			{
				AddToPlayerHand();
			}
		}
		FlipCoin();
		m_state = GameState.Begin;
	}

	void FlipCoin()
	{
		System.Random random = new System.Random();
		int num = random.Next(0,2);
		if(num == 0)
		{
			playerFirst = false;
			Debug.Log ("Player is not first");
		}
		else
		{
			playerFirst = true;
			Debug.Log ("Player is first");
		}
	}
	#endregion
	//Change current turn ChangeTurn()
	#region
	public void ChangeTurn()
	{
		if(playerFirst == false)
		{
			m_state = GameState.AITurn;
		}
		if(playerFirst == true)
		{
			m_state = GameState.PlayerTurn;
		}
	}
	#endregion
	
	//LaunchTurn()
	#region
	public void LaunchTurn()
	{
		if(m_state == GameState.AITurn)
		{
			playerTurnOver = true;
			EnemyTurn();
		}
		if(m_state == GameState.PlayerTurn)
		{
			playerTurnOver = false;
			UserTurn();
		}
	}
	#endregion
	
	//Check to see if AI or Player Wins CheckForWinner()
	#region
	public void CheckForWinner()
	{
		if(playerInstance.playerMorale <= 0)
		{
			m_state = GameState.AIWin;
			ShowMessage("AI wins");
		}
		if(enemyInstance.enemyMorale <=0)
		{
			m_state = GameState.PlayerWin;
			ShowMessage("Player wins");
		}
	}
	#endregion
	#endregion

	//Update()
	#region
	void Update () 
	{
		if(m_state == GameState.Initial)
		{
			InitialSetUp();
			Debug.Log ("Initial Set up done.");
		}
		if(m_state == GameState.Begin)
		{
			for(int x = 0; x<2; x ++)
			{
				Debug.Log(m_state);
				ChangeTurn();
				Debug.Log("LaunchTurnNow");
				Debug.Log(m_state);
				LaunchTurn();
				CheckForWinner();
			}
		}

	}
	#endregion
}

using UnityEngine;

//Base class that both player and enemy draw from
//Outlines what a character within the game can have and sets basic values for these.
public class BaseCharacter{

	private int startMorale;
	private int startSupply;
	private int maxSupply;
	private string currentDeck;

	public BaseCharacter()
	{
		startMorale = 30;
		startSupply = 2;
		maxSupply = 10;
		currentDeck = "";
	}


	//Use of getters and setters for attributes. Certain attributes only have getters as the base cannot be changed
	//To change these change within the instance of the character
	public int StartMorale
	{
		get{return startMorale;}
	}

	public int StartSupply
	{
		get{return startSupply;}
	}

	public int MaxSupply
	{
		get{return maxSupply;}
	}

	public string CurrentDeck
	{
		get{return currentDeck;}
		set{currentDeck = value;}
	}
}

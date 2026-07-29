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
		// 30 was measurably too low once retaliation landed. A deck totals around 100
		// morale, and since retaliation usually destroys both cards in an exchange, both
		// sides drained to zero within about ten turns — 28% to 51% of simulated matches
		// ended as mutual destruction rather than a win. At 50 a match runs long enough
		// for the supply curve to matter and for the result to be earned.
		// See Docs/Decisions.md D-12.
		startMorale = 50;
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

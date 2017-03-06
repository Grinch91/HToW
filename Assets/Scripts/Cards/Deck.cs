using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

//Deck class handling building, loading, shuffling and dealing cards from the a deck.
public class Deck:MonoBehaviour{

	//Defines that a deck is a list of cards
	public List<CardDef> deck = new List<CardDef>();
	public string[] fileContents;

	public virtual void Initialize()
	{
	}
	
	//Loading a deck contains via external files which they are saved as.
	#region
	public void Load(string file)
	{
		fileContents = System.IO.File.ReadAllLines(Application.dataPath + System.IO.Path.DirectorySeparatorChar + file);
		AddToDeck(fileContents);
	}
	#endregion

	//Method for clearing the deck
	#region
	public void ClearDeck()
	{
		deck.Clear();
	}
	#endregion
	
	//Method to add cards to a deck.
	#region
	public void AddToDeck(string[] myDeck)
	{
		//Using the contents from the Load method a deck is built based off the content from the file.
		foreach(string line in myDeck)
		{
			//Name, HP, DMG, COST, MORALE COST, IMAGE
			//Battle Chariot defined
			#region
			if(line == "1")
			{
				CardDef c = new CardDef("Battle Chariot", 4, 2, 1, 2, "battleChariot");
				deck.Add(c);
			}
			#endregion
			//Ceithern defined
			#region
			if(line == "2")
			{
				CardDef c = new CardDef("Ceithern", 8, 4, 3, 6, "ceithern");
				deck.Add(c);
			}
			#endregion
			//Fianna defined
			#region
			if(line == "3")
			{
				CardDef c = new CardDef("Fianna", 12, 12, 4, 10, "fianna");
				deck.Add(c);
			}
			#endregion
			//Raider defined
			#region
			if(line == "4")
			{
				CardDef c = new CardDef("Raider", 4, 4, 2, 4, "raider");
				deck.Add(c);
			}
			#endregion
			//Bondi defined
			#region
			if(line == "5")
			{
				CardDef c = new CardDef("Bondi", 8, 4, 4, 5, "bondi");
				deck.Add(c);
			}
			#endregion
			//Huscarl defined
			#region
			if(line == "6")
			{
				CardDef c = new CardDef("Huscarl", 12, 15, 6, 10, "huscarl");
				deck.Add(c);
			}
			#endregion
			//Merc archer defined
			#region
			if(line == "7")
			{
				CardDef c = new CardDef("MercArcher", 4, 2, 1, 1, "archer");
				deck.Add(c);
			}
			#endregion
			//Merc sellsword defined
			#region
			if(line == "8")
			{
				CardDef c = new CardDef("Sellsword", 4, 10, 3, 5, "sellsword");
				deck.Add(c);
			}
			#endregion
			//Merc Captain defined
			#region
			if(line == "9")
			{
				CardDef c = new CardDef("Captain", 15, 10, 6, 10, "captain");
				deck.Add(c);
			}
			#endregion
		}
	}
	#endregion

	//Shuffle Method
	#region
	public void Shuffle() 
	{
		// using Knuth Shuffle
		System.Random random = new System.Random();
		int j;
		
		for (int i = 0; i < deck.Count; i++){
			j = random.Next(deck.Count);
			CardDef tmp = deck[i];
			deck[i] = deck[j];
			deck[j] = tmp;
		}        
	}
	#endregion

	//Method for dealing a card, deals from top, removes top from deck, returns card
	#region
	public CardDef Deal()
	{
		int check = deck.Count;
		if(check != null)
		{
			CardDef returnCard = deck[0];
			deck.RemoveAt(0);
			return returnCard;
		}
		return null;
	}
	#endregion
}

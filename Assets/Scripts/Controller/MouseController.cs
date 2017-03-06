using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Controller used for selecting cards from hand and playing them to the active area.
public class MouseController : MonoBehaviour {

	private string temp;
	private Player playerTemp;
	private GameObject gameInstance;

	//When mouse is clicked on a collider
	void OnMouseDown()
	{
		//makes temp equal to the name of the object clicked.
		playerTemp = new Player();
		temp = gameObject.GetComponent<CardAttributes>().Data.Name;
		gameInstance = GameObject.Find("Game");
		//Check if card can be played
		if(playerTemp.playerSupply >= gameObject.GetComponent<CardAttributes>().Data.Cost)
		{
			gameInstance.GetComponent<Game>().OnCardHandClick(temp);
			Destroy(this.gameObject);
		}
		else
		{
			Debug.Log ("Nay");
		}


	}

}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Controller than handles selecting both the attacking card and the target card for the User
public class BattleController : MonoBehaviour {

	private GameObject go;
	private CardDef temp;
	public CardDef target;
	public CardDef attacker;

	//Runs once a collider has been clicked
	void OnMouseDown()
	{
		temp = gameObject.GetComponent<CardAttributes>().Data;
		go = GameObject.Find("Game");

		Debug.Log ("Mouse Down");
		//If variable IsEnemy is true then selected card is target
		if(temp.IsEnemy == true)
		{
			target = temp;
		}
		//If variable IsEnemy is false then selected card is attacker
		if(temp.IsEnemy == false)
		{
			attacker = temp;
		}
	}
}

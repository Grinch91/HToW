using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Class used to define what makes up a card
public class CardAttributes:MonoBehaviour
{
	public CardDef Data;
}
//Using System Serialisation to view attributes in unity inspector
[System.Serializable]
public class CardDef
{
	//Variables that make up what a card is.
	public string Name;
	public int Hp;
	public int Dmg;
	public int Cost;
	public int MoraleCost;
	public string Front;
	public string Back;
	public bool IsEnemy;
	public bool HasAttacked;

	//default constructor for a card
	public CardDef(string name, int hp, int dmg, int cost, int moraleCost, string front)
	{
		Name = name;
		Hp = hp;
		Dmg = dmg;
		Cost = cost;
		MoraleCost = moraleCost;
		Front = front;
		Back = "back";
		IsEnemy = false;
		HasAttacked = false;
	}
}

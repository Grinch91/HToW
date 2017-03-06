using UnityEngine;
using System.Collections;

//Script handling the Menu systel within the game
public class UI : MonoBehaviour {

	private delegate void GUIMethod(); 
	private GUIMethod currentGUIMethod;
	public GUIStyle mystyle;
	public Texture btn;
	public Texture banner;

	// Use this for initialization
	//Sets the first menu to be mainmenu
	void Start () {
		this.currentGUIMethod = MainMenu; 
	}

	void OnGUI()
	{
		this.currentGUIMethod(); 
	}

	//Handles displaying the correct information for MainMenu
	public void MainMenu()
	{

		GUI.Label(new Rect((Screen.width/4),-60,Screen.width, Screen.height), banner, mystyle);

		GUI.Label(new Rect((Screen.width/2-130),600,250,58),btn, mystyle);
		if(GUI.Button(new Rect((Screen.width/2-100),610,100,40), "Battle Mode", mystyle)) {
			Debug.Log("Loading Battle Menu");
			this.currentGUIMethod = BattleMenu;
		}

		GUI.Label(new Rect((Screen.width/2-130),680,250,58),btn, mystyle);
		if(GUI.Button(new Rect((Screen.width/2-100),690,100,40), "Cards", mystyle)) {
			Debug.Log("Loading Card View Menu");
			this.currentGUIMethod = CardMenu;
		}
		
		GUI.Label(new Rect((Screen.width/2-130),760,250,58),btn, mystyle);
		if(GUI.Button(new Rect((Screen.width/2-100),770,100,40), "Exit", mystyle)) {
			Debug.Log("Quiting Game");
			Application.Quit();
		}
	}
	
	//Handles displaying the correct information for CardMenu
	public void CardMenu()
	{
		// Make a background box
		GUI.Label(new Rect((Screen.width/4),-60,Screen.width, Screen.height), banner, mystyle);

		GUI.Label(new Rect((Screen.width/2-130),600,250,58),btn, mystyle);
		if(GUI.Button(new Rect((Screen.width/2-100),610,100,40), "Sorry N/A", mystyle)) {
		
		}

		GUI.Label(new Rect((Screen.width/2-130),680,250,58),btn, mystyle);
		if(GUI.Button(new Rect((Screen.width/2-100),690,100,40), "Main Menu", mystyle)) {
			Debug.Log("Returning to Main Menu");
			this.currentGUIMethod = MainMenu;
		}
	}

	//Handles displaying the correct information for BattleMenu
	public void BattleMenu()
	{
		bool checkValues = false;
		string enemy;
		string player;
		// Make a background box
		GUI.Label(new Rect((Screen.width/4),-60,Screen.width, Screen.height), banner, mystyle);

		
		GUI.Label(new Rect((Screen.width/2-130),600,250,58),btn, mystyle);
		if(GUI.Button(new Rect((Screen.width/2-100),610,100,40), "Load Battle", mystyle)) {
			Debug.Log("Loading Battle Scene");
			Application.LoadLevel("Battle");
		}

		GUI.Label(new Rect((Screen.width/2-130),680,250,58),btn, mystyle);
		if(GUI.Button(new Rect((Screen.width/2-100),690,100,40), "Main Menu", mystyle)) {
			Debug.Log("Returning to Main Menu");
			this.currentGUIMethod = MainMenu;
		}
	}
	
	/*Add in other menu cases here as they arise
	 public void MenuNameHere()
	 {
	 }*/

}
using UnityEngine;
using System.Collections;

public class MenuGUI : MonoBehaviour {

	void Start(){
		//Start code here
	}

	
	void OnGUI () {
		// Make a background box
		GUI.Box(new Rect(0,0,Screen.width, Screen.height), /*add in main menu background here Texture="",*/ "Hibernia: Tales of Warfare");
		
		// Make the first button. If it is pressed, Application.Loadlevel (1) will be executed
		if(GUI.Button(new Rect(20,40,80,20), "New Game")) {
			Debug.Log("Loading Level 1");
			//Application.LoadLevel("New");
		}
		
		// Make the second button.
		if(GUI.Button(new Rect(20,70,80,20), "Continue")) {
			Debug.Log("Restoring Player's Save progress");
			//Do code here Application.LoadLevel("Save");
		}

		if(GUI.Button(new Rect(20,100,80,20), "Battle Mode")) {
			Debug.Log("Loading Battle Menu");
			Application.LoadLevel("BattleMenu");
		}

		if(GUI.Button(new Rect(20,130,80,20), "Cards")) {
			Debug.Log("Loading Card View Menu");
			Application.LoadLevel("CardMenu");
		}

		// Make the second button.
		if(GUI.Button(new Rect(20,160,80,20), "Exit")) {
			Debug.Log("Quiting Game");
			Application.Quit();
		}
		
	}
}
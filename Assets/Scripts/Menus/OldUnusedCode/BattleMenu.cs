using UnityEngine;
using System.Collections;

public class BattleMenu : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}

	void OnGUI () {
		// Make a background box
		GUI.Box(new Rect(0,0,Screen.width, Screen.height), /*add in main menu background here Texture="",*/ "Hibernia: Tales of Warfare");
		
		// Make the first button. If it is pressed, Application.Loadlevel (1) will be executed
		if(GUI.Button(new Rect(20,40,80,20), "Test")) {
			Debug.Log("test");
			//Application.LoadLevel("New");
		}

		if(GUI.Button(new Rect(20,70,80,20), "Main Menu")) {
			Debug.Log("Returning to Main Menu");
			Application.LoadLevel("MainMenu");
		}
	}

	// Update is called once per frame
	void Update () {
	
	}
}

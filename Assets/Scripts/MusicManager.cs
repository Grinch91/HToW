using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour {

	AudioClip NewMusic; //Pick an audio track to play.
	private static MusicManager instance;

	void Awake ()
	{
		if(instance == null){
			instance = this;
		}
		else if(instance != null && instance.gameObject != this.gameObject){
			Destroy(this.gameObject);
			return;
		}

		GameObject go = new GameObject();
		go = GameObject.Find("song name here"); //Finds the game object called Game Music, if it goes by a different name, change this.
		go.audio.clip = NewMusic; //Replaces the old audio with the new one set in the inspector.
		go.audio.Play(); //Plays the audio.
	}
}

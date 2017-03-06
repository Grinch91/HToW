using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Controller used for testing features and displaying messages
public class GameButton : MonoBehaviour
{
	public string Message;

	void OnMouseDown()
	{
		transform.parent.gameObject.GetComponent<Game>().OnButton(Message);
	}
}

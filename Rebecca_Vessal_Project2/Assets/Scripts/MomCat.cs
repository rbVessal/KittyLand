using UnityEngine;
using System.Collections;

public class MomCat : MonoBehaviour 
{
	//The text mesh contains the text of the mom cat dialogue
	private TextMesh momCatDialogueTextMesh;
	
	// Use this for initialization
	void Start () 
	{
		momCatDialogueTextMesh = GameObject.FindGameObjectWithTag("Dialogue").GetComponent<TextMesh>();
		momCatDialogueTextMesh.text = "Help me find my lost kittens.\nCollide onto me to start.";
	}
	
	public void ChangeDialogueText()
	{
		momCatDialogueTextMesh.text = "Thank you for returning my lost kittens." + 
			"\nI have given you rainbow trail powers" + 
				"\nand a speed boost." + 
				"\nUse [SPACE] to use your speed boost.";
	}
}

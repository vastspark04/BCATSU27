using UnityEngine;
using UnityEngine.UI;

public class HUDDebugLabel : MonoBehaviour
{
	private Text debugText;

	private FlightAssist assist;

	private void Start()
	{
		debugText = GetComponent<Text>();
		assist = GetComponentInParent<FlightAssist>();
	}

	private void Update()
	{
		string text = "Lim pitchVel: " + assist.debug_gLimitedAngVel + "\nStick pitchVel: " + assist.debug_stickAngVel.x + "\nActual pitchVel: " + assist.debug_actualPitchVel;
		debugText.text = text;
	}
}

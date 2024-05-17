using Rewired;
using UnityEngine;

public class RewiredMouseTest : MonoBehaviour
{
	private float mouseX;

	private float mouseY;

	private float mouseScroll;

	private void Start()
	{
	}

	private void LateUpdate()
	{
		if (ReInput.isReady)
		{
			Player player = ReInput.players.GetPlayer(0);
			mouseX = player.GetAxis("Mouse X");
			mouseY = player.GetAxis("Mouse Y");
			mouseScroll = player.GetAxis("Mouse Scroll");
		}
	}

	private void OnGUI()
	{
		string text = $"Mouse X: {mouseX}\nMouse Y: {mouseY}\nMouse Scroll: {mouseScroll}";
		GUI.Label(new Rect(500f, 500f, 500f, 500f), text);
	}
}

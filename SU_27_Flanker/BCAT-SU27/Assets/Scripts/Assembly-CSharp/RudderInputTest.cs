using Rewired;
using UnityEngine;

public class RudderInputTest : MonoBehaviour
{
	public float moveRange = 100f;

	private float origX;

	private Player player;

	private void Start()
	{
		player = ReInput.players.GetPlayer(0);
		origX = base.transform.position.x;
	}

	private void Update()
	{
		float axis = player.GetAxis("Rudder");
		Vector3 position = base.transform.position;
		position.x = origX + axis * moveRange;
		base.transform.position = position;
	}

	private void OnGUI()
	{
		GUI.Label(text: "Rudder: " + player.GetAxis("Rudder"), position: new Rect(10f, 10f, 200f, 200f));
	}
}

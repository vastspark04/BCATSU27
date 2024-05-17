using UnityEngine;

public class TestInitializePlayer : MonoBehaviour
{
	public PlayerVehicleSetup playerVehicle;

	private void Start()
	{
		FlightSceneManager.instance.playerActor = playerVehicle.GetComponent<Actor>();
		playerVehicle.SetupForFlight();
	}
}

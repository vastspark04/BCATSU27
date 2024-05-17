using UnityEngine;

namespace MFlight.Demo{

public class Hud : MonoBehaviour
{
	[Header("Components")]
	[SerializeField]
	private MouseFlightController mouseFlight;

	[Header("HUD Elements")]
	[SerializeField]
	private RectTransform boresight;

	[SerializeField]
	private RectTransform mousePos;

	private Camera playerCam;

	private void Awake()
	{
		if (mouseFlight == null)
		{
			Debug.LogError(base.name + ": Hud - Mouse Flight Controller not assigned!");
		}
		playerCam = mouseFlight.GetComponentInChildren<Camera>();
		if (playerCam == null)
		{
			Debug.LogError(base.name + ": Hud - No camera found on assigned Mouse Flight Controller!");
		}
	}

	private void Update()
	{
		if (!(mouseFlight == null) && !(playerCam == null))
		{
			UpdateGraphics(mouseFlight);
		}
	}

	private void UpdateGraphics(MouseFlightController controller)
	{
		if (boresight != null)
		{
			boresight.position = playerCam.WorldToScreenPoint(controller.BoresightPos);
			boresight.gameObject.SetActive(boresight.position.z > 1f);
		}
		if (mousePos != null)
		{
			mousePos.position = playerCam.WorldToScreenPoint(controller.MouseAimPos);
			mousePos.gameObject.SetActive(mousePos.position.z > 1f);
		}
	}

	public void SetReferenceMouseFlight(MouseFlightController controller)
	{
		mouseFlight = controller;
	}
}}

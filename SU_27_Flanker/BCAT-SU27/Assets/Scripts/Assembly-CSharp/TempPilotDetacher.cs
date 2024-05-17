using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class TempPilotDetacher : MonoBehaviour
{
	public GameObject cameraRig;

	public GameObject pilotModel;

	public UnityEvent OnDetachPilot;

	public Vector3 detachOffset = Vector3.zero;

	private bool detached;

	private Material lineMat;

	public void DetachPilot(bool killBatts = true)
	{
		if (detached || !cameraRig)
		{
			return;
		}
		if (killBatts)
		{
			GetComponentInParent<FlightInfo>().GetComponentInChildren<Battery>().Kill();
		}
		detached = true;
		if ((bool)cameraRig.transform.parent)
		{
			cameraRig.transform.position += cameraRig.transform.parent.TransformVector(detachOffset);
		}
		cameraRig.transform.parent = null;
		cameraRig.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(cameraRig.transform.forward, Vector3.up));
		if (cameraRig.transform.position.y < WaterPhysics.instance.height)
		{
			Vector3 position = cameraRig.transform.position;
			position.y = WaterPhysics.instance.height;
			cameraRig.transform.position = position;
		}
		pilotModel.SetActive(value: false);
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if ((bool)controller.activeInteractable)
			{
				controller.ReleaseFromInteractable();
			}
		}
		float threshold = FloatingOriginShifter.instance.threshold;
		cameraRig.AddComponent<FloatingOriginShifter>().threshold = threshold;
		cameraRig.AddComponent<FloatingOriginTransform>();
		VRHandController[] componentsInChildren = cameraRig.GetComponentsInChildren<VRHandController>(includeInactive: true);
		foreach (VRHandController vRHandController in componentsInChildren)
		{
			if ((bool)vRHandController && !vRHandController.gameObject.GetComponent<VRTeleporter>())
			{
				VRTeleporter vRTeleporter = vRHandController.gameObject.AddComponent<VRTeleporter>();
				vRTeleporter.moveSpeed = 1080f;
				vRTeleporter.lineWidth = 0.1f;
				vRTeleporter.arcSpeed = 40f;
				vRTeleporter.lineMaterial = (lineMat = new Material(Shader.Find("Particles/MF-Additive")));
				vRTeleporter.lineMaterial.SetColor("_TintColor", Color.red);
			}
		}
		if (OnDetachPilot != null)
		{
			OnDetachPilot.Invoke();
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += OnExitScene;
		}
		if (VTOLMPUtils.IsMultiplayer() && (bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnLocalBriefingAvatarSpawned += Instance_OnLocalBriefingAvatarSpawned;
		}
	}

	private void Instance_OnLocalBriefingAvatarSpawned()
	{
		if ((bool)cameraRig)
		{
			Object.Destroy(cameraRig);
		}
	}

	private void OnDestroy()
	{
		if ((bool)lineMat)
		{
			Object.Destroy(lineMat);
		}
		if (VTOLMPUtils.IsMultiplayer() && (bool)cameraRig)
		{
			Object.Destroy(cameraRig);
		}
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnLocalBriefingAvatarSpawned -= Instance_OnLocalBriefingAvatarSpawned;
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
	}

	private void OnExitScene()
	{
		if ((bool)cameraRig)
		{
			Object.Destroy(cameraRig.transform.gameObject);
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
	}
}

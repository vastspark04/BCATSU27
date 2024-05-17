using System.Collections.Generic;
using UnityEngine;

public class ExternalCamManager : MonoBehaviour, IQSVehicleComponent
{
	public List<Camera> cameras = new List<Camera>();

	public int camIdx;

	public RenderTexture renderTexture;

	private bool camEnabled;

	public static ExternalCamManager instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		foreach (Camera camera in cameras)
		{
			camera.enabled = false;
		}
		camIdx--;
		NextCamera();
		SetCameraEnabled(_enabled: false);
	}

	public void AddCamera(Camera cam)
	{
		cam.enabled = false;
		cameras.Add(cam);
	}

	public void NextCamera()
	{
		for (int i = 0; i < cameras.Count; i++)
		{
			cameras[i].gameObject.SetActive(value: false);
		}
		camIdx = (camIdx + 1) % cameras.Count;
		cameras[camIdx].targetTexture = renderTexture;
		cameras[camIdx].gameObject.SetActive(camEnabled);
	}

	public void PrevCamera()
	{
		for (int i = 0; i < cameras.Count; i++)
		{
			cameras[i].gameObject.SetActive(value: false);
		}
		camIdx--;
		if (camIdx < 0)
		{
			camIdx = cameras.Count - 1;
		}
		cameras[camIdx].targetTexture = renderTexture;
		cameras[camIdx].gameObject.SetActive(camEnabled);
	}

	public void RemoveCamera(Camera cam)
	{
		int num = cameras.IndexOf(cam);
		if (num == camIdx)
		{
			PrevCamera();
		}
		cam.enabled = false;
		cameras.Remove(cam);
		if (camIdx > num)
		{
			camIdx--;
		}
	}

	public void SetCameraEnabled(bool _enabled)
	{
		camEnabled = _enabled;
		cameras[camIdx].gameObject.SetActive(_enabled);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode("ExternalCameras").SetValue("camIdx", camIdx);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("ExternalCameras");
		camIdx = node.GetValue<int>("camIdx");
		camIdx--;
		NextCamera();
	}
}

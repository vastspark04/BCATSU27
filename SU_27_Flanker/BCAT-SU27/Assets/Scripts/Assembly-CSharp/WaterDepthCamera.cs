using System;
using System.Collections;
using UnityEngine;

public class WaterDepthCamera : MonoBehaviour
{
	public static WaterDepthCamera instance;

	private Camera cam;

	public bool constantUpdate = true;

	public float maxOrthoSize = 20000f;

	public float minOrthoSize = 5000f;

	public float altitudeFloor = 2000f;

	public float altitudeMax = 6000f;

	private void Awake()
	{
		cam = GetComponent<Camera>();
		cam.enabled = false;
		instance = this;
	}

	private void Start()
	{
		FloatingOrigin.instance.OnOriginShift += Instance_OnOriginShift;
	}

	private void LateUpdate()
	{
		if (constantUpdate)
		{
			_Render();
		}
	}

	private void Instance_OnOriginShift(Vector3 offset)
	{
		if (!constantUpdate)
		{
			StartCoroutine(RenderRoutine());
		}
	}

	private IEnumerator RenderRoutine()
	{
		for (int i = 0; i < 2; i++)
		{
			yield return null;
		}
		yield return new WaitForEndOfFrame();
		_Render();
	}

	public void Render()
	{
		StartCoroutine(RenderAtEndOfFrame());
	}

	private IEnumerator RenderAtEndOfFrame()
	{
		yield return new WaitForEndOfFrame();
		_Render();
	}

	private void _Render()
	{
		float altitude = WaterPhysics.GetAltitude(VRHead.position);
		float num = Mathf.Lerp(minOrthoSize - altitudeFloor, maxOrthoSize, (altitude + altitudeFloor) / altitudeMax);
		Shader.SetGlobalFloat("_WaterDepthOrthoSize", num);
		cam.orthographicSize = num;
		float num2 = 2f * num / (float)cam.targetTexture.width;
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(Vector3.zero);
		Vector3D vector3D2 = vector3D;
		vector3D /= num2;
		vector3D.x = Math.Round(vector3D.x);
		vector3D.z = Math.Round(vector3D.z);
		vector3D *= num2;
		Vector3 vector = VTMapManager.GlobalToWorldPoint(vector3D);
		Vector3 vector2 = FloatingOrigin.accumOffset.toVector3 + (vector3D - vector3D2).toVector3;
		Shader.SetGlobalVector("_WaterDepthLastRenderPos", new Vector4(vector2.x, vector2.y, vector2.z, 1f));
		base.transform.position = new Vector3(vector.x, WaterPhysics.instance.height + 10000f, vector.z);
		base.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
		cam.Render();
	}
}

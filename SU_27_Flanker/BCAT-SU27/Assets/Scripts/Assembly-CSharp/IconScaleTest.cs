using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class IconScaleTest : MonoBehaviour
{
	public static float IconScaleMultiplier = 1f;

	public float scale = 0.1f;

	public bool applyScale = true;

	public bool faceCamera = true;

	public float maxDistance = -1f;

	public float minDistance;

	public bool directional;

	public float directionalPower = 1f;

	public bool cameraUp;

	public bool updateRoutine;

	public bool sizeByBrightness;

	private Transform myTransform;

	private Transform parent;

	private void Awake()
	{
		myTransform = base.transform;
	}

	private void OnEnable()
	{
		if (updateRoutine)
		{
			StartCoroutine(UpdateRoutine());
		}
		CurrentCameraEvents.OnRenderCamera += OnRenderCamera;
	}

	private void OnDisable()
	{
		CurrentCameraEvents.OnRenderCamera -= OnRenderCamera;
	}

	private void OnRenderCamera(Camera c)
	{
		UpdateIcon(c.transform, c.fieldOfView);
	}

	private IEnumerator UpdateRoutine()
	{
		while (base.enabled)
		{
			UpdateIcon(VRHead.instance.transform, VRHead.instance.fieldOfView);
			yield return null;
		}
	}

	private void UpdateIcon(Transform camTf, float fov)
	{
		Vector3 forward = myTransform.position - camTf.position;
		if (faceCamera)
		{
			if (cameraUp)
			{
				myTransform.rotation = Quaternion.LookRotation(forward, camTf.up);
			}
			else
			{
				myTransform.rotation = Quaternion.LookRotation(forward);
			}
		}
		if (applyScale)
		{
			float num = forward.magnitude;
			if (maxDistance > 0f)
			{
				num = Mathf.Clamp(num, minDistance, maxDistance);
			}
			fov = Mathf.Max(fov, 0.01f);
			float num2 = num * (fov / 180f);
			float num3 = 1f;
			if (directional)
			{
				num3 = Mathf.Clamp(Vector3.Dot(forward.normalized, -myTransform.parent.forward), 0.0001f, 1f);
				num3 = Mathf.Pow(num3, directionalPower);
			}
			if (sizeByBrightness)
			{
				num2 *= EnvironmentManager.CurrentLightFlareSizeMult;
			}
			myTransform.localScale = Mathf.Max(1E-05f, IconScaleMultiplier * num3 * scale * num2) * Vector3.one;
		}
	}
}

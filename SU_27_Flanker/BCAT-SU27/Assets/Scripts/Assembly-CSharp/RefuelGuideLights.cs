using UnityEngine;

public class RefuelGuideLights : MonoBehaviour
{
	public GameObject displayObject;

	public GameObject[] altitudeLights;

	public GameObject[] forwardLights;

	public float forwardExtent;

	public float altitudeExtent;

	public Transform targetPositionTransform;

	private Transform portTf;

	private float altInterval;

	private float fwdInterval;

	public bool guiding { get; private set; }

	private void Start()
	{
		altInterval = altitudeExtent * 2f / (float)altitudeLights.Length;
		fwdInterval = forwardExtent * 2f / (float)forwardLights.Length;
		displayObject.SetActive(value: false);
	}

	private void Update()
	{
		if (!guiding)
		{
			return;
		}
		if (portTf == null)
		{
			EndGuiding();
			return;
		}
		Vector3 vector = targetPositionTransform.InverseTransformPoint(portTf.position);
		float num = 0f - vector.y;
		float z = vector.z;
		num += altitudeExtent - altInterval / 2f;
		for (int i = 0; i < altitudeLights.Length; i++)
		{
			if (Mathf.Abs(num - (float)i * altInterval) < altInterval * 0.7f)
			{
				altitudeLights[i].SetActive(value: true);
			}
			else
			{
				altitudeLights[i].SetActive(value: false);
			}
		}
		z += forwardExtent - fwdInterval / 2f;
		for (int j = 0; j < forwardLights.Length; j++)
		{
			if (Mathf.Abs(z - (float)j * fwdInterval) < fwdInterval * 0.7f)
			{
				forwardLights[j].SetActive(value: true);
			}
			else
			{
				forwardLights[j].SetActive(value: false);
			}
		}
	}

	public void BeginGuiding(Transform portTransform)
	{
		displayObject.SetActive(value: true);
		portTf = portTransform;
		guiding = true;
	}

	public void EndGuiding()
	{
		displayObject.SetActive(value: false);
		portTf = null;
		guiding = false;
	}
}

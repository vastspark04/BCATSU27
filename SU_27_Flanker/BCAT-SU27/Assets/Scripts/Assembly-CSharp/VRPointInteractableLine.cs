using UnityEngine;

public class VRPointInteractableLine : MonoBehaviour
{
	public VRInteractable target;

	public Transform interactionTransform;

	private LineRenderer lr;

	private const int numPositions = 20;

	private Vector3[] positions;

	private Material lineMat;

	private void Awake()
	{
		lr = base.gameObject.AddComponent<LineRenderer>();
		lr.positionCount = 20;
		lr.startWidth = 0.01f;
		lr.endWidth = 0.01f;
		lineMat = new Material(Shader.Find("Particles/MF-Alpha Blended"));
		lineMat.SetColor("_TintColor", new Color(0f, 0.5f, 1f, 0.15f));
		lr.material = lineMat;
		positions = new Vector3[20];
	}

	private void OnDestroy()
	{
		if ((bool)lineMat)
		{
			Object.DestroyImmediate(lineMat);
		}
	}

	private void Update()
	{
		if ((bool)target && !ControllerEventHandler.eventsPaused)
		{
			lr.enabled = true;
			Vector3 position = target.transform.position;
			float magnitude = (position - base.transform.position).magnitude;
			Ray ray = new Ray(interactionTransform.position, interactionTransform.forward);
			Ray ray2 = new Ray(interactionTransform.position, position - interactionTransform.position);
			for (int i = 0; i < 20; i++)
			{
				float num = (float)i / 19f;
				float distance = num * magnitude;
				positions[i] = Vector3.Lerp(ray.GetPoint(distance), ray2.GetPoint(distance), num);
			}
			lr.SetPositions(positions);
			lr.startWidth = 0.01f;
			lr.endWidth = lr.startWidth * Mathf.Max(1f, Mathf.Sqrt(Vector3.Distance(VRHead.position, target.transform.position)));
		}
		else
		{
			Ray ray3 = new Ray(interactionTransform.position, interactionTransform.forward);
			for (int j = 0; j < 20; j++)
			{
				positions[j] = ray3.GetPoint((float)j / 20f);
			}
			lr.SetPositions(positions);
			lr.startWidth = 0.002f;
			lr.endWidth = 0.002f;
		}
	}
}

using UnityEngine;

public class CollimatedHUDUI : MonoBehaviour
{
	public float depth = 1000f;

	public float UIscale = 1f;

	public Transform staticHudParent;

	private float worldScale;

	private Transform myTransform;

	private void Start()
	{
		myTransform = base.transform;
		worldScale = 1f / myTransform.parent.TransformVector(Vector3.right).magnitude;
		Vector3 localScale = GetScaleFactor() * Vector3.one;
		myTransform.localScale = localScale;
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.CompareTag("StaticHUD"))
			{
				transform.SetParent(staticHudParent, worldPositionStays: true);
			}
		}
	}

	public float GetScaleFactor()
	{
		return 26f / 45f * UIscale * depth;
	}

	private void Update()
	{
		Vector3 localPosition = new Vector3(0f, 0f, depth * worldScale);
		Vector3 vector = myTransform.parent.InverseTransformPoint(VRHead.position);
		localPosition.x = vector.x;
		localPosition.y = vector.y;
		myTransform.localPosition = localPosition;
	}
}

using System.Collections;
using UnityEngine;

public class VRLodGroupTest : MonoBehaviour
{
	public float minDist = 10f;

	public float maxDist = 10000f;

	public float angle = 30f;

	public float time = 10f;

	public Transform camParentTf;

	private float rate;

	private void Start()
	{
		rate = 1f / time;
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		Vector3 startPos3 = camParentTf.position;
		startPos3.y = 0f;
		startPos3 = startPos3.normalized * minDist;
		Vector3 axis = Vector3.Cross(Vector3.up, startPos3);
		startPos3 = Quaternion.AngleAxis(0f - angle, axis) * startPos3;
		Vector3 endPos = startPos3.normalized * maxDist;
		while (base.enabled)
		{
			float t = 0f;
			while (t < 1f)
			{
				t = Mathf.MoveTowards(t, 1f, rate * Time.deltaTime);
				camParentTf.position = Vector3.Lerp(startPos3, endPos, t * t);
				yield return null;
			}
			while (t > 0f)
			{
				t = Mathf.MoveTowards(t, 0f, rate * Time.deltaTime);
				camParentTf.position = Vector3.Lerp(startPos3, endPos, t * t);
				yield return null;
			}
			yield return null;
		}
	}
}

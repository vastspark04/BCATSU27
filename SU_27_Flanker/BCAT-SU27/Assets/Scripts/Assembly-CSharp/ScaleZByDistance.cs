using UnityEngine;

public class ScaleZByDistance : MonoBehaviour
{
	public Transform stretchTf;

	public Transform targetTf;

	public bool localDist;

	private void OnDrawGizmos()
	{
		if ((bool)stretchTf && (bool)targetTf)
		{
			LateUpdate();
		}
	}

	private void LateUpdate()
	{
		float z = ((!localDist) ? (stretchTf.position - targetTf.position).magnitude : (stretchTf.parent.InverseTransformPoint(targetTf.position) - stretchTf.localPosition).magnitude);
		stretchTf.localScale = new Vector3(1f, 1f, z);
	}
}

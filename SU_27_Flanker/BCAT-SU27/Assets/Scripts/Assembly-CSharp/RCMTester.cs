using UnityEngine;

public class RCMTester : MonoBehaviour
{
	private RaycastCommandManager.RaycastHandle rHandle;

	private void Start()
	{
		if (RaycastCommandManager.instance.testBatched)
		{
			rHandle = RaycastCommandManager.instance.RegisterRaycaster(this);
		}
	}

	private void Update()
	{
		Vector3 vector = new Vector3(10f, 0f, 10f);
		Vector3 position = base.transform.position + vector;
		RaycastHit hitInfo;
		if (RaycastCommandManager.instance.testBatched)
		{
			rHandle.origin = base.transform.position + 500f * Vector3.up;
			rHandle.direction = Vector3.down;
			rHandle.rayDistance = 1000f;
			rHandle.layerMask = 1;
			if (rHandle.hit)
			{
				position.y = rHandle.resultPoint.y;
			}
		}
		else if (Physics.Raycast(base.transform.position + 500f * Vector3.up, Vector3.down, out hitInfo, 1000f, 1, QueryTriggerInteraction.Ignore))
		{
			position.y = hitInfo.point.y;
		}
		base.transform.position = position;
	}
}

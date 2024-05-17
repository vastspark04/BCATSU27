using UnityEngine;

public class VTSOWall : VTStaticObject
{
	protected override void OnPlacedInEditor()
	{
		base.OnPlacedInEditor();
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.gameObject.layer == 0)
			{
				collider.gameObject.layer = 1;
			}
		}
		if (Physics.Raycast(base.transform.position + 100f * Vector3.up, Vector3.down, out var hitInfo, 200f, 1))
		{
			Vector3 forward = Vector3.ProjectOnPlane(base.transform.forward, hitInfo.normal);
			Vector3 position = base.transform.position;
			position.y = hitInfo.point.y;
			base.transform.position = position;
			base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			SetGlobalPosition(VTMapManager.WorldToGlobalPoint(base.transform.position));
		}
		componentsInChildren = GetComponentsInChildren<Collider>();
		foreach (Collider collider2 in componentsInChildren)
		{
			if (collider2.gameObject.layer == 1)
			{
				collider2.gameObject.layer = 0;
			}
		}
	}
}

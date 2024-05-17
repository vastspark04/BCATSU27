using UnityEngine;

public class VTSOHelipad : VTStaticObject
{
	public Bounds bounds;

	private void OnDrawGizmosSelected()
	{
		Color cyan = Color.cyan;
		cyan.a = 0.75f;
		Gizmos.color = cyan;
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.lossyScale);
		Vector3 center = bounds.center;
		center.y = -5f;
		Vector3 size = bounds.size;
		size.y = 10f;
		Gizmos.DrawWireCube(center, size);
		cyan.a = 0.15f;
		Gizmos.color = cyan;
		Gizmos.DrawCube(center, size);
		Gizmos.matrix = Matrix4x4.identity;
	}

	protected override void OnPlacedInEditor()
	{
		base.OnPlacedInEditor();
		float num = 0f;
		Vector3 extents = bounds.extents;
		Vector3 center = bounds.center;
		center.y = 0f;
		Vector3 pos = base.transform.TransformPoint(center + new Vector3(0f - extents.x, 0f, extents.z));
		Vector3 pos2 = base.transform.TransformPoint(center + new Vector3(extents.x, 0f, extents.z));
		Vector3 pos3 = base.transform.TransformPoint(center + new Vector3(0f - extents.x, 0f, 0f - extents.z));
		Vector3 pos4 = base.transform.TransformPoint(center + new Vector3(extents.x, 0f, 0f - extents.z));
		num = Mathf.Max(0f, GetHeight(pos), GetHeight(pos2), GetHeight(pos3), GetHeight(pos4));
		Vector3 worldPoint = base.transform.position + num * Vector3.up;
		SetGlobalPosition(VTMapManager.WorldToGlobalPoint(worldPoint));
	}

	private float GetHeight(Vector3 pos)
	{
		if (Physics.Raycast(pos + 220f * Vector3.up, Vector3.down, out var hitInfo, 440f, 1))
		{
			return hitInfo.point.y - base.transform.position.y;
		}
		return 0f;
	}
}

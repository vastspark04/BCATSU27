using UnityEngine;

public class VizualizeBounds : MonoBehaviour
{
	public Bounds bounds;

	private void OnDrawGizmos()
	{
		MeshRenderer component = GetComponent<MeshRenderer>();
		if ((bool)component)
		{
			bounds = component.bounds;
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}
}

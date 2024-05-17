using UnityEngine;

public class TerrainToPrefabSpaceTest : MonoBehaviour
{
	public Transform prefabTf;

	public Transform terrainTf;

	public Transform vertTf;

	private void OnDrawGizmos()
	{
		Vector3 localPosition = vertTf.localPosition;
		Matrix4x4 matrix4x = prefabTf.worldToLocalMatrix * terrainTf.localToWorldMatrix;
		Vector3 vector = terrainTf.InverseTransformPoint(prefabTf.position);
		Vector3 position = matrix4x * (localPosition - vector);
		Gizmos.DrawLine(prefabTf.position, prefabTf.TransformPoint(position));
	}
}

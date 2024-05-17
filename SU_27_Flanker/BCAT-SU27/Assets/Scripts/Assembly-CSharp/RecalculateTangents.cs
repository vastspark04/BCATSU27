using UnityEngine;

public class RecalculateTangents : MonoBehaviour
{
	[ContextMenu("Recalculate")]
	public void Recalculate()
	{
		GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();
	}
}

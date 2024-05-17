using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityBuilderPixelDeform : CityBuilderPixel
{
	private bool deformComplete;

	public List<MeshFilter> deformMeshes;

	private List<Mesh> sharedMeshes;

	private int meshesComplete;

	private List<Mesh> meshesToDestroy = new List<Mesh>();

	private void OnEnable()
	{
	}

	public override bool IsPlacementComplete()
	{
		if (base.IsPlacementComplete())
		{
			return deformComplete;
		}
		return false;
	}

	protected override void OnPlacePixel(MeshCollider meshCollider)
	{
		base.OnPlacePixel(meshCollider);
		if (sharedMeshes == null)
		{
			sharedMeshes = new List<Mesh>();
			foreach (MeshFilter deformMesh in deformMeshes)
			{
				sharedMeshes.Add(deformMesh.sharedMesh);
			}
		}
		StartCoroutine(DeformRoutine(meshCollider));
	}

	private IEnumerator DeformRoutine(MeshCollider meshCollider)
	{
		while (!base.transformPlacementComplete)
		{
			yield return null;
		}
		DispatchDeformActions(meshCollider);
	}

	private void ReportMeshComplete()
	{
		meshesComplete++;
		if (meshesComplete == deformMeshes.Count)
		{
			deformComplete = true;
		}
	}

	private void DispatchDeformActions(MeshCollider meshCollider)
	{
		List<Vector3> tempVerts = new List<Vector3>();
		for (int j = 0; j < deformMeshes.Count; j++)
		{
			MeshFilter i = deformMeshes[j];
			int qmIdx = j;
			VTMapCities.instance.AddQueuedAction(delegate
			{
				QueuedDeformMesh(meshCollider, i, tempVerts, sharedMeshes, qmIdx);
			});
		}
	}

	private void QueuedDeformMesh(MeshCollider meshCollider, MeshFilter m, List<Vector3> tempVerts, List<Mesh> sharedMeshes, int mIdx)
	{
		if (!meshCollider || !m || tempVerts == null || sharedMeshes == null)
		{
			return;
		}
		sharedMeshes[mIdx].GetVertices(tempVerts);
		Mesh mesh = m.sharedMesh;
		if (mesh == sharedMeshes[mIdx])
		{
			mesh = m.mesh;
			meshesToDestroy.Add(mesh);
		}
		Vector3 onNormal = m.transform.InverseTransformDirection(Vector3.up);
		for (int i = 0; i < mesh.vertexCount; i++)
		{
			Vector3 position = tempVerts[i];
			Vector3 vector = m.transform.TransformPoint(position);
			Ray ray = new Ray(vector + 100f * Vector3.up, Vector3.down);
			if (meshCollider.Raycast(ray, out var hitInfo, 200f))
			{
				vector = hitInfo.point;
				position = m.transform.InverseTransformPoint(vector);
				position += Vector3.Project(tempVerts[i], onNormal);
				tempVerts[i] = position;
			}
		}
		mesh.SetVertices(tempVerts);
		m.mesh = mesh;
		ReportMeshComplete();
	}

	private void OnDestroy()
	{
		if (meshesToDestroy == null)
		{
			return;
		}
		foreach (Mesh item in meshesToDestroy)
		{
			Object.Destroy(item);
		}
		meshesToDestroy.Clear();
		meshesToDestroy = null;
	}
}

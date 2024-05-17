using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityBuilderPixel : MonoBehaviour
{
	public bool drawGizmos = true;

	[Range(0f, 4f)]
	public int cityLevel;

	public Transform rotationTransform;

	public List<Transform> objectTransforms;

	[Header("Trees")]
	public List<Transform> treeTransforms;

	public List<Vector3> treePositions;

	private static Vector3 pixelCenterPoint = new Vector3(87.75f, 0f, 63.75f);

	protected bool transformPlacementComplete { get; private set; }

	public virtual bool IsPlacementComplete()
	{
		return transformPlacementComplete;
	}

	public void PlaceObjectsToSurface(MeshCollider meshCollider)
	{
		base.gameObject.SetActive(value: true);
		OnPlacePixel(meshCollider);
		VTMapCities.instance.AddQueuedAction(delegate
		{
			QueuedPlacement(meshCollider);
		});
	}

	private IEnumerator PlacementRoutine(MeshCollider meshCollider)
	{
		transformPlacementComplete = false;
		for (int i = 0; i < objectTransforms.Count; i++)
		{
			Transform transform = objectTransforms[i];
			Vector3 position = transform.position;
			position.y = base.transform.position.y + 100f;
			if (meshCollider.Raycast(new Ray(position, Vector3.down), out var hitInfo, 200f))
			{
				transform.position = hitInfo.point;
			}
			yield return null;
		}
		transformPlacementComplete = true;
	}

	private void QueuedPlacement(MeshCollider meshCollider)
	{
		if (this == null || objectTransforms == null)
		{
			return;
		}
		for (int i = 0; i < objectTransforms.Count; i++)
		{
			Transform transform = objectTransforms[i];
			if ((bool)transform)
			{
				Vector3 position = transform.position;
				position.y = base.transform.position.y + 100f;
				if (meshCollider.Raycast(new Ray(position, Vector3.down), out var hitInfo, 200f))
				{
					transform.position = hitInfo.point;
				}
			}
		}
		transformPlacementComplete = true;
	}

	protected virtual void OnPlacePixel(MeshCollider meshCollider)
	{
	}

	public void SetRotation(IntVector2 pixelCoord)
	{
		int x = pixelCoord.x;
		int y = pixelCoord.y;
		if (x % 2 == 0)
		{
			if (y % 2 == 0)
			{
				rotationTransform.rotation = Quaternion.identity;
			}
			else
			{
				rotationTransform.rotation = Quaternion.Euler(0f, 90f, 0f);
			}
		}
		else if (y % 2 == 0)
		{
			rotationTransform.rotation = Quaternion.Euler(0f, -90f, 0f);
		}
		else
		{
			rotationTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
		}
	}

	private void OnDrawGizmos()
	{
		if (!drawGizmos)
		{
			return;
		}
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(base.transform.TransformPoint(pixelCenterPoint), new Vector3(120f, 1f, 120f));
		Gizmos.color = Color.red;
		if (treeTransforms != null)
		{
			foreach (Transform treeTransform in treeTransforms)
			{
				if ((bool)treeTransform)
				{
					Gizmos.DrawLine(treeTransform.position, treeTransform.position + 12f * Vector3.up);
					Gizmos.DrawSphere(treeTransform.position, 1f);
				}
			}
		}
		Gizmos.color = Color.green;
		if (treePositions == null)
		{
			return;
		}
		foreach (Vector3 treePosition in treePositions)
		{
			Gizmos.DrawLine(rotationTransform.TransformPoint(treePosition), rotationTransform.TransformPoint(treePosition) + 12f * Vector3.up);
		}
	}
}

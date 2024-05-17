using System.Collections.Generic;
using UnityEngine;

public class VTSOCompound : VTStaticObject
{
	public List<VTStaticObject> subObjects;

	private List<Vector3> origLps = new List<Vector3>();

	private List<Quaternion> origRots = new List<Quaternion>();

	private void Awake()
	{
		for (int i = 0; i < subObjects.Count; i++)
		{
			Vector3 item = Vector3.zero;
			Quaternion item2 = Quaternion.identity;
			VTStaticObject vTStaticObject = subObjects[i];
			if ((bool)vTStaticObject)
			{
				item = vTStaticObject.transform.localPosition;
				item2 = vTStaticObject.transform.localRotation;
			}
			origLps.Add(item);
			origRots.Add(item2);
		}
	}

	protected override void OnPlacedInEditor()
	{
		base.OnPlacedInEditor();
		PlaceObjects();
	}

	public override void OnLoadedFromConfig()
	{
		base.OnLoadedFromConfig();
		PlaceObjects();
	}

	protected override void OnSpawned()
	{
		base.OnSpawned();
		PlaceObjects();
	}

	private void PlaceObjects()
	{
		ResetObjs();
		foreach (VTStaticObject subObject in subObjects)
		{
			if (!subObject)
			{
				continue;
			}
			Collider[] componentsInChildren = subObject.GetComponentsInChildren<Collider>();
			foreach (Collider collider in componentsInChildren)
			{
				if (collider.gameObject.layer == 0)
				{
					collider.gameObject.layer = 1;
				}
			}
			if ((bool)VTMapGenerator.fetch)
			{
				VTMapGenerator.fetch.BakeColliderAtPosition(subObject.transform.position);
			}
			if (Physics.Raycast(subObject.transform.position + 100f * Vector3.up, Vector3.down, out var hitInfo, 200f, 1))
			{
				Vector3 position = subObject.transform.position;
				position.y = hitInfo.point.y;
				subObject.transform.position = position;
				if (subObject.alignToSurface)
				{
					Vector3 forward = Vector3.ProjectOnPlane(subObject.transform.forward, hitInfo.normal);
					subObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
				}
			}
			subObject.MoveInEditor();
			componentsInChildren = subObject.GetComponentsInChildren<Collider>();
			foreach (Collider collider2 in componentsInChildren)
			{
				if (collider2.gameObject.layer == 1)
				{
					collider2.gameObject.layer = 0;
				}
			}
		}
	}

	private void ResetObjs()
	{
		for (int i = 0; i < subObjects.Count; i++)
		{
			VTStaticObject vTStaticObject = subObjects[i];
			if ((bool)vTStaticObject)
			{
				vTStaticObject.transform.localPosition = origLps[i];
				vTStaticObject.transform.localRotation = origRots[i];
			}
		}
	}
}

using UnityEngine;

[ExecuteInEditMode]
public class ChangeUnitsToVesselLayer : MonoBehaviour
{
	public bool apply;

	public bool thisHierarchyOnly = true;

	private void Start()
	{
	}

	private void Update()
	{
		if (!apply)
		{
			return;
		}
		apply = false;
		if (thisHierarchyOnly)
		{
			MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer meshRenderer in componentsInChildren)
			{
				if (meshRenderer.gameObject.layer == 0 && !meshRenderer.gameObject.GetComponent<Collider>())
				{
					meshRenderer.gameObject.layer = 8;
				}
			}
			return;
		}
		Actor[] array = Object.FindObjectsOfType<Actor>();
		for (int i = 0; i < array.Length; i++)
		{
			MeshRenderer[] componentsInChildren = array[i].gameObject.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer meshRenderer2 in componentsInChildren)
			{
				if (meshRenderer2.gameObject.layer == 0 && !meshRenderer2.gameObject.GetComponent<Collider>())
				{
					meshRenderer2.gameObject.layer = 8;
				}
			}
		}
	}
}

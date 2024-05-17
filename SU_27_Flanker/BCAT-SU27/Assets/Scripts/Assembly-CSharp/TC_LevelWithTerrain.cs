using UnityEngine;

[ExecuteInEditMode]
public class TC_LevelWithTerrain : MonoBehaviour
{
	public bool levelChildren;

	private void Update()
	{
		if (levelChildren)
		{
			levelChildren = false;
			LevelChildren();
		}
	}

	private void LevelChildren()
	{
		Ray ray = default(Ray);
		ray.direction = new Vector3(0f, -1f, 0f);
		LayerMask.NameToLayer("Terrain");
		int childCount = base.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			ray.origin = child.position;
			if (Physics.Raycast(ray, out var hitInfo))
			{
				child.position = new Vector3(child.position.x, hitInfo.point.y, child.position.z);
			}
		}
	}
}

using UnityEngine;

[ExecuteInEditMode]
public class PlatoonShifter : MonoBehaviour
{
	public IntVector2 shift;

	public bool apply;

	private void Start()
	{
	}

	private void Update()
	{
		if (apply)
		{
			apply = false;
			GridPlatoon[] componentsInChildren = GetComponentsInChildren<GridPlatoon>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].spawnInGrid += shift;
			}
		}
	}
}

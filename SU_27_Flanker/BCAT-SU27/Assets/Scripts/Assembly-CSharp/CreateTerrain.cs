using TerrainComposer2;
using UnityEngine;

[ExecuteInEditMode]
public class CreateTerrain : MonoBehaviour
{
	public bool createTerrain;

	private void Update()
	{
		if (createTerrain)
		{
			createTerrain = false;
			CreateTerrains();
		}
	}

	private void CreateTerrains()
	{
		
	}
}

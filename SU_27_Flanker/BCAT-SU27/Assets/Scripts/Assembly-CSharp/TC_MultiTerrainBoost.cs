using UnityEngine;

public class TC_MultiTerrainBoost : MonoBehaviour
{
	private Camera MainCamera;

	private bool[] active1;

	private Terrain[] terrains;

	private Bounds[] bounds;

	private Plane[] planes;

	private float distance;

	private int count_terrain;

	private void Start()
	{
		MainCamera = GetComponent<Camera>();
		terrains = Resources.FindObjectsOfTypeAll(typeof(Terrain)) as Terrain[];
		bounds = new Bounds[terrains.Length];
		active1 = new bool[terrains.Length];
		calcBounds();
	}

	private void LateUpdate()
	{
		calcFrustrum();
		for (count_terrain = 0; count_terrain < bounds.Length; count_terrain++)
		{
			if (IsRenderedFrom(bounds[count_terrain]))
			{
				if (!active1[count_terrain])
				{
					terrains[count_terrain].enabled = true;
					active1[count_terrain] = true;
				}
			}
			else if (active1[count_terrain])
			{
				terrains[count_terrain].enabled = false;
				active1[count_terrain] = false;
			}
		}
	}

	private void calcBounds()
	{
		for (count_terrain = 0; count_terrain < terrains.Length; count_terrain++)
		{
			bounds[count_terrain].size = terrains[count_terrain].terrainData.size;
			bounds[count_terrain].center = new Vector3(terrains[count_terrain].transform.position.x + bounds[count_terrain].size.x / 2f, terrains[count_terrain].transform.position.y + bounds[count_terrain].size.y / 2f, terrains[count_terrain].transform.position.z + bounds[count_terrain].size.z / 2f);
			active1[count_terrain] = true;
		}
	}

	private void calcFrustrum()
	{
		planes = GeometryUtility.CalculateFrustumPlanes(MainCamera);
	}

	private bool IsRenderedFrom(Bounds bound)
	{
		return GeometryUtility.TestPlanesAABB(planes, bound);
	}
}

using UnityEngine;

public class TC_TerrainNeighbors : MonoBehaviour
{
	public Terrain left;

	public Terrain top;

	public Terrain right;

	public Terrain bottom;

	public void Start()
	{
		GetComponent<Terrain>().SetNeighbors(left, top, right, bottom);
	}
}

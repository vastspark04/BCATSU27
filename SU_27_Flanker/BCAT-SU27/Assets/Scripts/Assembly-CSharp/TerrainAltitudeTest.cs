using System.Diagnostics;
using UnityEngine;

public class TerrainAltitudeTest : MonoBehaviour
{
	public Transform testTf;

	public Vector2 uv;

	public float clr;

	private void Awake()
	{
		base.gameObject.AddComponent<FloatingOriginTransform>();
	}

	private void Update()
	{
		if ((bool)testTf && (bool)VTMapGenerator.fetch)
		{
			float terrainAltitude = VTMapGenerator.fetch.GetTerrainAltitude(base.transform.position);
			Vector3 position = base.transform.position;
			position.y = WaterPhysics.instance.height + terrainAltitude;
			testTf.position = position;
			Vector3D vector3D = VTMapManager.WorldToGlobalPoint(base.transform.position);
			int gridSize = VTMapGenerator.fetch.gridSize;
			float chunkSize = VTMapGenerator.fetch.chunkSize;
			uv = new Vector2((float)vector3D.x, (float)vector3D.z);
			float num = (float)gridSize * chunkSize;
			uv /= num;
			clr = VTMapGenerator.fetch.hmBdt.GetColorUV(uv.x, uv.y).r;
			if (Input.GetKeyDown(KeyCode.KeypadMinus))
			{
				TestPerf();
			}
		}
	}

	private void TestPerf()
	{
		Stopwatch stopwatch = new Stopwatch();
		int num = 5000;
		stopwatch.Start();
		for (int i = 0; i < num; i++)
		{
			Vector3 vector = base.transform.position + Random.onUnitSphere * 5000f;
			vector.y = WaterPhysics.instance.height;
			Physics.Linecast(vector + 10000f * Vector3.up, vector, out var _, 1);
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("raycasts: " + stopwatch.ElapsedMilliseconds + " ms");
		stopwatch.Reset();
		stopwatch.Start();
		for (int j = 0; j < num; j++)
		{
			VTMapGenerator.fetch.GetHeightmapAltitude(base.transform.position + Random.onUnitSphere * 5000f);
		}
		stopwatch.Stop();
		UnityEngine.Debug.Log("hmLookups: " + stopwatch.ElapsedMilliseconds + " ms");
	}
}

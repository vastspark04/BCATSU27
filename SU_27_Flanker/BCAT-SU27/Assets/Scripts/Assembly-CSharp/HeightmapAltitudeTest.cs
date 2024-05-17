using UnityEngine;

public class HeightmapAltitudeTest : MonoBehaviour
{
	public float interval = 38.4f;

	public float size = 614.4f;

	public Vector3 posOffset;

	private void Start()
	{
	}

	private void Update()
	{
		if (!VTCustomMapManager.instance)
		{
			return;
		}
		for (float num = (0f - size) / 2f; num < size / 2f; num += interval)
		{
			for (float num2 = (0f - size) / 2f; num2 < size / 2f; num2 += interval)
			{
				Vector3 vector = base.transform.position + new Vector3(num, 0f, num2);
				vector.y = VTCustomMapManager.instance.mapGenerator.GetHeightmapAltitude(vector + posOffset) + WaterPhysics.waterHeight;
				Debug.DrawLine(vector + new Vector3(2f, 0f, 2f), vector - new Vector3(2f, 0f, 2f));
				Debug.DrawLine(vector + new Vector3(-2f, 0f, 2f), vector - new Vector3(-2f, 0f, 2f));
				Debug.DrawLine(vector, vector + new Vector3(0f, 10f, 0f));
			}
		}
	}
}
